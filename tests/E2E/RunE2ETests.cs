using System;
using System.Linq;
using System.Threading.Tasks;
using GhostlySupaPoc.Tests.E2E;

namespace GhostlySupaPoc.Tests
{
    /// <summary>
    /// Simple E2E test runner - no complex frameworks, just run and report
    /// </summary>
    public class RunE2ETests
    {
        public static async Task Main(string[] args)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
   _____ _    _  ____   _____ _______ _  __     __  
  / ____| |  | |/ __ \ / ____|__   __| | \ \   / /_ 
 | |  __| |__| | |  | | (___    | |  | |  \ \_/ / _|
 | | |_ |  __  | |  | |\___ \   | |  | |   \   / |_ 
 | |__| | |  | | |__| |____) |  | |  | |____| |   _|
  \_____|_|  |_|\____/|_____/   |_|  |______|_|  |_|
                                                     
            E2E Test Suite - v1.0
");
            Console.ResetColor();
            
            // Parse arguments
            bool dryRun = args.Contains("--dry-run") || args.Contains("-d");
            bool verbose = args.Contains("--verbose") || args.Contains("-v");
            
            if (dryRun)
            {
                Console.WriteLine("🔍 DRY RUN MODE - No actual uploads will occur\n");
                ShowTestPlan();
                return;
            }
            
            if (args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                return;
            }
            
            // Run the tests
            var testSuite = new TherapistUploadTest();
            var startTime = DateTime.Now;
            
            Console.WriteLine($"Started at: {startTime:HH:mm:ss}\n");
            
            bool allPassed = await testSuite.RunAllTests();
            
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            
            Console.WriteLine($"\nCompleted at: {endTime:HH:mm:ss}");
            Console.WriteLine($"Duration: {duration.TotalSeconds:F2} seconds");
            
            // Exit code for CI/CD
            Environment.Exit(allPassed ? 0 : 1);
        }
        
        private static void ShowTestPlan()
        {
            Console.WriteLine("📋 TEST PLAN");
            Console.WriteLine("============\n");
            
            Console.WriteLine("The E2E test suite will execute:");
            Console.WriteLine();
            Console.WriteLine("1️⃣  Configuration Validation");
            Console.WriteLine("    ✓ Check appsettings.json or environment variables");
            Console.WriteLine("    ✓ Verify Supabase URL and key are set");
            Console.WriteLine("    ✓ Confirm bucket name is configured");
            Console.WriteLine();
            Console.WriteLine("2️⃣  Therapist Authentication");
            Console.WriteLine("    ✓ Connect to Supabase");
            Console.WriteLine("    ✓ Authenticate with test credentials");
            Console.WriteLine("    ✓ Verify session is established");
            Console.WriteLine("    ✓ Clean logout");
            Console.WriteLine();
            Console.WriteLine("3️⃣  C3D File Validation");
            Console.WriteLine("    ✓ Check test file exists");
            Console.WriteLine("    ✓ Verify file size (>512 bytes)");
            Console.WriteLine("    ✓ Validate C3D header (0x50)");
            Console.WriteLine();
            Console.WriteLine("4️⃣  Complete Upload Workflow");
            Console.WriteLine("    ✓ Authenticate therapist");
            Console.WriteLine("    ✓ Validate C3D file");
            Console.WriteLine("    ✓ Upload to patient P000 folder");
            Console.WriteLine("    ✓ Verify upload success");
            Console.WriteLine("    ✓ Sign out");
            Console.WriteLine();
            Console.WriteLine("5️⃣  Verify File in Storage");
            Console.WriteLine("    ✓ Re-authenticate");
            Console.WriteLine("    ✓ List files in P000 folder");
            Console.WriteLine("    ✓ Confirm file appears");
            Console.WriteLine("    ✓ Show recent uploads");
            Console.WriteLine();
            Console.WriteLine("Expected duration: ~10-15 seconds");
        }
        
        private static void ShowHelp()
        {
            Console.WriteLine("Usage: dotnet run --project tests/E2E/RunE2ETests.cs [options]\n");
            Console.WriteLine("Options:");
            Console.WriteLine("  --dry-run, -d    Show test plan without executing");
            Console.WriteLine("  --verbose, -v    Show detailed output");
            Console.WriteLine("  --help, -h       Show this help message");
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine("  Set these in appsettings.json or environment variables:");
            Console.WriteLine("  - SUPABASE_URL");
            Console.WriteLine("  - SUPABASE_KEY");
            Console.WriteLine("  - BUCKET_NAME (default: emg_data)");
            Console.WriteLine("  - TEST_THERAPIST_EMAIL");
            Console.WriteLine("  - TEST_THERAPIST_PASSWORD");
            Console.WriteLine();
            Console.WriteLine("Exit codes:");
            Console.WriteLine("  0 - All tests passed");
            Console.WriteLine("  1 - One or more tests failed");
        }
    }
}