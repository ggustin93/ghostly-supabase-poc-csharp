using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GhostlySupaPoc.Clients;
using GhostlySupaPoc.Config;
using GhostlySupaPoc.Examples;
using GhostlySupaPoc.Models;
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
            Console.WriteLine("  1. Test Official Supabase C# Client");
            Console.WriteLine("  2. Test Raw HTTP API Client");
            Console.WriteLine("  3. Compare Both Client Implementations");
            Console.WriteLine("  4. Cleanup Temporary Test Files 🧹");
            Console.WriteLine("  5. Multi-Therapist RLS Security Test 🔒");
            Console.WriteLine("  6. Mobile Upload Example 📱");
            Console.Write("Enter choice (1-6): ");
            
            var choice = Console.ReadLine();
            Console.WriteLine();

            string email = null;
            string password = null;

            if (choice == "1" || choice == "2" || choice == "3")
            {
                var cfg = SimpleConfig.Instance;
                
                // Check if we have any configured therapists
                bool hasTherapist1 = !string.IsNullOrEmpty(cfg.Therapist1Email);
                bool hasTherapist2 = !string.IsNullOrEmpty(cfg.Therapist2Email);
                
                Console.WriteLine("Select Authentication:");
                Console.WriteLine("  1. Enter credentials manually");
                if (hasTherapist1)
                    Console.WriteLine($"  2. Use Therapist 1: {cfg.Therapist1Email}");
                else
                    Console.WriteLine("  2. Use Therapist 1: (not configured)");
                    
                if (hasTherapist2)
                    Console.WriteLine($"  3. Use Therapist 2: {cfg.Therapist2Email}");
                    
                Console.Write("  Choice: ");
                
                var authChoice = Console.ReadLine();

                if (authChoice == "1")
                {
                    Console.Write("\n  Email: ");
                    email = Console.ReadLine();
                    Console.Write("  Password: ");
                    password = Console.ReadLine();
                }
                else if (authChoice == "3" && hasTherapist2)
                {
                    email = cfg.Therapist2Email;
                    password = cfg.Therapist2Password;
                    Console.WriteLine($"\n➜ Using Therapist 2: {email}\n");
                }
                else if (authChoice == "2" && hasTherapist1)
                {
                    email = cfg.Therapist1Email;
                    password = cfg.Therapist1Password;
                    Console.WriteLine($"\n➜ Using Therapist 1: {email}\n");
                }
                else
                {
                    Console.WriteLine("\n❌ Selected therapist is not configured. Please enter credentials manually.");
                    Console.Write("  Email: ");
                    email = Console.ReadLine();
                    Console.Write("  Password: ");
                    password = Console.ReadLine();
                }
                
                // Validate credentials before proceeding
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("\n❌ Email and password are required. Exiting.");
                    return;
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
            Console.WriteLine("🔄 Client Comparison Test");
            Console.WriteLine("=========================");
            Console.WriteLine("Testing both implementations with the same workflow\n");
            
            var config = SimpleConfig.Instance;

            Console.WriteLine("▶️  Test 1: Official Supabase C# Client");
            Console.WriteLine("---------------------------------------");
            using (var supabaseClient = CreateClient(ClientType.Supabase, config.BucketName))
            {
                await RunTestSequence(supabaseClient, email, password);
            }

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            Console.WriteLine("▶️  Test 2: Custom HTTP API Client");
            Console.WriteLine("----------------------------------");
            using (var httpClient = CreateClient(ClientType.Http, config.BucketName))
            {
                await RunTestSequence(httpClient, email, password);
            }
        }

        private static async Task ExecuteSingleClientTest(ClientType clientType, string email, string password)
        {
            var config = SimpleConfig.Instance;
            
            Console.WriteLine($"Testing {clientType} Client");
            Console.WriteLine(new string('-', 30));

            using (var client = CreateClient(clientType, config.BucketName))
            {
                await RunTestSequence(client, email, password);
            }
        }

        private static async Task<bool> RunTestSequence(ISupaClient client, string email, string password)
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

                // 2. Fetch assigned patients (RLS automatically filters)
                Console.WriteLine("\n👥 Fetching assigned patients (RLS filtered)...");
                var patients = await client.GetPatientsAsync();
                
                if (!patients.Any())
                {
                    Console.WriteLine("❌ No patients assigned to this therapist.");
                    Console.WriteLine("   Please ensure patients are assigned in the database.");
                    await client.SignOutAsync();
                    return false;
                }
                
                // 3. Select patient for upload
                var patientCode = GetPatientCodeFromUser(patients);
                if (string.IsNullOrEmpty(patientCode))
                {
                    Console.WriteLine("❌ No patient selected.");
                    await client.SignOutAsync();
                    return false;
                }

                // 4. Upload test file
                Console.WriteLine($"\n📤 Uploading test file for patient {patientCode}...");
                var testFile = CreateTestFile();
                var result = await client.UploadFileAsync(patientCode, testFile);
                
                if (result != null)
                {
                    Console.WriteLine($"✅ Uploaded: {result.FileName}");
                }
                
                // Cleanup test file
                try { File.Delete(testFile); } catch { }

                // 5. List files
                Console.WriteLine("\n📋 Listing files...");
                var files = await client.ListFilesAsync(patientCode);
                Console.WriteLine($"Found {files.Count} files for patient {patientCode}");

                // 6. Sign out
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

        private static string GetPatientCodeFromUser(List<Patient> patients)
        {
            if (!patients.Any())
            {
                return null;
            }
            
            Console.WriteLine("\n📋 Available patients (your assigned patients only):");
            for (int i = 0; i < patients.Count; i++)
            {
                var p = patients[i];
                var ageInfo = !string.IsNullOrEmpty(p.AgeGroup) ? $"Age: {p.AgeGroup}" : "";
                var genderInfo = !string.IsNullOrEmpty(p.Gender) ? $"Gender: {p.Gender}" : "";
                var details = new[] { ageInfo, genderInfo }.Where(s => !string.IsNullOrEmpty(s));
                var detailsStr = details.Any() ? $" ({string.Join(", ", details)})" : "";
                
                Console.WriteLine($"   {i + 1}. {p.PatientCode}{detailsStr}");
            }
            
            if (patients.Count == 1)
            {
                var patient = patients.First();
                Console.WriteLine($"\n➜ Using the only assigned patient: {patient.PatientCode}");
                return patient.PatientCode;
            }
            
            Console.Write($"\nSelect patient (1-{patients.Count}) or press Enter for first patient: ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
            {
                var firstPatient = patients.First();
                Console.WriteLine($"➜ Using first patient: {firstPatient.PatientCode}");
                return firstPatient.PatientCode;
            }
            
            if (int.TryParse(input, out int selection) && selection >= 1 && selection <= patients.Count)
            {
                var selectedPatient = patients[selection - 1];
                Console.WriteLine($"➜ Selected patient: {selectedPatient.PatientCode}");
                return selectedPatient.PatientCode;
            }
            
            // Check if they entered a patient code directly
            var directMatch = patients.FirstOrDefault(p => p.PatientCode.Equals(input, StringComparison.OrdinalIgnoreCase));
            if (directMatch != null)
            {
                Console.WriteLine($"➜ Selected patient: {directMatch.PatientCode}");
                return directMatch.PatientCode;
            }
            
            Console.WriteLine($"❌ Invalid selection. Using first patient: {patients.First().PatientCode}");
            return patients.First().PatientCode;
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
                if (string.IsNullOrEmpty(config.Therapist1Email) || string.IsNullOrEmpty(config.Therapist1Password) ||
                    string.IsNullOrEmpty(config.Therapist2Email) || string.IsNullOrEmpty(config.Therapist2Password))
                {
                    Console.WriteLine("❌ Missing therapist credentials. Please configure:");
                    Console.WriteLine("   THERAPIST1_EMAIL, THERAPIST1_PASSWORD");
                    Console.WriteLine("   THERAPIST2_EMAIL, THERAPIST2_PASSWORD");
                    return;
                }
                
                Console.WriteLine($"👤 Therapist 1: {config.Therapist1Email}");
                Console.WriteLine($"👤 Therapist 2: {config.Therapist2Email}");
                Console.WriteLine($"🗄️  Test Bucket: {config.BucketName}\n");
                
                // Run comprehensive multi-therapist RLS test
                var supabase = new Supabase.Client(config.SupabaseUrl, config.SupabaseKey);
                
                await RlsTests.MultiTherapistRlsTests.RunAllTests(
                    supabase,
                    config.Therapist1Email,
                    config.Therapist1Password,
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