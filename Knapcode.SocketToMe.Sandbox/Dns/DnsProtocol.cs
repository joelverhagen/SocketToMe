using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Knapcode.SocketToMe.Sandbox.Extensions;
using Knapcode.SocketToMe.Sandbox.Dns.Enumerations;
using Knapcode.SocketToMe.Sandbox.Dns.NameRecords;
using Knapcode.SocketToMe.Sandbox.Dns.ResourceRecords;

namespace Knapcode.SocketToMe.Sandbox.Dns
{
    public static class DnsProtocol
    {
        public static byte[] GetDnsRequestMessageBytes(int id, RequestCode requestCode, IEnumerable<Question> questions)
        {
            questions = questions.ToArray();

            var requestHeader = new Header
            {
                Id = id,
                Opcode = Opcode.StandardQuery,
                IsRecursionDesired = true,
                RequestCode = RequestCode.Size512,
                QuestionCount = questions.Count()
            };

            return Enumerable.Empty<byte>()
                .Concat(GetHeaderBytes(requestHeader))
                .Concat(questions.Select(GetQuestionBytes).SelectMany(b => b))
                .ToArray();
        }

        public static DnsResponseMessage GetDnsResponseMessage(byte[] responseContent)
        {
            int offset = 0;

            // parse the header
            var header = GetHeader(responseContent, offset);
            offset += header.Length;

            // parse the questions
            var rawQuestions = new List<RawQuestion>();
            for (int i = 0; i < header.QuestionCount; i++)
            {
                RawQuestion rawQuestion = GetRawQuestion(responseContent, offset);
                rawQuestions.Add(rawQuestion);
                offset += rawQuestion.Length;
            }

            // parse the answers
            var rawAnswers = new List<RawResourceRecord>();
            for (int i = 0; i < header.AnswerCount; i++)
            {
                RawResourceRecord rawAnswer = GetRawResourceRecord(responseContent, offset);
                rawAnswers.Add(rawAnswer);
                offset += rawAnswer.Length;
            }

            // parse the name servers
            var rawNameServers = new List<RawResourceRecord>();
            for (int i = 0; i < header.NameServerCount; i++)
            {
                RawResourceRecord rawNameServer = GetRawResourceRecord(responseContent, offset);
                rawNameServers.Add(rawNameServer);
                offset += rawNameServer.Length;
            }

            // parse the additional records
            var rawAdditionalRecords = new List<RawResourceRecord>();
            for (int i = 0; i < header.AdditionalRecordCount; i++)
            {
                RawResourceRecord rawAdditionalRecord = GetRawResourceRecord(responseContent, offset);
                rawAdditionalRecords.Add(rawAdditionalRecord);
                offset += rawAdditionalRecord.Length;
            }

            // convert raw records to more friendly models
            var response = new DnsResponseMessage
            {
                ResponseContent = responseContent,
                Header = header,
                Questions = rawQuestions.Select(r => GetQuestion(responseContent, 0, r)).ToList(),
                Answers = rawAnswers.Select(r => GetResourceRecord(responseContent, 0, r)).ToList(),
                NameServers = rawNameServers.Select(r => GetResourceRecord(responseContent, 0, r)).ToList(),
                AdditionalRecords = rawAdditionalRecords.Select(r => GetResourceRecord(responseContent, 0, r)).ToList()
            };

            return response;
        }

        public static byte[] GetHeaderBytes(Header header)
        {
            var shortId = (ushort) (header.Id%(ushort.MaxValue + 1));
            byte[] idBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) shortId));

            byte byte3 = 0;
            byte3 = byte3.SetBit(7, header.IsResponse);
            byte3 |= (byte) (((byte) header.Opcode%16) << 3);
            byte3 = byte3.SetBit(2, header.IsAuthoritativeAnswer);
            byte3 = byte3.SetBit(1, header.IsTruncated);
            byte3 = byte3.SetBit(0, header.IsRecursionDesired);

            byte byte4 = 0;
            byte4 = byte4.SetBit(7, header.IsRecursionAvailable);
            if (!header.IsResponse)
            {
                byte4 |= (byte) ((byte) header.RequestCode%16);
            }
            else
            {
                byte4 |= (byte) ((byte) header.ResponseCode%16);
            }

            byte[] countBytes = new[] {header.QuestionCount, header.AnswerCount, header.NameServerCount, header.AdditionalRecordCount}
                .Select(i => BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) i)))
                .SelectMany(b => b)
                .ToArray();

            return Enumerable.Empty<byte>()
                .Concat(idBytes)
                .Concat(new[] {byte3, byte4})
                .Concat(countBytes)
                .ToArray();
        }

        public static Header GetHeader(byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length < startIndex + 12)
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "The responseContent must have at least 12 bytes after the start index. The start index was {0} and the responseContent had {1} bytes.",
                    startIndex,
                    buffer.Length);
                throw new ArgumentException(message, "buffer");
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", "The start index must be an integer greater than or equal to zero.");
            }

            var header = new Header
            {
                Id = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, startIndex)),
                StartIndex = startIndex,
                Length = 12
            };

            byte byte3 = buffer[startIndex + 2];
            header.IsResponse = byte3.GetBit(7);
            header.Opcode = (Opcode) ((byte3 & 120) >> 3);
            header.IsAuthoritativeAnswer = byte3.GetBit(2);
            header.IsTruncated = byte3.GetBit(1);
            header.IsRecursionDesired = byte3.GetBit(0);

            byte byte4 = buffer[startIndex + 3];
            header.IsRecursionAvailable = byte4.GetBit(7);
            if ((byte4 & 112) != 0)
            {
                throw new Exception("The response responseContent had non-zero bits set in the reserved header section");
            }
            if (!header.IsResponse)
            {
                header.RequestCode = (RequestCode) (byte4 & 15);
            }
            else
            {
                header.ResponseCode = (ResponseCode) (byte4 & 15);
            }

            header.QuestionCount = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, startIndex + 4));
            header.AnswerCount = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, startIndex + 6));
            header.NameServerCount = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, startIndex + 8));
            header.AdditionalRecordCount = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, startIndex + 10));

            return header;
        }

        public static byte[] GetQuestionBytes(Question question)
        {
            var bytes = new List<byte>();

            // write the labels
            foreach (string label in question.Name.Split('.'))
            {
                bytes.Add((byte) label.Length);
                bytes.AddRange(Encoding.ASCII.GetBytes(label));
            }
            bytes.Add(0);

            // write the type and class
            bytes.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) question.Type)));
            bytes.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) question.Class)));

            return bytes.ToArray();
        }

        public static RawQuestion GetRawQuestion(byte[] buffer, int startIndex)
        {
            NameRecord[] nameRecords = GetNameRecords(buffer, startIndex);
            NameRecord lastNameRecord = nameRecords.Last();
            int nextIndex = lastNameRecord.StartIndex + lastNameRecord.Length;

            // read the type and class
            var questionType = (QuestionType) (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, nextIndex));
            nextIndex += 2;

            var questionClass = (QuestionClass) (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, nextIndex));
            nextIndex += 2;

            return new RawQuestion
            {
                Class = questionClass,
                Name = nameRecords,
                Type = questionType,
                StartIndex = startIndex,
                Length = nextIndex - startIndex
            };
        }

        public static RawResourceRecord GetRawResourceRecord(byte[] buffer, int startIndex)
        {
            // read the name
            NameRecord[] nameRecords = GetNameRecords(buffer, startIndex);
            NameRecord lastNameRecord = nameRecords.Last();
            int nextIndex = lastNameRecord.StartIndex + lastNameRecord.Length;

            // read the type, class, TTL, data length, and data
            var recordType = (DnsType) (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, nextIndex));
            nextIndex += 2;

            var recordClass = (Class) (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, nextIndex));
            nextIndex += 2;

            var ttl = (uint) IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, nextIndex));
            nextIndex += 4;

            var dataLength = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, nextIndex));
            nextIndex += 2;

            int dataStartIndex = nextIndex;
            nextIndex += dataLength;

            return new RawResourceRecord
            {
                Name = nameRecords.ToArray(),
                Type = recordType,
                Class = recordClass,
                Ttl = TimeSpan.FromSeconds(ttl),
                DataLength = dataLength,
                DataStartIndex = dataStartIndex,
                StartIndex = startIndex,
                Length = nextIndex - startIndex
            };
        }

        public static Question GetQuestion(byte[] buffer, int startIndex, RawQuestion rawQuestion)
        {
            return new Question
            {
                Class = rawQuestion.Class,
                Length = rawQuestion.Length,
                Name = GetName(buffer, startIndex, rawQuestion.Name),
                StartIndex = rawQuestion.StartIndex,
                Type = rawQuestion.Type
            };
        }

        public static ResourceRecord GetResourceRecord(byte[] buffer, int startIndex, RawResourceRecord rawResourceRecord)
        {
            return new ResourceRecordFactory().GetResourceRecord(buffer, startIndex, rawResourceRecord);
        }

        public static string GetName(byte[] buffer, int startIndex, IEnumerable<NameRecord> nameRecords)
        {
            IEnumerable<string> pieces = ResolveNameRecords(buffer, startIndex, nameRecords);
            string name = string.Join(".", pieces);
            return name;
        }

        public static IEnumerable<string> ResolveNameRecords(byte[] buffer, int startIndex, IEnumerable<NameRecord> nameRecords)
        {
            foreach (NameRecord nameRecord in nameRecords)
            {
                var label = nameRecord as Label;
                if (label != null)
                {
                    yield return label.Value;
                    continue;
                }

                var pointer = nameRecord as Pointer;
                if (pointer != null)
                {
                    foreach (string value in ResolveNameRecords(buffer, startIndex, GetNameRecords(buffer, startIndex + pointer.Offset)))
                    {
                        yield return value;
                    }
                    continue;
                }

                var zero = nameRecord as Zero;
                if (zero != null)
                {
                    continue;
                }

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "The resource name record of type '{0}' has not been fully implemented.",
                    nameRecord.GetType().FullName);
                throw new NotImplementedException(message);
            }
        }

        public static NameRecord[] GetNameRecords(byte[] buffer, int startIndex)
        {
            var nameRecords = new List<NameRecord>();
            int nextIndex = startIndex;
            while (buffer[nextIndex] != 0)
            {
                byte nextByte = buffer[nextIndex];
                if ((nextByte & 192) == 192)
                {
                    var offset = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, nextIndex));
                    offset &= 16383;

                    nameRecords.Add(new Pointer
                    {
                        Offset = offset,
                        Length = 2,
                        StartIndex = nextIndex
                    });
                    break;
                }

                // TODO uncomment
                /*
                if ((nextByte & 128) == 128 || (nextByte & 64) == 64)
                {
                    string message = string.Format(
                        CultureInfo.InvariantCulture,
                        "The label length byte at index {0} (0x{1:x2}) was not valid. It should either have '11' (indicating a pointer) or '00' (indicating a string) as its two most significant bits.",
                        nextIndex,
                        nextByte);
                    throw new ArgumentException(message);
                }
                */

                string value = Encoding.ASCII.GetString(buffer, nextIndex + 1, nextByte);
                nameRecords.Add(new Label
                {
                    Value = value,
                    StartIndex = nextIndex,
                    Length = nextByte + 1
                });

                nextIndex += 1 + nextByte;

                if (buffer[nextIndex] == 0)
                {
                    nameRecords.Add(new Zero
                    {
                        Length = 1,
                        StartIndex = nextIndex
                    });
                }
            }

            return nameRecords.ToArray();
        }
    }
}