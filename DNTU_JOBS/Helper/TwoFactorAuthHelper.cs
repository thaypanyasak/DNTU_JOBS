using OtpNet;
using System.Drawing.Imaging;
using System.Drawing;
using ZXing.Common;
using ZXing;

namespace DNTU_JOBS.Helper
{
    public static class TwoFactorAuthHelper
    {
        /// <summary>
        /// Generate a One-Time Password (OTP) using a secret key.
        /// </summary>
        public static string GenerateOtp(string secret)
        {
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            return totp.ComputeTotp();
        }

        /// <summary>
        /// Verify if the OTP code is valid.
        /// </summary>
        public static bool IsValidOtp(string key, string code)
        {
            var totp = new Totp(Base32Encoding.ToBytes(key), step: 30, totpSize: 6);
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }

        /// <summary>
        /// Generate a QR Code in Base64 format for display.
        /// </summary>
        public static string GenerateQrCodeBase64(string content)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 200,
                    Width = 200,
                    Margin = 0
                }
            };

            var pixelData = writer.Write(content);

            using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb))
            {
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                                                 ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return $"data:image/png;base64,{Convert.ToBase64String(stream.ToArray())}";
                }
            }
        }

        /// <summary>
        /// Format a secret key for display.
        /// </summary>
        public static string FormatKey(string key)
        {
            var result = new System.Text.StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < key.Length)
            {
                result.Append(key.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < key.Length)
            {
                result.Append(key.Substring(currentPosition));
            }
            return result.ToString().ToLowerInvariant();
        }
    }
}
