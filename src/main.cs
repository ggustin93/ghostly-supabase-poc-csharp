using System;
using System.IO;
using System.Threading.Tasks;
using GhostlySupaPoc.Clients;
using GhostlySupaPoc.Config;
using GhostlySupaPoc.Examples;
using GhostlySupaPoc.RlsTests;
using GhostlySupaPoc.Utils;

namespace GhostlySupaPoc
{
    /// <summary>
    /// Main program for the GHOSTLY+ Proof of Concept.
    /// Simple, clean, pragmatic - KISS principle applied.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("GHOSTLY+ Supabase C# Client POC");
            Console.WriteLine("================================\n");
            
            // Simple configuration
            var config = SimpleConfig.Instance;
            Console.WriteLine(config.GetStatusMessage());
            
            if (!config.IsValid())
            {
                bool isReplIt = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REPLIT_DB_URL"));
                
                if (isReplIt)
                {
                    Console.WriteLine("\n💡 To fix on Repl.it:");
                    Console.WriteLine("   1. Click Tools icon → Secrets");
                    Console.WriteLine("   2. Add SUPABASE_URL and SUPABASE_KEY");
                }
                else
                {
                    Console.WriteLine("\n💡 To fix locally:");
                    Console.WriteLine("   1. Edit appsettings.json with your Supabase credentials");
                    Console.WriteLine("   2. Or set environment variables: SUPABASE_URL and SUPABASE_KEY");
                }
                
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
                return;
            }

            try
            {
                await RunMainMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        private static async Task RunMainMenu()
        {
            Console.WriteLine("\nSelect an option:");
            Console.WriteLine("  1. Official Supabase C# Client");
            Console.WriteLine("  2. Raw HTTP API Client");
            Console.WriteLine("  3. Both (Side-by-Side Comparison)");
            Console.WriteLine("  4. Cleanup Local Test Files");
            Console.WriteLine("  5. Multi-Therapist RLS Test Suite 🔒");
            Console.WriteLine("  6. Mobile Upload Example 📱");
            Console.Write("Enter choice (1-6): ");
            
            var choice = Console.ReadLine();
            Console.WriteLine();

            string email = null;
            string password = null;

            if (choice == "1" || choice == "2" || choice == "3")
            {
                var cfg = SimpleConfig.Instance;
                Console.WriteLine("Select Authentication:");
                Console.WriteLine("  1. Enter credentials manually");
                Console.WriteLine($"  2. Use test user ({cfg.TestTherapistEmail})");
                Console.Write("  Choice (1/2): ");
                
                var authChoice = Console.ReadLine();

                if (authChoice == "1")
                {
                    Console.Write("\n  Email: ");
                    email = Console.ReadLine();
                    Console.Write("  Password: ");
                    password = Console.ReadLine();
                }
                else
                {
                    email = cfg.TestTherapistEmail;
                    password = cfg.TestTherapistPassword;
                    Console.WriteLine($"\n-> Using test user: {email}\n");
                }
            }

            switch (choice)
            {
                case "1":
                    await ExecuteSingleClientTest(ClientType.Supabase, email, password);
                    break;
                case "2":
                    await ExecuteSingleClientTest(ClientType.Http, email, password);
                    break;
                case "3":
                    await ExecuteComparisonTest(email, password);
                    break;
                case "4":
                    CleanupTestFiles();
                    break;
                case "5":
                    await RunRlsPoc();
                    break;
                case "6":
                    await RunMobileUploadExample();
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
        
        private enum ClientType { Supabase, Http }

        private static ISupaClient CreateClient(ClientType type, string bucket)
        {
            var config = SimpleConfig.Instance;
            return type == ClientType.Supabase
                ? new SupabaseClient(config.SupabaseUrl, config.SupabaseKey, bucket)
                : new CustomHttpClient(config.SupabaseUrl, config.SupabaseKey, bucket);
        }

        private static async Task ExecuteComparisonTest(string email, string password)
        {
            Console.WriteLine("🔄 Comparison Mode - Testing Both Implementations");
            Console.WriteLine("=================================================\n");
            
            var patientCode = GetPatientCodeFromUser();
            var config = SimpleConfig.Instance;

            Console.WriteLine("🔵 ROUND 1: Official Supabase C# Client");
            Console.WriteLine("---------------------------------------");
            using (var supabaseClient = CreateClient(ClientType.Supabase, config.BucketName))
            {
                await RunTestSequence(supabaseClient, email, password, patientCode);
            }

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            Console.WriteLine("🟠 ROUND 2: Raw HTTP API Client");
            Console.WriteLine("-------------------------------");
            using (var httpClient = CreateClient(ClientType.Http, config.BucketName))
            {
                await RunTestSequence(httpClient, email, password, patientCode);
            }
        }

        private static async Task ExecuteSingleClientTest(ClientType clientType, string email, string password)
        {
            var config = SimpleConfig.Instance;
            var patientCode = GetPatientCodeFromUser();
            
            Console.WriteLine($"Testing {clientType} Client");
            Console.WriteLine(new string('-', 30));

            using (var client = CreateClient(clientType, config.BucketName))
            {
                await RunTestSequence(client, email, password, patientCode);
            }
        }

        private static async Task<bool> RunTestSequence(ISupaClient client, string email, string password, string patientCode)
        {
            try
            {
                // 1. Authenticate
                Console.WriteLine("🔐 Authenticating...");
                if (!await client.AuthenticateAsync(email, password))
                {
                    Console.WriteLine("❌ Authentication failed");
                    return false;
                }

                // 2. Upload test file
                Console.WriteLine($"\n📤 Uploading test file for patient {patientCode}...");
                var testFile = CreateTestFile();
                var result = await client.UploadFileAsync(patientCode, testFile);
                
                if (result != null)
                {
                    Console.WriteLine($"✅ Uploaded: {result.FileName}");
                }

                // 3. List files
                Console.WriteLine("\n📋 Listing files...");
                var files = await client.ListFilesAsync(patientCode);
                Console.WriteLine($"Found {files.Count} files");

                // 4. Sign out
                await client.SignOutAsync();
                Console.WriteLine("✅ Signed out");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return false;
            }
        }

        private static string GetPatientCodeFromUser()
        {
            Console.Write("Enter patient code (e.g., P001) or press Enter for P000 (test): ");
            var input = Console.ReadLine();
            return string.IsNullOrWhiteSpace(input) ? "P000" : input;
        }

        private static string CreateTestFile()
        {
            var fileName = $"test_{DateTime.Now:yyyyMMdd_HHmmss}.c3d";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            
            // Create minimal valid C3D file (512 bytes minimum)
            var c3dData = new byte[512];
            c3dData[1] = 0x50; // C3D parameter section indicator
            File.WriteAllBytes(filePath, c3dData);
            
            return filePath;
        }

        private static void CleanupTestFiles()
        {
            Console.WriteLine("🧹 Cleaning up test files...");
            var tempPath = Path.GetTempPath();
            var testFiles = Directory.GetFiles(tempPath, "test_*.c3d");
            
            foreach (var file in testFiles)
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"   Deleted: {Path.GetFileName(file)}");
                }
                catch { }
            }
            
            Console.WriteLine($"✅ Cleaned up {testFiles.Length} files");
        }

        private static async Task RunRlsPoc()
        {
            Console.WriteLine("🔒 Multi-Therapist RLS Test Suite");
            Console.WriteLine("==================================\n");
            
            try
            {
                var config = SimpleConfig.Instance;
                
                // Validate that we have both therapist credentials
                if (string.IsNullOrEmpty(config.TestTherapistEmail) || string.IsNullOrEmpty(config.TestTherapistPassword) ||
                    string.IsNullOrEmpty(config.Therapist2Email) || string.IsNullOrEmpty(config.Therapist2Password))
                {
                    Console.WriteLine("❌ Missing therapist credentials. Please configure:");
                    Console.WriteLine("   TEST_THERAPIST_EMAIL, TEST_THERAPIST_PASSWORD");
                    Console.WriteLine("   THERAPIST2_EMAIL, THERAPIST2_PASSWORD");
                    return;
                }
                
                Console.WriteLine($"👤 Therapist 1: {config.TestTherapistEmail}");
                Console.WriteLine($"👤 Therapist 2: {config.Therapist2Email}");
                Console.WriteLine($"🗄️  Test Bucket: {config.BucketName}\n");
                
                // Run comprehensive multi-therapist RLS test
                var supabase = new Supabase.Client(config.SupabaseUrl, config.SupabaseKey);
                
                await RlsTests.MultiTherapistRlsTests.RunAllTests(
                    supabase,
                    config.TestTherapistEmail,
                    config.TestTherapistPassword,
                    config.Therapist2Email,
                    config.Therapist2Password,
                    config.BucketName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ RLS test suite failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static async Task RunMobileUploadExample()
        {
            await Examples.MobileUploadExample.RunExample();
        }
    }
}