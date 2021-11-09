/*
    FDSRem - C&C Renegade FDS Communicator Library
    Copyright (C) 2021 Unstoppable

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
    See the LICENSE file for more details.
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FDSRem
{
    internal static class CryptographyClass
    {
        public static string Password = "password";

        public static byte[] Encrypt(string Text)
        {
            string tmp = new string('\0', 4) + Text + "\0";

            byte[] buf = Encoding.ASCII.GetBytes(tmp);
            byte[] pwd = Encoding.ASCII.GetBytes(Password);

            for (int i = 0; i < buf.Length; i++)
            {
                pwd[i % 8] ^= (buf[i] = (byte)((((0xFF00 | (buf[i] + i)) - 0x32) & 0xFF) ^ pwd[i % 8]));
            }


            byte[] res = new byte[Text.Length + 9];
            Array.Copy(BitConverter.GetBytes(GetChecksum(buf)), 0, res, 0, 4);
            Array.Copy(buf, 0, res, 4, buf.Length);

            return res;
        }

        public static string Decrypt(byte[] Data)
        {
            byte[] buf = Data.Skip(4).ToArray();
            byte[] sum = Data.Take(4).ToArray();

            if (GetChecksum(buf) != BitConverter.ToUInt32(sum))
            {
                throw new CryptographicException("Checksums mismatch.");
            }

            byte[] pwd = Encoding.ASCII.GetBytes(Password);

            for (int i = 0; i < buf.Length; i++)
            {
                byte b = pwd[i % 8];
                pwd[i % 8] = (byte)(buf[i] ^ pwd[i % 8]);
                buf[i] = (byte)((buf[i] ^ b) - i + 0x32);
            }

            return Encoding.ASCII.GetString(buf.Skip(4)
                                               .SkipLast(1)
                                               .ToArray());
        }

        public static uint GetChecksum(byte[] Data)
        {
            ulong Checksum = 0;

            for (int i = 0; i < Data.Length; i += 4)
            {
                Checksum = (Checksum >> 0x1F) + Checksum * 2;

                var Temp = Data;

                if (i + 4 > Data.Length)
                {
                    Array.Resize(ref Temp, Data.Length + 3);
                }

                byte[] New = new byte[4];

                var Skipped = Temp.Skip(i).ToArray();
                Skipped = Skipped.Length <= 4 ? Skipped : Skipped.Take(4).ToArray();
                Array.Copy(Skipped, New, Skipped.Length);

                Checksum += BitConverter.ToUInt32(New, 0);

                while (Checksum > Math.Pow(2, 32))
                {
                    Checksum -= (ulong)Math.Pow(2, 32);
                }
            }

            return (uint)Checksum;
        }
    }
}
