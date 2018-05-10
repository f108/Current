using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ExternalMouse
{
    public class AES
    {
        AesCryptoServiceProvider AESProvider;

        public AES ()
        {
            AESProvider = new AesCryptoServiceProvider
            {
                KeySize = 256,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
        }

        public void SetCodeword(string code)
        {
            SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            AESProvider.Key = sha256.ComputeHash(UnicodeEncoding.Unicode.GetBytes(code));
        }

        public void SetKey(byte[] data)
        {
            AESProvider.Key = data;
        }

        public byte[] GenerateAndSetRandomKey()
        {
            //SHA256 sha256 = SHA256.Create();
            //Random rand = new Random();
            AESProvider.GenerateKey();
            //AESProvider.Key = sha256.ComputeHash(UnicodeEncoding.Unicode.GetBytes("@"+Environment.TickCount+"$"+rand.Next(1000,int.MaxValue)));
            return AESProvider.Key;
        }

        public byte[] Encrypt(byte[] data)
        {
            AESProvider.GenerateIV();
            ICryptoTransform encryptor = AESProvider.CreateEncryptor();
            byte[] ret = encryptor.TransformFinalBlock(data, 0, data.Length);
            ret = AESProvider.IV.Concat(ret).ToArray();
            return ret;
        }

        public byte[] Decrypt(byte[] data)
        {
            AESProvider.IV = data.Take(16).ToArray();
            ICryptoTransform decryptor = AESProvider.CreateDecryptor();
            byte[] ret = decryptor.TransformFinalBlock(data, 16, data.Length - 16);
            return ret;
        }
    }
}
