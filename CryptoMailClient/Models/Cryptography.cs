﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CryptoMailClient.Models
{
    static class Cryptography
    {
        public static byte[] EncryptAES(byte[] data, string rsaPublicKey)
        {
            AesManaged aes = new AesManaged
            {
                BlockSize = 128,
                KeySize = 256,
                Mode = CipherMode.CBC
            };

            aes.GenerateKey();
            aes.GenerateIV();

            byte[] keyEncrypted = EncryptRSA(aes.Key, rsaPublicKey);

            // Структура зашифрованных данных:
            // - длина ключа
            // - длина вектора инициализации IV
            // - зашифрованный ключ
            // - IV
            // - зашифрованный текст

            ICryptoTransform encryptor = aes.CreateEncryptor();

            MemoryStream outStream = new MemoryStream();

            byte[] keyLength = BitConverter.GetBytes(keyEncrypted.Length);
            byte[] ivLength = BitConverter.GetBytes(aes.IV.Length);
            outStream.Write(keyLength, 0, keyLength.Length);
            outStream.Write(ivLength, 0, ivLength.Length);
            outStream.Write(keyEncrypted, 0, keyEncrypted.Length);
            outStream.Write(aes.IV, 0, aes.IV.Length);

            using (CryptoStream outStreamEncrypted =
                new CryptoStream(outStream, encryptor, CryptoStreamMode.Write))
            {
                int blockSize = aes.BlockSize / 8;
                byte[] block = new byte[blockSize];

                using (MemoryStream inStream = new MemoryStream(data))
                {
                    int count;
                    do
                    {
                        count = inStream.Read(block, 0, blockSize);
                        outStreamEncrypted.Write(block, 0, count);
                    } while (count > 0);
                }

                // Обновляем исходный поток данных и очищаем буфер,
                // то есть в этом методе вызывается и Close().
                outStreamEncrypted.FlushFinalBlock();
            }

            outStream.Close();

            return outStream.ToArray();
        }

        public static byte[] DecryptAES(byte[] bytes, string rsaPrivateKey)
        {
            AesManaged aes = new AesManaged
            {
                BlockSize = 128,
                KeySize = 256,
                Mode = CipherMode.CBC
            };

            // Структура зашифрованных данных:
            // - длина ключа
            // - длина вектора инициализации IV
            // - зашифрованный ключ
            // - IV
            // - зашифрованный текст

            MemoryStream inStream = new MemoryStream(bytes);

            byte[] keyLength = new byte[4];
            byte[] ivLength = new byte[4];

            inStream.Read(keyLength, 0, keyLength.Length);
            inStream.Read(ivLength, 0, ivLength.Length);

            byte[] keyEncrypted =
                new byte[BitConverter.ToInt32(keyLength, 0)];
            byte[] iv = new byte[BitConverter.ToInt32(ivLength, 0)];

            inStream.Read(keyEncrypted, 0, keyEncrypted.Length);
            inStream.Read(iv, 0, iv.Length);

            byte[] keyDecrypted = DecryptRSA(keyEncrypted, rsaPrivateKey);

            ICryptoTransform decryptor = aes.CreateDecryptor(keyDecrypted, iv);

            MemoryStream outStream = new MemoryStream();
            int blockSize = aes.BlockSize / 8;
            byte[] block = new byte[blockSize];

            try
            {
                using (CryptoStream outStreamDecrypted =
                    new CryptoStream(outStream, decryptor,
                        CryptoStreamMode.Write))
                {
                    int count;
                    do
                    {
                        count = inStream.Read(block, 0, blockSize);
                        outStreamDecrypted.Write(block, 0, count);
                    } while (count > 0);

                    // Обновляем исходный поток данных и очищаем буфер,
                    // то есть в этом методе вызывается и Close().
                    outStreamDecrypted.FlushFinalBlock();
                }
            }
            catch (CryptographicException)
            {
                throw new CryptographicException(
                    "Невозможно расшифровать данные с заданным ключом.");
            }
            finally
            {
                outStream.Close();
                inStream.Close();
            }

            return outStream.ToArray();
        }

        public static byte[] EncryptRSA(byte[] data, string rsaPublicKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(rsaPublicKey);
            return rsa.Encrypt(data, false);
        }

        public static byte[] DecryptRSA(byte[] data, string rsaPrivateKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(rsaPrivateKey);
            return rsa.Decrypt(data, false);
        }

        public static string ComputeHash(string data)
        {
            var md5 = new MD5CryptoServiceProvider();
            var hash = md5.ComputeHash(Encoding.Unicode.GetBytes(data));
            return BitConverter.ToString(hash);
        }
    }
}