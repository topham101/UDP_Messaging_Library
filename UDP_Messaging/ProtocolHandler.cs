using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDP_Messaging
{
    public enum Protocol
    {
        Connection_Request = 1,
        Connection_Test = 2,
        Connection_Test_Response = 3,
        Content_Message = 4,
        Connection_Close = 5,
        Connection_Accepted = 6
    }
    public static class ProtocolHandler
    {
        /// <summary>
        /// Builds a message in the format "[2 Digit Protocol ID] [One Word Protocol Name]\r\n[content length]\r\n\r\n[Content]"
        /// </summary>
        /// <param name="protocolCode">The protocol code.</param>
        /// <param name="content">The message content.</param>
        /// <returns>A protocol compliant message.</returns>
        public static string BuildMessage(byte packetNumber, Protocol protocolCode, string content = "")
        {
            string code = ((int)protocolCode).ToString("00"); // gets the integer value of the enum as a two digit string
            return $"{packetNumber}~{code} {protocolCode.ToString()}\r\n{content.Length}\r\n\r\n{content}";
        }

        /// <summary>
        /// Interprets a protocol compliant message. Returns null if message is erroneous.
        /// </summary>
        /// <param name="message">The protocol compliant message.</param>
        /// <returns>The protocol used and the message content.</returns>
        public static Tuple<byte, Protocol, string> InterpretMessage(string message)
        {
            byte packetNumber;
            string content = string.Empty;
            Protocol protocol;

            try // Interpret message here
            {
                // PacketNumber
                int endOfPN = message.IndexOf('~');
                packetNumber = byte.Parse(message.Substring(0, endOfPN));
                message = message.Substring(endOfPN + 1);

                // Protocol ID
                int protocolID = int.Parse(message.Substring(0, 2));
                if (!Enum.IsDefined(typeof(Protocol), protocolID))
                    throw new InvalidCastException("Invalid Protocol Enum Value");
                protocol = (Protocol)protocolID;

                // Check protocol name is correct
                int pointer = message.IndexOf("\r\n");
                string protocolName = message.Substring(2, pointer - 2);
                if (!Enum.TryParse<Protocol>(protocolName, out _))
                    throw new InvalidCastException("Invalid Protocol Name");
                
                // Get Content Length
                pointer += 2;
                int pointer2 = message.IndexOf("\r\n\r\n");
                int contentLength = int.Parse(message.Substring(pointer, pointer2 - pointer));

                if (contentLength > 0)
                {
                    // Get content
                    pointer2 += 4;
                    content = message.Substring(pointer2);
                    if (content.Length != contentLength)
                        throw new Exception("Invalid message content. Data missing.");
                }
            }
            catch
            {
                return null;
            }

            return new Tuple<byte, Protocol, string>(packetNumber, protocol, content);
        }
    }
}
