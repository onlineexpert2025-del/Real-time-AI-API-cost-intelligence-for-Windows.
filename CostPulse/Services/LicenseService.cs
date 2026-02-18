using System;
using System.Security.Cryptography;
using System.Text;

namespace CostPulse.Services
{
    public class LicenseService
    {
        private const string Salt = "CostPulseV2Salt"; // Simple salt for basic validation

        public bool DeveloperMode { get; set; } = false;

        public bool IsPro
        {
            get
            {
                // Portfolio Mode: Always Pro
                if (DeveloperMode) return true;

                // Real Validation Logic (Kept for architecture demo)
                return ValidateLicense(App.DataService?.Data?.Settings?.LicenseKey);
            }
        }

        public bool ValidateLicense(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            // Simple Format: PRO-{CHECKSUM}-{RANDOM}
            // Real implementation would use a server or stronger crypto.
            // For V1, we just check if it starts with PRO and has a valid checksum of the random part?
            // Or simplified: Just a hardcoded check or simple hash for now to demonstrate the gate.
            
            // Let's implement a simple Checksum validation:
            // Key format: PRO-XXXX-YYYY where YYYY is hash(XXXX + Salt) truncated
            
            var parts = key.Split('-');
            if (parts.Length != 3) return false;
            if (parts[0] != "PRO") return false;

            string randomPart = parts[1];
            string checksumPart = parts[2];

            string expectedChecksum = GenerateChecksum(randomPart);
            return checksumPart.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }

        public string GenerateKey(string randomPart)
        {
            // Helper for dev/testing to generate valid keys
            return $"PRO-{randomPart}-{GenerateChecksum(randomPart)}";
        }

        private string GenerateChecksum(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input + Salt);
                var hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 4);
            }
        }
    }
}
