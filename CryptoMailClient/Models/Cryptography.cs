using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CryptoMailClient.Models
{
    static class Cryptography
    {
        public static Encoding Encoding = Encoding.Unicode;
        public const string HEADER_ENCRYPTED = "X-Encrypted";
        public const string HEADER_SIGNED = "X-Signed";
        public const string HEADER_SIGNATURE = "X-Signature";

        #region Encryption/Decryption

        public static string EncryptData(in string data, string publicKey)
        {
            var bytes = EncryptAES(Encoding.GetBytes(data), publicKey);
            return Convert.ToBase64String(bytes);
        }

        public static byte[] EncryptAES(in byte[] bytes, string rsaPublicKey)
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

                using (MemoryStream inStream = new MemoryStream(bytes))
                {
                    int count;
                    do
                    {
                        count = inStream.Read(block, 0, blockSize);
                        outStreamEncrypted.Write(block, 0, count);
                    } while (count > 0);
                }

                // Обновляем исходный поток данных и очищаем буфер
                // (в методе FlushFinalBlock() также вызывается и Close()).
                outStreamEncrypted.FlushFinalBlock();
            }

            outStream.Close();

            return outStream.ToArray();
        }

        public static string DecryptData(in string data, string privateKey)
        {
            var bytes = DecryptAES(Convert.FromBase64String(data), privateKey);
            return Encoding.GetString(bytes);
        }

        public static byte[] DecryptAES(in byte[] bytes, string rsaPrivateKey)
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

                    // Обновляем исходный поток данных и очищаем буфер
                    // (в методе FlushFinalBlock() также вызывается и Close()).
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

        public static byte[] EncryptRSA(in byte[] data, string rsaPublicKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(rsaPublicKey);
            return rsa.Encrypt(data, false);
        }

        public static byte[] DecryptRSA(in byte[] data, string rsaPrivateKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(rsaPrivateKey);
            return rsa.Decrypt(data, false);
        }

        public static byte[] EncryptDPAPI(byte[] data, byte[] entropyBytes)
        {
            byte[] entropy = ComputeMD5Hash(entropyBytes);
            return ProtectedData.Protect(data,
                entropy, DataProtectionScope.CurrentUser);
        }

        public static byte[] DecryptDPAPI(byte[] data, byte[] entropyBytes)
        {
            byte[] entropy = ComputeMD5Hash(entropyBytes);
            return ProtectedData.Unprotect(data, entropy,
                DataProtectionScope.CurrentUser);
        }

        #endregion

        #region Signature

        public static string SignData(in string data, string rsaPrivateKey)
        {
            var signature = SignRSA(Encoding.GetBytes(data), rsaPrivateKey);
            return Convert.ToBase64String(signature);
        }

        public static byte[] SignRSA(in byte[] bytes, string rsaPrivateKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(rsaPrivateKey);

            RSAParameters rsaParams = rsa.ExportParameters(true);
            RSACng rsaAlgorithm = new RSACng();
            rsaAlgorithm.ImportParameters(rsaParams);

            return rsaAlgorithm.SignData(bytes, HashAlgorithmName.MD5,
                RSASignaturePadding.Pss);
        }

        public static bool VerifyData(in string data, string rsaPublicKey,
            string signature)
        {
            return VerifyRSA(Encoding.GetBytes(data), rsaPublicKey,
                Convert.FromBase64String(signature));
        }

        public static bool VerifyRSA(in byte[] bytes, string rsaPublicKey,
            byte[] signature)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(rsaPublicKey);

            RSAParameters rsaParams = rsa.ExportParameters(false);
            RSACng rsaAlgorithm = new RSACng();
            rsaAlgorithm.ImportParameters(rsaParams);

            return rsaAlgorithm.VerifyData(bytes, signature,
                HashAlgorithmName.MD5,
                RSASignaturePadding.Pss);
        }

        #endregion

        #region Hashing

        public static string ComputeHash(in string data)
        {
            var hash = ComputeMD5Hash(Encoding.GetBytes(data));
            return BitConverter.ToString(hash);
        }

        public static byte[] ComputeMD5Hash(in byte[] bytes)
        {
            var md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(bytes);
        }

        #endregion
    }
}
