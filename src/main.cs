using System;
using System.IO;
using System.Threading.Tasks;
using GhostlySupaPoc.Clients;
using GhostlySupaPoc.Config;
using GhostlySupaPoc.RlsTests;
using GhostlySupaPoc.Utils;
using Supabase;

namespace GhostlySupaPoc
{
    /// <summary>
    /// Main program for the GHOSTLY+ Proof of Concept.
    /// This application demonstrates and compares two client implementations for interacting with Supabase:
    /// 1. A client using the official Supabase C# library.
    /// 2. A client using raw HTTP API calls.
    /// It also includes a comprehensive test suite for validating multi-therapist Row-Level Security (RLS).
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("GHOSTLY+ Supabase C# Client Comparison POC");
            Console.WriteLine("=============================================\n");
            
            DotEnv.Load();

            if (!TestConfig.IsValid())
            {
                ConsoleHelper.WriteError("Invalid configuration. Please check environment variables or .env file.");
                Console.ReadKey();
                return;
            }
            ConsoleHelper.WriteSuccess("Configuration loaded successfully.\n");

            try
            {
                await RunMainMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ A critical error occurred: {ex.Message}");
                Console.WriteLine($"💡 Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nExecution finished. Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task RunMainMenu()
        {
            Console.WriteLine("Select an implementation to test:");
            Console.WriteLine("  1. Official Supabase C# Client");
            Console.WriteLine("  2. Raw HTTP API Client");
            Console.WriteLine("  3. Both (Side-by-Side Comparison)");
            Console.WriteLine("  4. Cleanup Local Test Files");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("  5. Multi-Therapist RLS Test Suite " + ConsoleHelper.SECURITY);
            Console.ResetColor();
            Console.Write("Enter choice (1-5): ");
            var choice = Console.ReadLine();
            Console.WriteLine();

            string email = null;
            string password = null;

            if (choice == "1" || choice == "2" || choice == "3")
            {
                Console.WriteLine("Select Authentication Method:");
                Console.WriteLine("  1. Enter credentials manually");
                Console.WriteLine($"  2. Use pre-configured test user ({TestConfig.Therapist1Email})");
                Console.Write("  Choice (1/2): ");
                var authChoice = Console.ReadLine();

                if (authChoice == "1")
                {
                    Console.WriteLine("\nEnter Therapist Credentials:");
                    Console.Write("  Email: ");
                    email = Console.ReadLine();
                    Console.Write("  Password: ");
                    password = Console.ReadLine();
                    Console.WriteLine();

                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    {
                        ConsoleHelper.WriteError("Email and password cannot be empty for manual entry.");
                        return;
                    }
                }
                else
                {
                    email = TestConfig.Therapist1Email;
                    password = TestConfig.Therapist1Password;
                    Console.WriteLine($"\n-> Using pre-configured test user: {email}\n");
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
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
        
        private enum ClientType { Supabase, Http }

        /// <summary>
        /// Creates an instance of a client based on the specified type.
        /// </summary>
        private static ISupaClient CreateClient(ClientType type, string bucket)
        {
            return type == ClientType.Supabase
                ? new SupabaseClient(TestConfig.SupabaseUrl, TestConfig.SupabaseAnonKey, bucket)
                : new CustomHttpClient(TestConfig.SupabaseUrl, TestConfig.SupabaseAnonKey, bucket);
        }

        /// <summary>
        /// Executes the test sequence for both client implementations and compares the results.
        /// </summary>
        private static async Task ExecuteComparisonTest(string email, string password)
        {
            Console.WriteLine("🔄 Comparison Mode - Testing Both Implementations");
            Console.WriteLine("=================================================\n");
            
            var patientCode = GetPatientCodeFromUser();
            bool supabaseSuccess;
            bool httpSuccess;

            Console.WriteLine("🔵 ROUND 1: Official Supabase C# Client");
            Console.WriteLine("---------------------------------------");
            using (var supabaseClient = CreateClient(ClientType.Supabase, TestConfig.LegacyTestBucket))
            {
                supabaseSuccess = await RunTestSequence(supabaseClient, email, password, patientCode);
            }

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            Console.WriteLine("🟠 ROUND 2: Raw HTTP API Client");
            Console.WriteLine("-------------------------------");
            using (var httpClient = CreateClient(ClientType.Http, TestConfig.LegacyTestBucket))
            {
                httpSuccess = await RunTestSequence(httpClient, email, password, patientCode);
            }
            
            PocUtils.DisplayTestSummary(supabaseSuccess, httpSuccess, patientCode, isComparison: true);
        }

        /// <summary>
        /// Executes a standard test sequence for a single client implementation.
        /// </summary>
        private static async Task ExecuteSingleClientTest(ClientType clientType, string email, string password)
        {
            var clientName = clientType == ClientType.Supabase ? "Official Supabase C# Client" : "Raw HTTP API Client";
            var icon = clientType == ClientType.Supabase ? "🔵" : "🟠";

            Console.WriteLine($"{icon} Testing {clientName}");
            Console.WriteLine(new string('=', 20 + clientName.Length) + "\n");

            using (var client = CreateClient(clientType, TestConfig.LegacyTestBucket))
            {
                var patientCode = GetPatientCodeFromUser();
                var success = await RunTestSequence(client, email, password, patientCode);

                var isSupabaseClient = client is SupabaseClient;
                PocUtils.DisplayTestSummary(
                    supabaseSuccess: isSupabaseClient && success,
                    httpSuccess: !isSupabaseClient && success,
                    patientCode: patientCode,
                    isComparison: false);
            }
        }

        /// <summary>
        /// Prompts the user to select or generate a patient code for testing.
        /// </summary>
        private static string GetPatientCodeFromUser()
        {
            Console.WriteLine("👤 Select Patient For Test");
            Console.WriteLine("  1. Use a randomly generated patient code");
            Console.WriteLine("  2. Enter a specific patient code");
            Console.Write("  Choice (1/2): ");
            var patientChoice = Console.ReadLine();

            string patientCode;
            if (patientChoice == "2")
            {
                Console.Write("  Enter patient code (e.g., P001, P042): ");
                patientCode = Console.ReadLine()?.Trim().ToUpper();
                if (string.IsNullOrEmpty(patientCode))
                {
                    patientCode = PocUtils.GenerateTestPatientCode();
                    Console.WriteLine($"   -> No input; using randomly generated code: {patientCode}");
                }
                else
                {
                    Console.WriteLine($"   -> Using specified patient code: {patientCode}");
                }
            }
            else
            {
                patientCode = PocUtils.GenerateTestPatientCode();
                Console.WriteLine($"   -> Using randomly generated code: {patientCode}");
            }
            Console.WriteLine();
            return patientCode;
        }

        /// <summary>
        /// Removes locally generated test files.
        /// </summary>
        private static void CleanupTestFiles()
        {
            Console.WriteLine("🧹 Cleaning up local test files...");
            Console.WriteLine("=====================================\n");

            try
            {
                var cleanedCount = PocUtils.CleanupTestFiles();
                Console.WriteLine($"✅ Cleanup complete. Removed {cleanedCount} local files.");
                Console.WriteLine("\nNote: Remote files in Supabase Storage are not affected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An error occurred during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes the full suite of operations for a given client to test its functionality.
        /// </summary>
        private static async Task<bool> RunTestSequence(ISupaClient client, string email, string password, string patientCode)
        {
            try
            {
                ConsoleHelper.WriteHeader("1. Initial Authentication");
                if (!await client.AuthenticateAsync(email, password))
                {
                    ConsoleHelper.WriteError("Authentication failed. Aborting further tests.");
                    return false;
                }
                ConsoleHelper.WriteSuccess("Successfully authenticated.");
                // Sign out to ensure the RLS test starts in an unauthenticated state.
                await client.SignOutAsync();
                Console.WriteLine();

                ConsoleHelper.WriteHeader("2. Security Test: Verifying RLS Policies");
                if (!await client.TestRLSProtectionAsync(email, password))
                {
                    ConsoleHelper.WriteError("RLS security test failed. Aborting further tests.");
                    ConsoleHelper.WriteInfo("Verify user credentials and RLS policies in your Supabase project.");
                    return false;
                }

                // Re-authenticate before proceeding to ensure the session is active
                ConsoleHelper.WriteHeader("Re-authenticating for subsequent tests");
                if (!await client.AuthenticateAsync(email, password))
                {
                    ConsoleHelper.WriteError("Re-authentication failed. Aborting.");
                    return false;
                }
                Console.WriteLine();

                ConsoleHelper.WriteHeader($"3. File Upload Test (Patient: {patientCode})");
                var sampleFile = await PocUtils.CreateSampleC3DFileAsync(patientCode);
                if (string.IsNullOrEmpty(sampleFile)) return false;
                
                var uploadResult = await client.UploadFileAsync(patientCode, sampleFile);
                if (uploadResult == null)
                {
                    ConsoleHelper.WriteError("File upload failed.");
                    return false;
                }
                
                ConsoleHelper.WriteSuccess("Upload successful.");
                ConsoleHelper.WriteInfo($"  -> Stored in: {patientCode}/");
                ConsoleHelper.WriteInfo($"  -> Filename:  {Path.GetFileName(uploadResult.FileName)}");
                ConsoleHelper.WriteInfo($"  -> Size:      {uploadResult.FileSize} bytes");
                ConsoleHelper.WriteInfo($"  -> Path:      {uploadResult.FilePath}");
                ConsoleHelper.WriteInfo($"  -> Timestamp: {uploadResult.UploadedAt:yyyy-MM-dd HH:mm:ss} UTC");
                
                string uploadedFileName = Path.GetFileName(uploadResult.FileName);

                ConsoleHelper.WriteHeader($"4. File Listing Test (Patient: {patientCode})");
                var files = await client.ListFilesAsync(patientCode);
                if (files == null || !files.Any(f => f.Name == uploadedFileName))
                {
                    ConsoleHelper.WriteError($"Uploaded file '{uploadedFileName}' not found in list.");
                    return false;
                }
                ConsoleHelper.WriteSuccess($"Found {files.Count} file(s) for patient {patientCode}, including the uploaded file.");
                foreach (var file in files)
                {
                    ConsoleHelper.WriteInfo($"  -> {file.Name} (Created: {file.CreatedAt:g})");
                }

                ConsoleHelper.WriteHeader("5. File Download Test");
                string downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "c3d-test-download");
                string downloadedFilePath = Path.Combine(downloadDirectory, uploadedFileName);
                if (!await client.DownloadFileAsync(uploadedFileName, downloadedFilePath, patientCode))
                {
                    ConsoleHelper.WriteError("File download failed.");
                    return false;
                }
                ConsoleHelper.WriteSuccess($"File downloaded successfully to: {downloadedFilePath}");

                ConsoleHelper.WriteHeader("6. Sign Out");
                await client.SignOutAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ An error occurred during the test sequence: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Runs the specialized multi-therapist RLS test suite.
        /// </summary>
        private static async Task RunRlsPoc()
        {
            Console.WriteLine("🚀 Running Multi-Therapist RLS Test Suite");
            Console.WriteLine("==========================================\n");

            // The RLS test suite requires its own client instance to manage
            // the authentication state of multiple users (therapists).
            var options = new SupabaseOptions { AutoRefreshToken = true };
            var rlsTestClient = new Client(TestConfig.SupabaseUrl, TestConfig.SupabaseAnonKey, options);
            await rlsTestClient.InitializeAsync();
            
            try
            {
                // Prepare the test environment for both therapists
                await RlsTestSetup.PrepareTestEnvironment(rlsTestClient, TestConfig.Therapist1Email, TestConfig.Therapist1Password, TestConfig.RlsTestBucket);
                await RlsTestSetup.PrepareTestEnvironment(rlsTestClient, TestConfig.Therapist2Email, TestConfig.Therapist2Password, TestConfig.RlsTestBucket);

                // Run the static test method with the dedicated client
                await MultiTherapistRlsTests.RunAllTests(
                    rlsTestClient,
                    TestConfig.Therapist1Email,
                    TestConfig.Therapist1Password,
                    TestConfig.Therapist2Email,
                    TestConfig.Therapist2Password,
                    TestConfig.RlsTestBucket
                );
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"A critical error occurred during the RLS test suite: {ex.Message}");
            }
        }
    }
}