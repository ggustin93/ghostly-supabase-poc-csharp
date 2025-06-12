using System;
using System.IO;
using System.Threading.Tasks;
using GhostlySupaPoc.Clients;
using GhostlySupaPoc.RlsTests;
using GhostlySupaPoc.Utils;

namespace GhostlySupaPoc
{
    /// <summary>
    /// Main program demonstrating GHOSTLY+ Supabase integration comparing two approaches:
    /// 1. Official Supabase C# Client
    /// 2. Raw HTTP API calls
    /// Both with patient-specific subfolders and comprehensive testing
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🎮 GHOSTLY+ Supabase C# Client Comparison POC");
            Console.WriteLine("=============================================\n");

            // Initialize Supabase connection
            if (!PocUtils.ValidateSupabaseConfig(out string supabaseUrl, out string supabaseKey))
            {
                Console.WriteLine("❌ Missing configuration! Please check the README.md for setup instructions.");
                Console.WriteLine("\n💡 Setup Required:");
                Console.WriteLine("   • Set SUPABASE_URL environment variable");
                Console.WriteLine("   • Set SUPABASE_ANON_KEY environment variable");
                Console.WriteLine("   • Create 'c3d-files' bucket in Supabase Storage");
                Console.WriteLine("   • Create test user in Supabase Authentication");
                Console.WriteLine("\n📋 Press any key to exit...");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("✅ Connected to Supabase\n");

            try
            {
                // Ask user which implementation to test
                Console.WriteLine("🔧 Choose Implementation to Test:");
                Console.WriteLine("1️⃣ Official Supabase C# Client (Legacy)");
                Console.WriteLine("2️⃣ Raw HTTP API Client (Legacy)");
                Console.WriteLine("3️⃣ Both (Comparison Mode)");
                Console.WriteLine("4️⃣ Cleanup Test Files");
                Console.WriteLine("5️⃣ Multi-Therapist RLS Test");
                Console.Write("Choice (1/2/3/4/5): ");
                var choice = Console.ReadLine();
                Console.WriteLine();

                string email = null;
                string password = null;

                if (choice == "1" || choice == "2" || choice == "3")
                {
                    // Get user credentials for testing
                    Console.WriteLine("👨‍⚕️ Therapist Authentication");
                    Console.Write("Email: ");
                    email = Console.ReadLine();
                    Console.Write("Password: ");
                    password = Console.ReadLine();
                    Console.WriteLine();
                }

                switch (choice)
                {
                    case "1":
                        await TestSupabaseClient(supabaseUrl, supabaseKey, email, password);
                        break;
                    case "2":
                        await TestHttpClient(supabaseUrl, supabaseKey, email, password);
                        break;
                    case "3":
                        await TestBothClients(supabaseUrl, supabaseKey, email, password);
                        break;
                    case "4":
                        CleanupTestFiles();
                        break;
                    case "5":
                        await RunRlsPoc(supabaseUrl, supabaseKey);
                        break;
                    default:
                        Console.WriteLine("❌ Invalid choice.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Critical Error: {ex.Message}");
                Console.WriteLine($"💡 Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\n📋 Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Test using official Supabase C# Client
        /// </summary>
        private static async Task<bool> TestSupabaseClient(string supabaseUrl, string supabaseKey, string email, string password)
        {
            Console.WriteLine("🔵 Testing Official Supabase C# Client");
            Console.WriteLine("=====================================\n");

            var ghostly = new LegacySupabaseClient(supabaseUrl, supabaseKey);
            var patientCode = GetPatientCode();
            var success = await RunTestSequence(ghostly, email, password, patientCode);
            PocUtils.DisplayTestSummary(success, false, patientCode);
            return success;
        }

        /// <summary>
        /// Test using raw HTTP API calls
        /// </summary>
        private static async Task<bool> TestHttpClient(string supabaseUrl, string supabaseKey, string email, string password)
        {
            Console.WriteLine("🟠 Testing Raw HTTP API Client");
            Console.WriteLine("==============================\n");

            using var ghostly = new LegacyHttpClient(supabaseUrl, supabaseKey);
            var patientCode = GetPatientCode();
            var success = await RunTestSequence(ghostly, email, password, patientCode);
            PocUtils.DisplayTestSummary(false, success, patientCode);
            return success;
        }

        /// <summary>
        /// Test both clients for comparison
        /// </summary>
        private static async Task TestBothClients(string supabaseUrl, string supabaseKey, string email, string password)
        {
            Console.WriteLine("🔄 Comparison Mode - Testing Both Implementations");
            Console.WriteLine("=================================================\n");

            // Get patient code once for both tests
            var patientCode = GetPatientCode();

            // Test Supabase Client first
            Console.WriteLine("🔵 ROUND 1: Official Supabase C# Client");
            Console.WriteLine("---------------------------------------");
            var supabaseClient = new LegacySupabaseClient(supabaseUrl, supabaseKey);
            var supabaseSuccess = await RunTestSequence(supabaseClient, email, password, patientCode);

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Test HTTP Client second  
            Console.WriteLine("🟠 ROUND 2: Raw HTTP API Client");
            Console.WriteLine("-------------------------------");
            using var httpClient = new LegacyHttpClient(supabaseUrl, supabaseKey);
            var httpSuccess = await RunTestSequence(httpClient, email, password, patientCode);

            // Use the enhanced summary from Utils
            PocUtils.DisplayTestSummary(supabaseSuccess, httpSuccess, patientCode);
        }

        /// <summary>
        /// Get patient code from therapist (after authentication context is established)
        /// </summary>
        private static string GetPatientCode()
        {
            Console.WriteLine("👤 Patient Selection");
            Console.WriteLine("1️⃣ Use random patient code");
            Console.WriteLine("2️⃣ Enter specific patient code");
            Console.Write("Choice (1/2): ");
            var patientChoice = Console.ReadLine();

            string patientCode;
            if (patientChoice == "2")
            {
                Console.Write("Enter patient code (e.g., P001, P042): ");
                patientCode = Console.ReadLine()?.Trim().ToUpper();
                if (string.IsNullOrEmpty(patientCode))
                {
                    patientCode = PocUtils.GenerateTestPatientCode();
                    Console.WriteLine($"   ⚠️ Using default: {patientCode}");
                }
                else
                {
                    Console.WriteLine($"   ✅ Selected patient: {patientCode}");
                }
            }
            else
            {
                patientCode = PocUtils.GenerateTestPatientCode();
                Console.WriteLine($"   🎲 Generated: {patientCode}");
            }
            Console.WriteLine();
            return patientCode;
        }

        /// <summary>
        /// Cleanup test files and show results
        /// </summary>
        private static void CleanupTestFiles()
        {
            Console.WriteLine("🧹 Cleanup Mode - Removing Test Files");
            Console.WriteLine("=====================================\n");

            try
            {
                var cleanedCount = PocUtils.CleanupTestFiles();

                Console.WriteLine($"✅ Cleanup completed!");
                Console.WriteLine($"📁 Removed {cleanedCount} local test files");
                Console.WriteLine("\n💡 Note: Remote files in Supabase Storage are preserved");
                Console.WriteLine("   You can manually delete them from the Supabase dashboard if needed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Run the standard test sequence for any client implementation
        /// </summary>
        private static async Task<bool> RunTestSequence(dynamic ghostly, string email, string password, string patientCode)
        {
            try
            {
                // Test 1: RLS Security Validation
                Console.WriteLine("1️⃣ Security Test - RLS Protection");
                var rlsWorking = await ghostly.TestRLSProtectionAsync(email, password);
                if (!rlsWorking)
                {
                    Console.WriteLine("   ❌ Authentication failed - cannot continue tests");
                    Console.WriteLine("   💡 Please verify your credentials in Supabase Authentication");
                    return false;
                }
                Console.WriteLine();

                // Test 2: File Upload (with patient subfolder)
                Console.WriteLine($"2️⃣ File Upload Test (Patient: {patientCode})");
                var sampleFile = await PocUtils.CreateSampleC3DFileAsync(patientCode);
                if (sampleFile != null)
                {
                    var uploadResult = await ghostly.UploadFileAsync(patientCode, sampleFile);
                    Console.WriteLine(uploadResult != null ? "   ✅ Upload successful" : "   ❌ Upload failed");

                    if (uploadResult != null)
                    {
                        Console.WriteLine($"   📁 File stored in: {patientCode}/");
                        Console.WriteLine($"   📄 Filename: {Path.GetFileName(uploadResult.FileName)}");
                        Console.WriteLine($"   📊 Size: {uploadResult.FileSize} bytes");
                    }
                }
                else
                {
                    Console.WriteLine("   ❌ Failed to create sample file");
                    return false;
                }
                Console.WriteLine();

                // Test 3: File Listing (show patient folder structure)
                Console.WriteLine($"3️⃣ File List Test (Patient: {patientCode})");
                var files = await ghostly.ListFilesAsync(patientCode);
                Console.WriteLine(files.Count > 0 ? "   ✅ Listing successful" : "   ⚠️ No files listed");
                Console.WriteLine();

                // Test 4: File Download
                Console.WriteLine($"4️⃣ File Download Test (Patient: {patientCode})");
                if (files.Count > 0)
                {
                    var fileToDownload = files[0];
                    string downloadFileName = (fileToDownload.Name as string).Split('/').Last(); // Handle patientCode/filename
                    var downloadPath = Path.Combine("./c3d-test-download", downloadFileName);

                    var downloadSuccess = await ghostly.DownloadFileAsync(fileToDownload.Name, downloadPath);
                    Console.WriteLine(downloadSuccess ? $"   ✅ Download successful to {downloadPath}" : "   ❌ Download failed");
                }
                else
                {
                    Console.WriteLine("   ⚠️ Skipping download test - no files to download");
                }
                Console.WriteLine();

                // Test 5: Sign Out
                Console.WriteLine("5️⃣ Sign Out Test");
                await ghostly.SignOutAsync();
                Console.WriteLine();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test Sequence Error: {ex.Message}");
                Console.WriteLine($"💡 Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Runs the complete Proof of Concept for the multi-therapist RLS strategy.
        /// </summary>
        private static async Task RunRlsPoc(string supabaseUrl, string supabaseKey)
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            // Initialize a new client for this specific test run
            var supabase = new Supabase.Client(supabaseUrl, supabaseKey, options);
            await supabase.InitializeAsync();

            var therapist1Email = "therapist1@example.com";
            var therapist1Password = PocUtils.GetEnvironmentVariable("THERAPIST1_PASSWORD", "default_password_1");
            var therapist2Email = "therapist2@example.com";
            var therapist2Password = PocUtils.GetEnvironmentVariable("THERAPIST2_PASSWORD", "default_password_2");

            try
            {
                // Phase 1: Prepare the environment by uploading files for each therapist.
                // This also implicitly tests the INSERT storage policies.
                await RlsTestSetup.PrepareTestEnvironment(supabase, therapist1Email, therapist1Password);
                await RlsTestSetup.PrepareTestEnvironment(supabase, therapist2Email, therapist2Password);

                // Phase 2: Run all validation tests.
                await MultiTherapistRlsTests.RunAllTests(supabase, therapist1Email, therapist1Password, therapist2Email, therapist2Password);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n A critical error occurred during the RLS POC: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}