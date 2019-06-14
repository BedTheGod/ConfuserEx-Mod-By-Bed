using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Confuser.Runtime
{
    internal static class Integrity
    {
        internal static void Initialize()
        {
            var bas = new StreamReader(typeof(Integrity).Assembly.Location).BaseStream;
            var file = new BinaryReader(bas);
            var file2 = File.ReadAllBytes(typeof(Integrity).Assembly.Location);

            var byt = file.ReadBytes(file2.Length - 32);
            var a = Hash(byt);
            file.BaseStream.Position = file.BaseStream.Length - 32;
            string b = Encoding.ASCII.GetString(file.ReadBytes(32));

            if (a != b)
                throw new BadImageFormatException();
        }

        internal static string Hash(byte[] metin)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] btr = metin;
            btr = md5.ComputeHash(btr);
            StringBuilder sb = new StringBuilder();

            foreach (byte ba in btr)
            {
                sb.Append(ba.ToString("x2").ToLower());
            }
            return sb.ToString();
        }
    }
}
