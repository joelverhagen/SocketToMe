using FluentAssertions;
using Knapcode.SocketToMe.Sandbox.Dns;
using Knapcode.SocketToMe.Sandbox.Dns.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knapcode.SocketToMe.Sandbox.Tests.Dns
{
    [TestClass]
    public class DnsProtocolTests
    {
        [TestMethod]
        public void GetHeader_WithValidHeader_DeserializesProperly()
        {
            // ARRANGE
            var expected = new Header
            {
                AdditionalRecordCount = 1,
                AnswerCount = 2,
                Id = 3,
                IsAuthoritativeAnswer = true,
                IsRecursionAvailable = true,
                IsRecursionDesired = true,
                IsResponse = true,
                IsTruncated = true,
                NameServerCount = 4,
                Opcode = (Opcode) 5,
                QuestionCount = 6,
                ResponseCode = (ResponseCode) 7
            };
            byte[] buffer = DnsProtocol.GetHeaderBytes(expected);

            // ACT
            Header actual = DnsProtocol.GetHeader(buffer, 0);

            // ASSERT
            actual.ShouldBeEquivalentTo(expected);
        }

        [TestMethod]
        public void GetQuestion_WithValidQuestion_DeserializesProperly()
        {
            // ARRANGE
            var expected = new Question
            {
                Class = (QuestionClass) 1,
                Name = "www.example.com",
                Type = (QuestionType) 2
            };
            byte[] buffer = DnsProtocol.GetQuestionBytes(expected);

            // ACT
            Question actual = DnsProtocol.GetQuestion(buffer, 0, DnsProtocol.GetRawQuestion(buffer, 0));

            // ASSERT
            actual.ShouldBeEquivalentTo(expected);
        }
    }
}
