using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GhostlySupaPoc.Models; // For FileUploadResult

namespace GhostlySupaPoc.Utils
{
    /// <summary>
    /// Custom DateTime converter for Supabase JSON responses
    /// Handles ISO 8601 date format with optional timezone info
    /// </summary>
    public class SupabaseDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string dateString = reader.GetString();

            if (string.IsNullOrEmpty(dateString))
                return DateTime.MinValue;

            // Try to parse various ISO 8601 formats that Supabase might return
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                return result;
            }

            // Fallback for specific Supabase formats
            if (DateTimeOffset.TryParse(dateString, out DateTimeOffset offsetResult))
            {
                return offsetResult.DateTime;
            }

            Console.WriteLine($"⚠️ Could not parse date: {dateString}");
            return DateTime.MinValue;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }

    /// <summary>
    /// Configuration and utility helper specifically for the legacy POCs,
    /// managing environment variables, file operations, and test data.
    /// </summary>
    public static class PocUtils
    {
        /// <summary>
        /// Get environment variable with fallback and validation
        /// </summary>
        /// <param name="key">Environment variable key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <param name="required">Whether this variable is required</param>
        /// <returns>Environment variable value or default</returns>
        public static string GetEnvironmentVariable(string key, string defaultValue = null, bool required = false)
        {
            var value = Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrEmpty(value))
            {
                if (required)
                {
                    throw new InvalidOperationException($"Required environment variable '{key}' is not set");
                }
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Validate that all required Supabase configuration is present
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public static bool ValidateSupabaseConfig(out string supabaseUrl, out string supabaseKey)
        {
            try
            {
                supabaseUrl = GetEnvironmentVariable("SUPABASE_URL", required: true);
                supabaseKey = GetEnvironmentVariable("SUPABASE_ANON_KEY", required: true);

                // Basic validation - Supabase URLs must use HTTPS
                if (!supabaseUrl.StartsWith("https://"))
                {
                    Console.WriteLine("❌ SUPABASE_URL must start with https://");
                    return false;
                }

                // Anon keys are JWT tokens and should be reasonably long
                if (supabaseKey.Length < 50)
                {
                    Console.WriteLine("❌ SUPABASE_ANON_KEY appears to be invalid (too short)");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Configuration error: {ex.Message}");
                supabaseUrl = null;
                supabaseKey = null;
                return false;
            }
        }

        /// <summary>
        /// Create a sample C3D file with dummy EMG data for testing purposes
        /// </summary>
        /// <param name="patientCode">Patient identifier for the sample data</param>
        /// <param name="outputDirectory">Directory to save the file (defaults to system temp)</param>
        /// <returns>Full path to the created file, or null if creation failed</returns>
        public static async Task<string> CreateSampleC3DFileAsync(string patientCode, string outputDirectory = null)
        {
            try
            {
                // Use system temp directory if none specified
                outputDirectory ??= Path.GetTempPath();

                // Ensure output directory exists
                Directory.CreateDirectory(outputDirectory);

                var fileName = $"sample_{patientCode}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(outputDirectory, fileName);

                // Generate dummy EMG sample data
                var content = $@"GHOSTLY+ Sample EMG Data
Patient: {patientCode}
Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}
Sampling Rate: 1000 Hz
Duration: 5 seconds
Channels: Biceps, Triceps, Forearm

Dummy Sample EMG readings:
Time(s), Biceps(µV), Triceps(µV), Forearm(µV)
0.000, 45.2, 32.1, 28.4
0.001, 48.6, 35.4, 31.2
0.002, 52.1, 29.8, 29.7
0.003, 49.3, 38.7, 33.1
0.004, 46.8, 33.2, 30.5
0.005, 51.4, 31.9, 32.8
0.006, 47.9, 36.3, 28.9
0.007, 53.2, 34.1, 31.4
0.008, 44.7, 37.8, 29.3
0.009, 50.1, 30.6, 32.7

--- Game Session Data ---
Level: 1
Score: 750
Duration: 300 seconds
Max Voluntary Contraction: 85%
Total Contractions: 127
Average Contraction Time: 2.3s
Session Type: Autonomous Training
Device: Android Tablet (GHOSTLY+ v1.2)
";

                await File.WriteAllTextAsync(filePath, content);
                Console.WriteLine($"📄 Created sample file: {fileName}");
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ File creation error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ensure a directory exists, creating it if necessary
        /// </summary>
        /// <param name="directoryPath">Path to directory</param>
        /// <returns>True if directory exists or was created successfully</returns>
        public static bool EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Console.WriteLine($"📁 Created directory: {directoryPath}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create directory {directoryPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate a test patient code for demonstrations
        /// </summary>
        /// <param name="prefix">Prefix for patient code (default: "P")</param>
        /// <param name="number">Patient number (default: random 001-999)</param>
        /// <returns>Patient code like "P001", "P042", etc.</returns>
        public static string GenerateTestPatientCode(string prefix = "P", int? number = null)
        {
            var patientNumber = number ?? new Random().Next(1, 1000);
            return $"{prefix}{patientNumber:D3}";
        }

        /// <summary>
        /// Clean up test files and directories created during POC runs
        /// </summary>
        /// <param name="directory">Directory to clean (default: "./c3d-test-download")</param>
        /// <returns>Number of files deleted</returns>
        public static int CleanupTestFiles(string directory = "./c3d-test-download")
        {
            try
            {
                if (!Directory.Exists(directory))
                    return 0;

                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                // Remove empty directories
                if (Directory.GetDirectories(directory).Length == 0 &&
                    Directory.GetFiles(directory).Length == 0)
                {
                    Directory.Delete(directory);
                }

                Console.WriteLine($"🧹 Cleaned up {files.Length} test files from {directory}");
                return files.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Cleanup error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Display a summary of the POC test results
        /// </summary>
        /// <param name="supabaseSuccess">Whether Supabase client test succeeded</param>
        /// <param name="httpSuccess">Whether HTTP client test succeeded</param>
        /// <param name="patientCode">Patient code used in tests</param>
        public static void DisplayTestSummary(bool supabaseSuccess, bool httpSuccess, string patientCode)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("*** GHOSTLY+ POC TEST SUMMARY ***");
            Console.WriteLine(new string('=', 60));

            Console.WriteLine($"> Test Configuration:");
            Console.WriteLine($"   Patient Code: {patientCode}");
            Console.WriteLine($"   Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            Console.WriteLine($"> Results:");
            Console.WriteLine($"   🔵 Supabase Client: {(supabaseSuccess ? "✅ SUCCESS" : "❌ FAILED")}");
            Console.WriteLine($"   🟠 HTTP Client:     {(httpSuccess ? "✅ SUCCESS" : "❌ FAILED")}");
            Console.WriteLine();

            if (supabaseSuccess && httpSuccess)
            {
                Console.WriteLine("🎉 Both implementations work as expected !");
                Console.WriteLine($"📁 Patient {patientCode} files are properly organized in Supabase Storage");
            }
            else if (supabaseSuccess || httpSuccess)
            {
                Console.WriteLine("⚠️ PARTIAL SUCCESS - One implementation needs attention");
                var working = supabaseSuccess ? "Supabase Client" : "HTTP Client";
                var failing = supabaseSuccess ? "HTTP Client" : "Supabase Client";
                Console.WriteLine($"✅ {working} is working perfectly");
                Console.WriteLine($"❌ {failing} needs debugging - check logs above");
            }
            else
            {
                Console.WriteLine("❌ CONFIGURATION ISSUE - Both implementations failed");
                Console.WriteLine("💡 Check your Supabase configuration:");
                Console.WriteLine("   • Verify SUPABASE_URL and SUPABASE_ANON_KEY");
                Console.WriteLine("   • Ensure 'c3d-files' bucket exists");
                Console.WriteLine("   • Verify user authentication in Supabase");
            }

            Console.WriteLine(new string('=', 60));
        }
    }
}