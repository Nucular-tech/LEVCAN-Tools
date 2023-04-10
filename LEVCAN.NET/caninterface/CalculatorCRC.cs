using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEVCAN
{
    class CalculatorCRC
    {
        public static byte Calculate_CRC8(byte[] data)
        {
            return CRC8_Calculate__RefIn_false__RefOut_false(0x07, 0x00, 0x00, data, -1);
        }

        public static byte Calculate_CRC8_MAXIM(byte[] data)
        {
            return CRC8_Calculate__RefIn_true__RefOut_true(0x31, 0x00, 0x00, data);
        }

        public static byte Calculate_CRC8_ITU(byte[] data, int size = -1)
        {
            return CRC8_Calculate__RefIn_false__RefOut_false(0x07, 0x00, 0x55, data, size);
        }

        static byte CRC8_Calculate__RefIn_false__RefOut_false(byte poly, byte init, byte xorOut, byte[] data, int size)
        {
            byte crc8 = init;
            if (size == -1)
                size = data.Length;

            for (int i = 0; i < size; i++)
            {
                crc8 = (byte)(crc8 ^ data[i]);
                for (byte j = 0; j < 8; j++)
                {
                    if ((crc8 & 0x80) != 0)
                    {
                        crc8 = (byte)((crc8 << 1) ^ poly);
                    }
                    else
                    {
                        crc8 = (byte)(crc8 << 1);
                    }
                }
            }
            crc8 ^= xorOut;

            return crc8;
        }

        static byte CRC8_Calculate__RefIn_true__RefOut_true(byte poly, byte init, byte xorOut, byte[] data)
        {
            byte reverse_poly = 0;
            for (byte j = 0; j < 8; j++)
            {
                reverse_poly <<= 1;
                reverse_poly += (byte)(poly & 0x01);
                poly >>= 1;
            }

            byte crc8 = init;
            for (int i = 0; i < data.Length; i++)
            {
                crc8 = (byte)(crc8 ^ data[i]);

                for (byte j = 0; j < 8; j++)
                {
                    if ((crc8 & 0x01) != 0)
                    {
                        crc8 = (byte)((crc8 >> 1) ^ reverse_poly);
                    }
                    else
                    {
                        crc8 = (byte)(crc8 >> 1);
                    }
                }
            }

            crc8 ^= xorOut;

            return crc8;
        }
    }
}
