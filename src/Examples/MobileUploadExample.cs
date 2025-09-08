using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Supabase;
using GhostlySupaPoc.Config;
using GhostlySupaPoc.Models;
using GhostlySupaPoc.Utils;

namespace GhostlySupaPoc.Examples
{
    /// <summary>
    /// Concise example for mobile app: Therapist authentication + C3D upload
    /// Copy this class to your mobile app and adapt as needed
    /// </summary>
    public class MobileUploadExample
    {
        private readonly Client _supabase;
        private readonly string _bucketName;
        
        /// <summary>
        /// Initialize with Supabase credentials
        /// For mobile apps: Store credentials securely, not in code
        /// </summary>
        public MobileUploadExample(string supabaseUrl, string supabaseKey, string bucketName = "emg_data")
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,        // Auto-refresh JWT tokens
                AutoConnectRealtime = false      // Not needed for upload
            };
            
            _supabase = new Client(supabaseUrl, supabaseKey, options);
            _bucketName = bucketName;
        }
        
        /// <summary>
        /// Initialize the Supabase client (call once at app startup)
        /// </summary>
        public async Task Initialize()
        {
            await _supabase.InitializeAsync();
        }
        
        /// <summary>
        /// Sign in as therapist
        /// </summary>
        public async Task<bool> SignIn(string email, string password)
        {
            try
            {
                var response = await _supabase.Auth.SignIn(email, password);
                if (response?.User != null)
                {
                    Console.WriteLine($"✅ Authenticated as: {response.User.Email}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Auth error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get assigned patients for the authenticated therapist
        /// </summary>
        public async Task<List<Patient>> GetAssignedPatients()
        {
            try
            {
                var response = await _supabase.From<Patient>().Get();
                return response.Models ?? new List<Patient>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching patients: {ex.Message}");
                return new List<Patient>();
            }
        }
        
        /// <summary>
        /// Upload C3D file for a patient
        /// </summary>
        public async Task<bool> UploadC3DFile(string filePath, string patientCode)
        {
            try
            {
                // Read file
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                
                // Storage path follows pattern: {patientCode}/{filename}
                var storagePath = $"{patientCode}/{fileName}";
                
                // Upload to Supabase Storage
                var storage = _supabase.Storage.From(_bucketName);
                var result = await storage.Upload(
                    fileBytes,
                    storagePath,
                    new Supabase.Storage.FileOptions
                    {
                        ContentType = "application/octet-stream",
                        Upsert = false  // Don't overwrite existing files
                    }
                );
                
                if (!string.IsNullOrEmpty(result))
                {
                    Console.WriteLine($"✅ File uploaded: {storagePath}");
                    Console.WriteLine("ℹ️ Database record created automatically via webhook");
                    return true;
                }
                
                Console.WriteLine("❌ Upload failed");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sign out
        /// </summary>
        public async Task SignOut()
        {
            await _supabase.Auth.SignOut();
        }
        
        // ============================================================
        // CONSOLE TEST EXAMPLE - Uses config from this POC project
        // ============================================================
        
        /// <summary>
        /// Test example using POC configuration
        /// </summary>
        public static async Task RunExample()
        {
            ConsoleHelper.WriteMajorHeader("📱 Mobile Upload Example");
            
            var config = SimpleConfig.Instance;
            
            // Check configuration
            if (!config.IsValid())
            {
                Console.WriteLine(config.GetStatusMessage());
                return;
            }
            
            // Show example code for mobile developers
            Console.WriteLine("Example implementation for your mobile app:\n");
            Console.WriteLine(@"```csharp
// 1. Initialize with Supabase credentials (at app startup)
// IMPORTANT: Store credentials securely! Never hardcode in production!
var uploader = new MobileUploadExample(
    Environment.GetEnvironmentVariable(""SUPABASE_URL""),      // Your Supabase project URL
    Environment.GetEnvironmentVariable(""SUPABASE_ANON_KEY""), // Your Supabase anon key
    ""emg_data""  // bucket name
);
await uploader.Initialize();

// 2. Sign in as therapist
bool success = await uploader.SignIn(
    therapistEmail,    // Therapist's email
    therapistPassword  // Therapist's password
);

// 3. Get assigned patients and upload for specific patient
if (success)
{
    var patients = await uploader.GetAssignedPatients();
    if (patients.Any())
    {
        var patientCode = patients.First().PatientCode;  // Or let user select
        await uploader.UploadC3DFile(
            filePath,      // Path to C3D file
            patientCode    // Use actual assigned patient code
        );
    }
    
    // 4. Sign out when done
    await uploader.SignOut();
}

// Note: Contact your team lead for test credentials.
// Never commit credentials to source control!
```");
            
            Console.WriteLine("\n📋 Key Points for Mobile Developers:");
            Console.WriteLine("• Use Supabase Client (not raw HTTP)");
            Console.WriteLine($"• Files go to '{config.BucketName}' bucket");
            Console.WriteLine("• Path format: {patientCode}/{filename}");
            Console.WriteLine("• Webhook creates therapy_session automatically");
            Console.WriteLine("• RLS ensures data isolation between therapists");
            Console.WriteLine("\n⚠️  Security Notes:");
            Console.WriteLine("• Test credentials are in appsettings.json (POC only)");
            Console.WriteLine("• Use environment variables or secure key storage in production");
            
            // Test with actual configuration
            Console.WriteLine($"\n🧪 Test Configuration Available:");
            Console.WriteLine($"   • Therapist 1: {config.Therapist1Email ?? "not configured"}");
            Console.WriteLine($"   • Therapist 2: {config.Therapist2Email ?? "not configured"}");
            Console.WriteLine($"   • Bucket: {config.BucketName}");
            
            Console.Write("\nRun test upload? (y/n): ");
            var response = Console.ReadLine();
            
            if (response?.ToLower() == "y")
            {
                // Select therapist for testing
                string testEmail = config.Therapist1Email;
                string testPassword = config.Therapist1Password;
                
                if (!string.IsNullOrEmpty(config.Therapist2Email))
                {
                    Console.Write("Use Therapist 1 or 2? (1/2): ");
                    if (Console.ReadLine() == "2")
                    {
                        testEmail = config.Therapist2Email;
                        testPassword = config.Therapist2Password;
                    }
                }
                
                var uploader = new MobileUploadExample(
                    config.SupabaseUrl,
                    config.SupabaseKey,
                    config.BucketName
                );
                
                await uploader.Initialize();
                
                Console.WriteLine($"\n🔐 Signing in as: {testEmail}");
                if (await uploader.SignIn(testEmail, testPassword))
                {
                    // Get assigned patients (RLS automatically filters)
                    Console.WriteLine("👥 Fetching assigned patients...");
                    var patients = await uploader.GetAssignedPatients();
                    
                    if (!patients.Any())
                    {
                        Console.WriteLine("❌ No patients assigned to this therapist.");
                        await uploader.SignOut();
                        return;
                    }
                    
                    Console.WriteLine($"✅ Found {patients.Count} assigned patient(s):");
                    foreach (var p in patients)
                    {
                        var details = new[] { 
                            !string.IsNullOrEmpty(p.AgeGroup) ? $"Age: {p.AgeGroup}" : "",
                            !string.IsNullOrEmpty(p.Gender) ? $"Gender: {p.Gender}" : ""
                        }.Where(s => !string.IsNullOrEmpty(s));
                        var detailsStr = details.Any() ? $" ({string.Join(", ", details)})" : "";
                        Console.WriteLine($"   • {p.PatientCode}{detailsStr}");
                    }
                    
                    // Use the first assigned patient for demo
                    var selectedPatient = patients.First();
                    Console.WriteLine($"\n📤 Uploading test file for patient: {selectedPatient.PatientCode}");
                    
                    // Use real C3D file from test samples
                    var sampleC3DPath = Path.Combine(Directory.GetCurrentDirectory(), "c3d-test-samples", "Ghostly_Emg_20250310_11-50-16-0578.c3d");
                    if (File.Exists(sampleC3DPath))
                    {
                        Console.WriteLine($"📄 Using real C3D file: {Path.GetFileName(sampleC3DPath)}");
                        await uploader.UploadC3DFile(sampleC3DPath, selectedPatient.PatientCode);
                    }
                    else
                    {
                        Console.WriteLine("❌ Real C3D sample file not found. Please ensure c3d-test-samples/Ghostly_Emg_20250310_11-50-16-0578.c3d exists");
                    }
                    
                    await uploader.SignOut();
                }
            }
        }
    }
}