using System;
using System.Security.Cryptography;
using System.Text;

namespace MoveleirosChatServer.Utils
{
    public static class PasswordHash
    {
        public static bool PasswordsMatch(string dbPassword, string enteredPassword, string salt)
        {
            if (string.IsNullOrEmpty(dbPassword) || string.IsNullOrEmpty(enteredPassword))
                return false;

            var savedPassword = CreatePasswordHash(enteredPassword, salt);

            return dbPassword.Equals(savedPassword);
        }

        private static string CreatePasswordHash(string enteredPassword, string salt)
        {
            return CreateHash(Encoding.UTF8.GetBytes(String.Concat(enteredPassword, salt)), "SHA1");
        }

        private static string CreateHash(byte[] data, string hashAlgorithm = "SHA1")
        {
            if (String.IsNullOrEmpty(hashAlgorithm))
                hashAlgorithm = "SHA1";

            var algorithm = SHA1.Create();
            if (algorithm == null)
                throw new ArgumentException("Unrecognized hash name");

            var hashByteArray = algorithm.ComputeHash(data);
            return BitConverter.ToString(hashByteArray).Replace("-", "");
        }
    }
}
