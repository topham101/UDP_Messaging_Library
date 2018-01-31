using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDP_Messaging
{
    public class PacketChecker
    {
        private const byte maxBufferSize = 255;
        private const byte maxDistance = 128;

        private byte lastReceived;
        private bool anyPacketsReceived;

        private byte currentPacketNumber = 0;

        public event ConnectionIssuesEvent ConnectionIssues;
        
        /// <summary>
        /// Checks if the packet is a duplicate or old.
        /// </summary>
        /// <param name="packetNumber">The packet number of the last received packet.</param>
        /// <returns>Returns true if the packet is valid.</returns>
        public bool CheckPacket(byte packetNumber)
        {
            if (!anyPacketsReceived)
            {
                anyPacketsReceived = true;
                lastReceived = packetNumber;
                return true;
            }

            int distance_1 = (packetNumber - lastReceived);
            bool isGreater_1 = packetNumber > lastReceived;

            // If packet number is greater and hasnt cycled past 255
            if (isGreater_1 && distance_1 < maxDistance)
            {
                if (distance_1 > 16)
                {
                    ConnectionIssues?.Invoke(ConnectionIssue.SuspectedPacketLoss);
                }
                lastReceived = packetNumber;
                return true;
            }

            int packetNumber_2 = packetNumber + 256,
                distance_2 = (packetNumber_2 - lastReceived);
            bool isGreater_2 = packetNumber_2 > lastReceived;

            // If packet number is greater and has cycled past 255
            if (isGreater_2 && distance_2 < maxDistance)
            {
                if (distance_2 > 16)
                {
                    ConnectionIssues?.Invoke(ConnectionIssue.SuspectedPacketLoss);
                }
                lastReceived = packetNumber;
                return true;
            }

            return false;
        }

        public byte GetPacketNumber(bool increment = true)
        {
            byte packetNumber = currentPacketNumber;

            // Increment Packet Number
            if (increment)
            {
                if (currentPacketNumber == 255)
                    currentPacketNumber = 0;
                else currentPacketNumber++; 
            }

            return packetNumber;
        }

        public void Reset()
        {
            lastReceived = 0;
            currentPacketNumber = 0;
            anyPacketsReceived = false;
        }
    }
}
