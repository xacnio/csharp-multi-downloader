using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace MultiDownload
{
    class Utils
    {
        public static string GetDefaultExtension(string mimeType)
        {
            string result;
            RegistryKey key;
            object value;

            key = Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type\" + mimeType, false);
            value = key != null ? key.GetValue("Extension", null) : null;
            result = value != null ? value.ToString() : string.Empty;

            return result;
        }

        public static string GetReadableFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int sizeIndex = 0;
            double size = bytes;

            while (size >= 1024 && sizeIndex < sizes.Length - 1)
            {
                size /= 1024;
                sizeIndex++;
            }

            return $"{size:0.##} {sizes[sizeIndex]}";
        }

        public static string FormatElapsedTime(long elapsedTime)
        {
            long totalSeconds = elapsedTime % 60;
            long totalMinutes = (elapsedTime / 60) % 60;
            long totalHours = (elapsedTime / (60 * 60)) % 24;
            long totalDays = elapsedTime / (24 * 60 * 60);

            string formattedTime = string.Empty;

            if (totalDays > 0)
            {
                formattedTime += totalDays + " gün, ";
            }

            if (totalHours > 0)
            {
                formattedTime += totalHours + " saat, ";
            }

            if (totalMinutes > 0)
            {
                formattedTime += totalMinutes + " dakika, ";
            }

            if (totalSeconds > 0)
            {
                formattedTime += totalSeconds + " saniye, ";
            }

            formattedTime = formattedTime.TrimEnd(',', ' ');

            return formattedTime;
        }
        public static bool IsValidURL(String url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) return true;
            return false;
        }
        public static void SendNotification(String text, String header, Icon icon)
        {
            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.Icon = icon;
            notifyIcon.Visible = true;
            notifyIcon.BalloonTipTitle = header;
            notifyIcon.BalloonTipText = text;
            notifyIcon.ShowBalloonTip(3000); // 3 saniye boyunca göster
        }

        public static string HashPassword(string password)
        {
            // Rastgele bir tuz oluşturun
            byte[] salt = new byte[16] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };

            // PBKDF2 algoritması ile şifrelemeyi gerçekleştirin
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            // Şifrelenmiş veriyi tuzla birleştirerek depolayın
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            // Karma işlemi sonucunu Base64 formatında döndürün
            return Convert.ToBase64String(hashBytes);
        }

        public static string GetDatabaseDirectory()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string newFolderPath = Path.Combine(documentsPath, "MultiDownload");
            newFolderPath = Path.Combine(newFolderPath, "Database");

            if (!Directory.Exists(newFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(newFolderPath);
                    return newFolderPath + "/";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Klasör oluşturma hatası: " + ex.Message);
                    return "MultiDownload/Database";
                }
            }
            else
            {
                return newFolderPath + "/";
            }
        }

        public static string GetDownloadDirectory()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string newFolderPath = Path.Combine(documentsPath, "MultiDownload");

            if (!Directory.Exists(newFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(newFolderPath);
                    return newFolderPath + "/";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Klasör oluşturma hatası: " + ex.Message);
                    return "MultiDownload/";
                }
            }
            else
            {
                return newFolderPath + "/";
            }
        }
    }
}
