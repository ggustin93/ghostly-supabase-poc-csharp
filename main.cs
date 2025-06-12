using System;
using System.IO;
using System.Threading.Tasks;

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
        if (!Utils.ValidateSupabaseConfig(out string supabaseUrl, out string supabaseKey))
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
            // Get user credentials for testing
            Console.WriteLine("👨‍⚕️ Therapist Authentication");
            Console.Write("Email: ");
            var email = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();
            Console.WriteLine();

            // Ask user which implementation to test
            Console.WriteLine("🔧 Choose Implementation to Test:");
            Console.WriteLine("1️⃣ Official Supabase C# Client");
            Console.WriteLine("2️⃣ Raw HTTP API Client");
            Console.WriteLine("3️⃣ Both (Comparison Mode)");
            Console.WriteLine("4️⃣ Cleanup Test Files");
            Console.Write("Choice (1/2/3/4): ");
            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    var supabaseSuccess = await TestSupabaseClient(supabaseUrl, supabaseKey, email, password);
                    break;
                case "2":
                    var httpSuccess = await TestHttpClient(supabaseUrl, supabaseKey, email, password);
                    break;
                case "3":
                    await TestBothClients(supabaseUrl, supabaseKey, email, password);
                    break;
                case "4":
                    CleanupTestFiles();
                    break;
                default:
                    Console.WriteLine("❌ Invalid choice. Using HTTP Client as default.");
                    var defaultSuccess = await TestHttpClient(supabaseUrl, supabaseKey, email, password);
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

        var ghostly = new GhostlyPOC(supabaseUrl, supabaseKey);
        var patientCode = GetPatientCode();
        var success = await RunTestSequence(ghostly, email, password, patientCode);
        Utils.DisplayTestSummary(success, false, patientCode);
        return success;
    }

    /// <summary>
    /// Test using raw HTTP API calls
    /// </summary>
    private static async Task<bool> TestHttpClient(string supabaseUrl, string supabaseKey, string email, string password)
    {
        Console.WriteLine("🟠 Testing Raw HTTP API Client");
        Console.WriteLine("==============================\n");

        var ghostly = new GhostlyHttpPOC(supabaseUrl, supabaseKey);
        var patientCode = GetPatientCode();
        var success = await RunTestSequence(ghostly, email, password, patientCode);
        Utils.DisplayTestSummary(false, success, patientCode);
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
        var supabaseClient = new GhostlyPOC(supabaseUrl, supabaseKey);
        var supabaseSuccess = await RunTestSequence(supabaseClient, email, password, patientCode);

        Console.WriteLine("\n" + new string('=', 50) + "\n");

        // Test HTTP Client second  
        Console.WriteLine("🟠 ROUND 2: Raw HTTP API Client");
        Console.WriteLine("-------------------------------");
        var httpClient = new GhostlyHttpPOC(supabaseUrl, supabaseKey);
        var httpSuccess = await RunTestSequence(httpClient, email, password, patientCode);

        // Use the enhanced summary from Utils
        Utils.DisplayTestSummary(supabaseSuccess, httpSuccess, patientCode);
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
                patientCode = Utils.GenerateTestPatientCode();
                Console.WriteLine($"   ⚠️ Using default: {patientCode}");
            }
            else
            {
                Console.WriteLine($"   ✅ Selected patient: {patientCode}");
            }
        }
        else
        {
            patientCode = Utils.GenerateTestPatientCode();
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
            var cleanedCount = Utils.CleanupTestFiles();

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
            var sampleFile = await Utils.CreateSampleC3DFileAsync(patientCode);
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
            Console.WriteLine("3️⃣ File Listing Test");
            var files = await ghostly.ListFilesAsync();
            Console.WriteLine();

            // Test 3b: List files for specific patient
            Console.WriteLine($"3️⃣b Patient-Specific Listing (Patient: {patientCode})");
            var patientFiles = await ghostly.ListFilesAsync(patientCode);
            Console.WriteLine();

            // Test 4: File Download (conditional)
            if (patientFiles.Count > 0)
            {
                Console.WriteLine("4️⃣ File Download Test");

                // Handle different file object types
                string fileName;
                try
                {
                    // Try Supabase client format first
                    fileName = patientFiles[0].Name;
                }
                catch
                {
                    // Fall back to HTTP client format
                    fileName = patientFiles[0].name;
                }

                var downloadPath = Path.Combine("./c3d-test-download", $"downloaded_{Path.GetFileName(fileName)}");
                Utils.EnsureDirectoryExists("./c3d-test-download");

                // Download with patient code (will use subfolder structure)
                var downloadSuccess = await ghostly.DownloadFileAsync(fileName, downloadPath, patientCode);
                if (downloadSuccess)
                {
                    Console.WriteLine("   ✅ Download successful");
                    Console.WriteLine($"   📁 Saved to: {downloadPath}");
                }
                else
                {
                    Console.WriteLine("   ❌ Download failed");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("4️⃣ File Download Test");
                Console.WriteLine("   ⏭️ Skipped (no patient files available)\n");
            }

            // Test 5: Cleanup
            Console.WriteLine("5️⃣ Session Cleanup");
            await ghostly.SignOutAsync();
            Console.WriteLine();

            // Success
            Console.WriteLine("🎉 Test Sequence Completed Successfully!");
            Console.WriteLine($"📂 Patient {patientCode} files are organized in: {patientCode}/");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test Sequence Error: {ex.Message}");
            Console.WriteLine($"💡 Details: {ex.GetType().Name}");
            return false;
        }
    }
}