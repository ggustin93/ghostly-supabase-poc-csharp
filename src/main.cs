using System;
using System.IO;
using System.Threading.Tasks;
using GhostlySupaPoc.Clients;
using GhostlySupaPoc.Config;
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
            
            // Try to load configuration from .env file for local development
            DotEnv.Load();

            // Initialize and validate Supabase connection from centralized config
            if (!TestConfig.IsValid())
            {
                ConsoleHelper.WriteError("Invalid configuration! Please check your environment variables.");
                Console.ReadKey();
                return;
            }
            ConsoleHelper.WriteSuccess("Connected to Supabase\n");

            try
            {
                // Ask user which implementation to test
                Console.WriteLine("🔧 Choose Implementation to Test:");
                Console.WriteLine("1️⃣ Official Supabase C# Client (Legacy)");
                Console.WriteLine("2️⃣ Raw HTTP API Client (Legacy)");
                Console.WriteLine("3️⃣ Both (Comparison Mode)");
                Console.WriteLine("4️⃣ Cleanup Test Files");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("5️⃣ Multi-Therapist RLS Test " + ConsoleHelper.SECURITY);
                Console.ResetColor();
                Console.Write("Choice (1/2/3/4/5): ");
                var choice = Console.ReadLine();
                Console.WriteLine();

                string email = null;
                string password = null;

                if (choice == "1" || choice == "2" || choice == "3")
                {
                    // Get user credentials for testing
                    Console.WriteLine("👨‍⚕️ Therapist Authentication");
                    Console.Write("Email (leave blank for default): ");
                    email = Console.ReadLine();
                    Console.Write("Password: ");
                    password = Console.ReadLine();
                    Console.WriteLine();

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        email = TestConfig.Therapist1Email;
                        Console.WriteLine($"   -> Using default email: {email}");
                    }
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        password = TestConfig.Therapist1Password;
                        Console.WriteLine($"   -> Using default password.");
                    }
                }

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("🔵 Testing Official Supabase C# Client");
                        Console.WriteLine("=====================================\n");
                        using (var client = new LegacySupabaseClient(TestConfig.SupabaseUrl, TestConfig.SupabaseAnonKey, TestConfig.LegacyTestBucket))
                        {
                            await RunClientTest(client, email, password);
                        }
                        break;
                    case "2":
                        Console.WriteLine("🟠 Testing Raw HTTP API Client");
                        Console.WriteLine("==============================\n");
                        using (var client = new LegacyHttpClient(TestConfig.SupabaseUrl, TestConfig.SupabaseAnonKey, TestConfig.LegacyTestBucket))
                        {
                            await RunClientTest(client, email, password);
                        }
                        break;
                    case "3":
                        Console.WriteLine("🔄 Comparison Mode - Testing Both Implementations");
                        Console.WriteLine("=================================================\n");
                        
                        // Get patient code once for both tests
                        var patientCode = GetPatientCode();
                        bool supabaseSuccess = false;
                        bool httpSuccess = false;

                        Console.WriteLine("🔵 ROUND 1: Official Supabase C# Client");
                        Console.WriteLine("---------------------------------------");
                        using (var supabaseClient = new LegacySupabaseClient(TestConfig.SupabaseUrl, TestConfig.SupabaseAnonKey, TestConfig.LegacyTestBucket))
                        {
                            supabaseSuccess = await RunTestSequence(supabaseClient, email, password, patientCode);
                        }

                        Console.WriteLine("\n" + new string('=', 50) + "\n");

                        Console.WriteLine("🟠 ROUND 2: Raw HTTP API Client");
                        Console.WriteLine("-------------------------------");
                        using (var httpClient = new LegacyHttpClient(TestConfig.SupabaseUrl, TestConfig.SupabaseAnonKey, TestConfig.LegacyTestBucket))
                        {
                           httpSuccess = await RunTestSequence(httpClient, email, password, patientCode);
                        }
                        
                        // Display a single, combined summary at the end of both rounds
                        PocUtils.DisplayTestSummary(supabaseSuccess, httpSuccess, patientCode, isComparison: true);
                        break;
                    case "4":
                        CleanupTestFiles();
                        break;
                    case "5":
                        await RunRlsPoc();
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
        /// Executes a full test sequence for a given client implementation.
        /// </summary>
        private static async Task RunClientTest(ILegacyClient client, string email, string password)
        {
            var patientCode = GetPatientCode();
            var success = await RunTestSequence(client, email, password, patientCode);

            // Determine which client ran to display the correct summary
            var isSupabaseClient = client is LegacySupabaseClient;
            PocUtils.DisplayTestSummary(
                supabaseSuccess: isSupabaseClient && success, 
                httpSuccess: !isSupabaseClient && success, 
                patientCode: patientCode,
                isComparison: false);
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
        private static async Task<bool> RunTestSequence(ILegacyClient ghostly, string email, string password, string patientCode)
        {
            try
            {
                // Test 1: RLS Security Validation
                ConsoleHelper.WriteHeader("1️⃣ Security Test - RLS Protection");
                var rlsWorking = await ghostly.TestRLSProtectionAsync(email, password);
                if (!rlsWorking)
                {
                    ConsoleHelper.WriteError("Authentication failed - cannot continue tests");
                    ConsoleHelper.WriteInfo("Please verify your credentials in Supabase Authentication");
                    return false;
                }

                // Test 2: File Upload (with patient subfolder)
                ConsoleHelper.WriteHeader($"2️⃣ File Upload Test (Patient: {patientCode})");
                var sampleFile = await PocUtils.CreateSampleC3DFileAsync(patientCode);
                if (sampleFile != null)
                {
                    var uploadResult = await ghostly.UploadFileAsync(patientCode, sampleFile);
                    
                    if (uploadResult != null)
                    {
                        ConsoleHelper.WriteSuccess("Upload successful");
                        ConsoleHelper.WriteInfo($"  │ File stored in: {patientCode}/");
                        ConsoleHelper.WriteInfo($"  │ Filename: {Path.GetFileName(uploadResult.FileName)}");
                        ConsoleHelper.WriteInfo($"  │ Size: {uploadResult.FileSize} bytes");
                        ConsoleHelper.WriteInfo($"  │ Path: {uploadResult.FilePath}");
                        ConsoleHelper.WriteInfo($"  └ Timestamp: {uploadResult.UploadedAt:yyyy-MM-dd HH:mm:ss}");
                    }
                    else
                    {
                        ConsoleHelper.WriteError("Upload failed");
                    }
                }
                else
                {
                    ConsoleHelper.WriteError("Failed to create sample file");
                    return false;
                }

                // Test 3: File Listing (show patient folder structure)
                ConsoleHelper.WriteHeader($"3️⃣ File List Test (Patient: {patientCode})");
                var files = await ghostly.ListFilesAsync(patientCode);
                
                if (files.Count > 0)
                {
                    ConsoleHelper.WriteSuccess($"Listing successful - Found {files.Count} file(s)");
                    
                    // Display file details for verification
                    for (int i = 0; i < Math.Min(files.Count, 3); i++) // Show up to 3 files
                    {
                        var file = files[i];
                        ConsoleHelper.WriteInfo($"  │ File {i+1}: {file.Name}");
                        ConsoleHelper.WriteInfo($"  │   ID: {file.Id}");
                        if (file.Size.HasValue)
                            ConsoleHelper.WriteInfo($"  │   Size: {file.Size} bytes");
                        if (file.CreatedAt.HasValue)
                            ConsoleHelper.WriteInfo($"  │   Created: {file.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    }
                    
                    if (files.Count > 3)
                        ConsoleHelper.WriteInfo($"  └ ... and {files.Count - 3} more file(s)");
                    else
                        ConsoleHelper.WriteInfo($"  └ End of file list");
                }
                else
                {
                    ConsoleHelper.WriteWarning("No files listed");
                }

                // Test 4: File Download
                ConsoleHelper.WriteHeader($"4️⃣ File Download Test (Patient: {patientCode})");
                if (files.Count > 0)
                {
                    var fileToDownload = files[0];
                    string downloadFileName = fileToDownload.Name.Split('/').Last();
                    var downloadPath = Path.Combine("./c3d-test-download", downloadFileName);

                    var downloadSuccess = await ghostly.DownloadFileAsync(fileToDownload.Name, downloadPath, patientCode);
                    
                    if (downloadSuccess)
                    {
                        ConsoleHelper.WriteSuccess($"Download successful");
                        ConsoleHelper.WriteInfo($"  │ Source: {fileToDownload.Name}");
                        ConsoleHelper.WriteInfo($"  │ Destination: {downloadPath}");
                        
                        // Show file info
                        var fileInfo = new FileInfo(downloadPath);
                        if (fileInfo.Exists)
                        {
                            ConsoleHelper.WriteInfo($"  │ Size: {fileInfo.Length} bytes");
                            ConsoleHelper.WriteInfo($"  └ Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                        }
                    }
                    else
                    {
                        ConsoleHelper.WriteError("Download failed");
                    }
                }
                else
                {
                    ConsoleHelper.WriteWarning("Skipping download test - no files to download");
                }

                // Test 5: Sign Out
                ConsoleHelper.WriteHeader("5️⃣ Sign Out Test");
                await ghostly.SignOutAsync();
                ConsoleHelper.WriteSuccess("Successfully signed out");

                return true;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Test Sequence Error: {ex.Message}");
                ConsoleHelper.WriteInfo($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Runs the complete Proof of Concept for the multi-therapist RLS strategy.
        /// </summary>
        private static async Task RunRlsPoc()
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            // Initialize a new client for this specific test run
            var supabase = new Supabase.Client(TestConfig.SupabaseUrl, TestConfig.SupabaseAnonKey, options);
            await supabase.InitializeAsync();

            try
            {
                // Phase 1: Prepare the environment using credentials from config
                await RlsTestSetup.PrepareTestEnvironment(supabase, TestConfig.Therapist1Email, TestConfig.Therapist1Password, TestConfig.RlsTestBucket);
                await RlsTestSetup.PrepareTestEnvironment(supabase, TestConfig.Therapist2Email, TestConfig.Therapist2Password, TestConfig.RlsTestBucket);

                // Phase 2: Run all validation tests using credentials from config
                await MultiTherapistRlsTests.RunAllTests(supabase, TestConfig.Therapist1Email, TestConfig.Therapist1Password, TestConfig.Therapist2Email, TestConfig.Therapist2Password, TestConfig.RlsTestBucket);
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