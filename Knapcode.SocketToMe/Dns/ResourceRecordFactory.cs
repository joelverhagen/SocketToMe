using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Knapcode.SocketToMe.Extensions;
using Knapcode.SocketToMe.Dns.Enumerations;
using Knapcode.SocketToMe.Dns.NameRecords;
using Knapcode.SocketToMe.Dns.ResourceRecords;

namespace Knapcode.SocketToMe.Dns
{
    public class ResourceRecordFactory
    {
        /// <summary>
        /// Source: http://tools.ietf.org/html/rfc4034
        /// </summary>
        private const byte DnskeyProtocol = 3;

        public ResourceRecord GetResourceRecord(byte[] buffer, int startIndex, RawResourceRecord rawResourceRecord)
        {
            // perform type-specific parsing
            ResourceRecord resourceRecord;
            var data = new byte[rawResourceRecord.DataLength];
            Buffer.BlockCopy(buffer, startIndex + rawResourceRecord.DataStartIndex, data, 0, rawResourceRecord.DataLength);
            switch (rawResourceRecord.Type)
            {
                case DnsType.A:
                    resourceRecord = new AResourceRecord {Address = new IPAddress(data)};
                    break;

                case DnsType.Ns:
                    resourceRecord = new NsResourceRecord {NameServer = DnsProtocol.GetName(buffer, 0, DnsProtocol.GetNameRecords(data, 0))};
                    break;

                case DnsType.Soa:
                    resourceRecord = GetSoaResourceRecord(buffer, startIndex, data);
                    break;

                case DnsType.Mx:
                    resourceRecord = new MxResourceRecord
                    {
                        Preference = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0)),
                        Exchange = DnsProtocol.GetName(buffer, 0, DnsProtocol.GetNameRecords(data, 2))
                    };
                    break;

                case DnsType.Txt:
                    resourceRecord = new TxtResourceRecord
                    {
                        Text = Encoding.ASCII.GetString(data),
                        Data = data
                    };
                    break;

                case DnsType.Aaaa:
                    resourceRecord = new AResourceRecord {Address = new IPAddress(data)};
                    break;

                case DnsType.Ds:
                    resourceRecord = GetDsResourceRecord(data);
                    break;

                case DnsType.Rrsig:
                    resourceRecord = GetRrsigResourceRecord(buffer, startIndex, data);
                    break;

                case DnsType.Nsec:
                    resourceRecord = GetNsecResourceRecord(buffer, startIndex, data);
                    break;

                case DnsType.DnsKey:
                    resourceRecord = GetDnskeyResourceRecord(data);
                    break;

                default:
                    resourceRecord = new UnknownResourceRecord {Data = data};
                    break;
            }

            // fill in general fields
            resourceRecord.Name = DnsProtocol.GetName(buffer, startIndex, rawResourceRecord.Name);
            resourceRecord.Type = rawResourceRecord.Type;
            resourceRecord.Class = rawResourceRecord.Class;
            resourceRecord.Ttl = rawResourceRecord.Ttl;

            return resourceRecord;
        }

        private static SoaResourceRecord GetSoaResourceRecord(byte[] buffer, int startIndex, byte[] data)
        {
            NameRecord[] primaryNameServerNameRecords = DnsProtocol.GetNameRecords(data, 0);
            NameRecord lastNameRecord = primaryNameServerNameRecords.Last();
            int nextIndex = lastNameRecord.StartIndex + lastNameRecord.Length;

            NameRecord[] responsibleMailAddressRecords = DnsProtocol.GetNameRecords(data, nextIndex);
            lastNameRecord = responsibleMailAddressRecords.Last();
            nextIndex = lastNameRecord.StartIndex + lastNameRecord.Length;

            int serialNumber = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, nextIndex));
            var refresh = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, nextIndex + 4));
            var retry = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, nextIndex + 8));
            var expire = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, nextIndex + 12));
            var minimum = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, nextIndex + 16));

            return new SoaResourceRecord
            {
                PrimaryNameServer = DnsProtocol.GetName(buffer, startIndex, primaryNameServerNameRecords),
                ResponsibleMailAddress = DnsProtocol.GetName(buffer, startIndex, responsibleMailAddressRecords),
                SerialNumber = serialNumber,
                Refresh = TimeSpan.FromSeconds(refresh),
                Retry = TimeSpan.FromSeconds(retry),
                Expire = TimeSpan.FromSeconds(expire),
                Minimum = TimeSpan.FromSeconds(minimum)
            };
        }

        private static DsResourceRecord GetDsResourceRecord(byte[] data)
        {
            short keyTag = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            var algorithm = (DnssecAlgorithmType) data[2];
            var digestType = (DnssecDigestType) data[3];

            int digestLength;
            switch (digestType)
            {
                case DnssecDigestType.Sha1:
                    digestLength = 20;
                    break;

                case DnssecDigestType.Sha256:
                    digestLength = 32;
                    break;

                default:
                    string message = string.Format(
                        CultureInfo.InvariantCulture,
                        "The DNSSEC digest type '{0}' (0x{1:x2}) is not supported.",
                        digestType,
                        (byte) digestType);
                    throw new NotImplementedException(message);
            }

            var digest = new byte[digestLength];
            Buffer.BlockCopy(data, 4, digest, 0, digestLength);

            return new DsResourceRecord
            {
                KeyTag = keyTag,
                Algorithm = algorithm,
                DigestType = digestType,
                Digest = digest
            };
        }

        private static RrsigResourceRecord GetRrsigResourceRecord(byte[] buffer, int startIndex, byte[] data)
        {
            var typeCovered = (DnsType) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            var algorithm = (DnssecAlgorithmType) data[2];
            int labels = data[3];
            var originalTtl = (uint) IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 4));
            var signatureExpiration = (uint) IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 8));
            var signatureInception = (uint) IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 12));
            short keyTag = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 16));
            
            var nameRecords = DnsProtocol.GetNameRecords(data, 18);
            string signerName = DnsProtocol.GetName(buffer, startIndex, nameRecords);
            NameRecord lastNameRecord = nameRecords.Last();
            int nextIndex = lastNameRecord.StartIndex + lastNameRecord.Length;

            var signature = new byte[data.Length - nextIndex];
            Buffer.BlockCopy(data, nextIndex, signature, 0, signature.Length);

            return new RrsigResourceRecord
            {
                TypeCovered = typeCovered,
                Algorithm = algorithm,
                LabelCount = labels,
                OriginalTtl = TimeSpan.FromSeconds(originalTtl),
                SignatureExpiration = FromUnixTimestamp(signatureExpiration),
                SignatureInception = FromUnixTimestamp(signatureInception),
                KeyTag = keyTag,
                SignerName = signerName,
                Signature = signature
            };
        }

        private static DateTime FromUnixTimestamp(uint seconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);
        }

        private NsecResourceRecord GetNsecResourceRecord(byte[] buffer, int startIndex, byte[] data)
        {
            var nameRecords = DnsProtocol.GetNameRecords(data, 0);
            string nextDomainName = DnsProtocol.GetName(buffer, startIndex, nameRecords);
            NameRecord lastNameRecord = nameRecords.Last();
            int nextIndex = lastNameRecord.StartIndex + lastNameRecord.Length;

            var types = new List<DnsType>();
            while (nextIndex < data.Length)
            {
                int windowBlockMinimum = data[nextIndex] * 256;
                nextIndex++;
                byte bitmapLength = data[nextIndex];
                nextIndex++;

                for (int i = 0; i < bitmapLength * 8; i++)
                {
                    if (data[nextIndex + (i / 8)].GetBit(7 - (i % 8)))
                    {
                        types.Add((DnsType) windowBlockMinimum + i);
                    }
                }

                nextIndex += bitmapLength;
            }

            return new NsecResourceRecord
            {
                NextDomainName = nextDomainName,
                AllTypes = types
            };
        }

        private DnskeyResourceRecord GetDnskeyResourceRecord(byte[] data)
        {
            bool holdsDnsZoneKey = data[0].GetBit(0);
            bool holdsKeyForSecureEntryPoint = data[1].GetBit(0);
            byte protocol = data[2];
            var algorithm = (DnssecAlgorithmType) data[3];

            if (protocol != DnskeyProtocol)
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "The DNSKEY resource record has protocol version {0} instead of {1}.",
                    protocol,
                    DnskeyProtocol);
                throw new NotImplementedException(message);
            }

            var publicKey = new byte[data.Length - 4];
            Buffer.BlockCopy(data, 4, publicKey, 0, publicKey.Length);

            return new DnskeyResourceRecord
            {
                HoldsDnsZoneKey = holdsDnsZoneKey,
                HoldsKeyForSecureEntryPoint = holdsKeyForSecureEntryPoint,
                Protocol = protocol,
                Algorithm = algorithm,
                PublicKey = publicKey
            };
        }
    }
}