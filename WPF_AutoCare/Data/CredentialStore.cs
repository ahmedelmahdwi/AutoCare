using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace C__project_Auto_Care.Data
{
    /// <summary>
    /// Secure local credential storage using Windows DPAPI (Data Protection API).
    /// Bound to the current Windows user profile.
    /// </summary>
    public static class CredentialStore
    {
        private static readonly string StoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoCare",
            "remember.dat");

        public static void SaveCredentials(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username)) return;

                string dir = Path.GetDirectoryName(StoragePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string payload = username + "||" + (password ?? "");
                byte[] plainBytes = Encoding.UTF8.GetBytes(payload);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(StoragePath, encryptedBytes);
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Could not save remembered credentials: " + ex.Message, "CredentialStore");
            }
        }

        public static bool LoadCredentials(out string username, out string password)
        {
            username = "";
            password = "";

            try
            {
                if (!File.Exists(StoragePath))
                    return false;

                byte[] encryptedBytes = File.ReadAllBytes(StoragePath);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                string payload = Encoding.UTF8.GetString(plainBytes);

                string[] parts = payload.Split(new string[] { "||" }, StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    username = parts[0];
                    password = parts[1];
                    return !string.IsNullOrEmpty(username);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warn("Could not load remembered credentials: " + ex.Message, "CredentialStore");
                ClearCredentials();
            }

            return false;
        }

        public static void ClearCredentials()
        {
            try
            {
                if (File.Exists(StoragePath))
                    File.Delete(StoragePath);
            }
            catch { }
        }
    }
}
