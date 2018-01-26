using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDP_Messaging
{
    internal static class HammingEncoder
    {
        public static byte[] EncodeBits(byte[] bytes)
        {
            var messageBits = new BitArray(bytes);

            int remainder = messageBits.Length % 4,
                iterations = (short)(messageBits.Length / 4),
                storageBits = 4 - remainder,
                arraySize = iterations * 7;

            if (remainder > 0)
                arraySize += 7;
            BitArray returnArray = new BitArray(arraySize);

            // iterate through each group of 4
            BitArray bitset;
            for (int i = 0; i < iterations; i++)
            {
                int c = i * 4;
                bitset = new BitArray(7, false)
                {
                    [0] = messageBits[c],
                    [1] = messageBits[c + 1],
                    [2] = messageBits[c + 2],
                    [3] = messageBits[c + 3]
                };

                Encode(ref bitset);
                int c2 = i * 7;
                for (byte j = 0; j < 7; j++)
                {
                    returnArray[c2 + j] = bitset[j];
                }
            }

            // then do left overs
            if (remainder > 0)
            {
                int c = iterations * 4, c2 = iterations * 7;
                bitset = new BitArray(7, false);
                for (byte i = 0; i < remainder; i++)
                {
                    bitset[i] = messageBits[c + i];
                }
                Encode(ref bitset);
                for (byte j = 0; j < 7; j++)
                {
                    returnArray[c2 + j] = bitset[j];
                }
            }
            
            double size = Math.Ceiling(returnArray.Length / 8.0);
            byte[] encodedBytes = new byte[(int)size];
            returnArray.CopyTo(encodedBytes, 0);
            return encodedBytes;
        }
        public static byte[] DecodeBits(byte[] bytes)
        {
            var encodedBits = new BitArray(bytes);

            // truncate (round down) - remove empty bits off the end
            int size = (int)((double)encodedBits.Length / 7.0);
            int outputArraySize = size * 4;
            BitArray decodedBits = new BitArray(outputArraySize);

            BitArray pBitset;
            for (ushort i = 0; i < size; i++)
            {
                int c = i * 7, c2 = i * 4;
                pBitset = new BitArray(7, false)
                {
                    [0] = encodedBits[c],
                    [1] = encodedBits[c + 1],
                    [2] = encodedBits[c + 2],
                    [3] = encodedBits[c + 3],
                    [4] = encodedBits[c + 4],
                    [5] = encodedBits[c + 5],
                    [6] = encodedBits[c + 6]
                };

                ErrorProcessor(ref pBitset);

                decodedBits[c2] = pBitset[0];
                decodedBits[c2 + 1] = pBitset[1];
                decodedBits[c2 + 2] = pBitset[2];
                decodedBits[c2 + 3] = pBitset[3];
            }

            byte[] decodedBytes = new byte[decodedBits.Length / 8];
            decodedBits.CopyTo(decodedBytes, 0);
            return decodedBytes;
        }
        private static void Encode(ref BitArray bitset)
        {
            bool x = false, y = false, z = false;
            bool a = bitset[0],
                b = bitset[1],
                c = bitset[2],
                d = bitset[3];

            x = a ^ c ^ d;
            y = a ^ b ^ c;
            z = b ^ c ^ d;

            bitset[4] = x;
            bitset[5] = y;
            bitset[6] = z;
        }
        private static void ErrorProcessor(ref BitArray bitset)
        {
            bool x0 = bitset[4] != bitset[0] ^ bitset[2] ^ bitset[3],
            y0 = bitset[5] != bitset[0] ^ bitset[1] ^ bitset[2],
            z0 = bitset[6] != bitset[1] ^ bitset[2] ^ bitset[3];

            if (x0 && y0 && z0)
                bitset[2] = bitset[2] == true ? false : true;
            else if (x0 && y0)
                bitset[0] = bitset[0] == true ? false : true;
            else if (x0 && z0)
                bitset[3] = bitset[3] == true ? false : true;
            else if (y0 && z0)
                bitset[1] = bitset[1] == true ? false : true;
        }
    }
}
