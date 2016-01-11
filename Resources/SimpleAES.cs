using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AutoFollow.Resources
{
    public class SimpleAES
    {
        private readonly ICryptoTransform _decryptorTransform;
        private readonly ICryptoTransform _encryptorTransform;
        private readonly byte[] _key = { 123, 212, 19, 101, 24, 26, 85, 134, 114, 184, 27, 1, 37, 2, 222, 5, 241, 1, 175, 144, 6, 53, 8, 29, 24, 1, 17, 218, 125, 4, 53, 209};
        private readonly byte[] _vector = { 146, 64, 112, 111, 123, 1, 121, 119, 231, 221, 12, 112, 54, 7, 114, 82 };
        private readonly UTF8Encoding _utfEncoder;

        public SimpleAES()
        {
            var rm = new RijndaelManaged();
            _encryptorTransform = rm.CreateEncryptor(_key, _vector);
            _decryptorTransform = rm.CreateDecryptor(_key, _vector);
            _utfEncoder = new UTF8Encoding();
        }

        public static byte[] GenerateEncryptionKey()
        {
            var rm = new RijndaelManaged();
            rm.GenerateKey();
            return rm.Key;
        }

        public static byte[] GenerateEncryptionVector()
        {
            var rm = new RijndaelManaged();
            rm.GenerateIV();
            return rm.IV;
        }

        public string EncryptToString(string textValue)
        {
            return ByteArrToString(Encrypt(textValue));
        }

        public byte[] Encrypt(string textValue)
        {
            Byte[] bytes = _utfEncoder.GetBytes(textValue);
            var memoryStream = new MemoryStream();
            var cs = new CryptoStream(memoryStream, _encryptorTransform, CryptoStreamMode.Write);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();
            memoryStream.Position = 0;
            var encrypted = new byte[memoryStream.Length];
            memoryStream.Read(encrypted, 0, encrypted.Length);
            cs.Close();
            memoryStream.Close();
            return encrypted;
        }

        public string DecryptString(string encryptedString)
        {
            return Decrypt(StrToByteArray(encryptedString));
        }

        public string Decrypt(byte[] encryptedValue)
        {
            var encryptedStream = new MemoryStream();
            var decryptStream = new CryptoStream(encryptedStream, _decryptorTransform, CryptoStreamMode.Write);
            decryptStream.Write(encryptedValue, 0, encryptedValue.Length);
            decryptStream.FlushFinalBlock();
            encryptedStream.Position = 0;
            var decryptedBytes = new Byte[encryptedStream.Length];
            encryptedStream.Read(decryptedBytes, 0, decryptedBytes.Length);
            encryptedStream.Close();
            return _utfEncoder.GetString(decryptedBytes);
        }

        public byte[] StrToByteArray(string str)
        {
            if (str.Length == 0)
                throw new Exception("Invalid string value in StrToByteArray");
            var byteArr = new byte[str.Length/3];
            var i = 0;
            var j = 0;
            do {
                byte val = byte.Parse(str.Substring(i, 3));
                byteArr[j++] = val;
                i += 3;
            } while (i < str.Length);
            return byteArr;
        }
 
        public string ByteArrToString(byte[] byteArr)
        {
            var tempStr = "";
            for (var i = 0; i <= byteArr.GetUpperBound(0); i++)
            {
                byte val = byteArr[i];
                if (val < 10)
                    tempStr += "00" + val;
                else if (val < 100)
                    tempStr += "0" + val;
                else
                    tempStr += val.ToString(CultureInfo.InvariantCulture);
            }
            return tempStr;
        }
    }
}