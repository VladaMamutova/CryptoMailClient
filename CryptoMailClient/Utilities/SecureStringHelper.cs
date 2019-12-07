using System;
using System.Runtime.InteropServices;
using System.Security;

namespace CryptoMailClient.Utilities
{
    public static class SecureStringHelper
    {
        /// <summary>
        /// Безопасно сравнивает два объекта SecureString путём операции XOR
        /// для каждой пары байтов, делая сравнение неуязвимым для временных атак.
        /// </summary>
        /// <param name="secureString1">Первый объект SecureString для сравнения.</param>
        /// <param name="secureString2">Второй объект SecureString для сравнения.</param>
        /// <returns>Возвращает булевое значение, показывающее, равны ли значения объектов.</returns>
        public static bool CompareSecureStrings(SecureString secureString1,
            SecureString secureString2)
        {
            bool result = false;

            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;
            try
            {
                bstr1 = Marshal.SecureStringToBSTR(secureString1);
                bstr2 = Marshal.SecureStringToBSTR(secureString2);

                var length1 = Marshal.ReadInt32(bstr1, -4);
                var length2 = Marshal.ReadInt32(bstr2, -4);

                var equal = 0;
                byte c2 = 0;

                // Чтобы нельзя было сравнить время работы алгоритма
                // при разных длинах значений и угадать длину исходного значения,
                // всегда будем сравнивать символы по длине исходного значения.
                // Если исходное значение больше сравниваемого,
                // то устанавливаем флаг, что значения были не равны,
                // и оставшиеся символы первого сравниваем с последним
                // символом второго значения.
                bool equalLength = length1 == length2;
                for (var i = 0; i < length1; i++)
                {
                    byte c1 = Marshal.ReadByte(bstr1 + i);
                    if (i < length2)
                    {
                        c2 = Marshal.ReadByte(bstr2 + i);
                    }

                    equal |= c1 ^ c2;
                }

                result = equal == 0 && equalLength;
            }
            finally
            {
                if (bstr1 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr1);
                }

                if (bstr2 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr2);
                }
            }

            return result;
        }

        public static string SecureStringToString(SecureString secureString)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString =
                    Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
