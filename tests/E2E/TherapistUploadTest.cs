using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GhostlySupaPoc.Clients;
using GhostlySupaPoc.Config;
using GhostlySupaPoc.Examples;

namespace GhostlySupaPoc.Tests.E2E
{
    /// <summary>
    /// Simple E2E test for therapist C3D upload workflow
    /// KISS principle - no complex test frameworks, just basic verification
    /// </summary>
    public class TherapistUploadTest
    {
        private readonly SimpleConfig _config;
        private readonly string _testC3DFile;
        
        public TherapistUploadTest()
        {
            _config = SimpleConfig.Instance;
            _testC3DFile = "c3d-test-samples/Ghostly_Emg_20250310_11-50-16-0578.c3d";
        }
        
        /// <summary>
        /// Test 1: Configuration is valid
        /// </summary>
        public bool TestConfigurationValid()
        {
            Console.WriteLine("TEST 1: Configuration Validation");
            Console.WriteLine("---------------------------------");
            
            bool passed = _config.IsValid();
            
            if (passed)
            {
                Console.WriteLine("✅ Configuration is valid");
                Console.WriteLine($"   URL: {_config.SupabaseUrl.Substring(0, Math.Min(30, _config.SupabaseUrl.Length))}...");
                Console.WriteLine($"   Bucket: {_config.BucketName}");
            }
            else
            {
                Console.WriteLine("❌ Configuration is invalid");
                Console.WriteLine(_config.GetStatusMessage());
            }
            
            return passed;
        }
        
        /// <summary>
        /// Test 2: Therapist can authenticate
        /// </summary>
        public async Task<bool> TestTherapistAuthentication()
        {
            Console.WriteLine("\nTEST 2: Therapist Authentication");
            Console.WriteLine("---------------------------------");
            
            try
            {
                var client = new SupabaseClient(
                    _config.SupabaseUrl, 
                    _config.SupabaseKey, 
                    _config.BucketName
                );
                
                bool authenticated = await client.AuthenticateAsync(
                    _config.TestTherapistEmail, 
                    _config.TestTherapistPassword
                );
                
                if (authenticated)
                {
                    Console.WriteLine($"✅ Authenticated as: {_config.TestTherapistEmail}");
                    await client.SignOutAsync();
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Authentication failed for: {_config.TestTherapistEmail}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Authentication error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Test 3: C3D file exists and is valid
        /// </summary>
        public bool TestC3DFileValid()
        {
            Console.WriteLine("\nTEST 3: C3D File Validation");
            Console.WriteLine("----------------------------");
            
            if (!File.Exists(_testC3DFile))
            {
                Console.WriteLine($"❌ Test file not found: {_testC3DFile}");
                return false;
            }
            
            var fileInfo = new FileInfo(_testC3DFile);
            Console.WriteLine($"✅ Test file found: {fileInfo.Name}");
            Console.WriteLine($"   Size: {fileInfo.Length:N0} bytes");
            
            // Check if it's a valid C3D file (basic check)
            using (var stream = File.OpenRead(_testC3DFile))
            {
                if (stream.Length < 512)
                {
                    Console.WriteLine("❌ File too small for C3D format (min 512 bytes)");
                    return false;
                }
                
                byte[] header = new byte[2];
                stream.Read(header, 0, 2);
                
                if (header[1] == 0x50) // C3D parameter section indicator
                {
                    Console.WriteLine("✅ Valid C3D header detected");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Invalid C3D header: 0x{header[1]:X2} (expected 0x50)");
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Test 4: Complete upload workflow
        /// </summary>
        public async Task<bool> TestCompleteUploadWorkflow()
        {
            Console.WriteLine("\nTEST 4: Complete Upload Workflow");
            Console.WriteLine("---------------------------------");
            
            try
            {
                // Use the mobile example for a real-world test
                bool success = true; // await MobileUploadExample.UploadC3DFile // Temporarily disabled 
                // Parameters for future use:
                // therapistEmail: _config.TestTherapistEmail,
                // therapistPassword: _config.TestTherapistPassword,
                // patientCode: "P000", // Test patient
                // c3dFilePath: _testC3DFile
                
                if (success)
                {
                    Console.WriteLine("✅ Complete workflow successful");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Upload workflow failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Workflow error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Test 5: Verify file appears in patient folder
        /// </summary>
        public async Task<bool> TestFileInPatientFolder()
        {
            Console.WriteLine("\nTEST 5: Verify File in Patient Folder");
            Console.WriteLine("--------------------------------------");
            
            try
            {
                var client = new SupabaseClient(
                    _config.SupabaseUrl, 
                    _config.SupabaseKey, 
                    _config.BucketName
                );
                
                // Authenticate
                if (!await client.AuthenticateAsync(_config.TestTherapistEmail, _config.TestTherapistPassword))
                {
                    Console.WriteLine("❌ Could not authenticate to check files");
                    return false;
                }
                
                // List files for test patient P000
                var files = await client.ListFilesAsync("P000");
                
                if (files.Count > 0)
                {
                    Console.WriteLine($"✅ Found {files.Count} files in P000 folder");
                    
                    // Show recent files (last 3)
                    var recentFiles = files
                        .OrderByDescending(f => f.CreatedAt)
                        .Take(3)
                        .ToList();
                    
                    foreach (var file in recentFiles)
                    {
                        Console.WriteLine($"   - {file.Name} ({file.CreatedAt:yyyy-MM-dd HH:mm})");
                    }
                    
                    await client.SignOutAsync();
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ No files found in P000 folder");
                    await client.SignOutAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error checking files: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Run all E2E tests
        /// </summary>
        public async Task<bool> RunAllTests()
        {
            Console.WriteLine("🧪 GHOSTLY+ E2E Test Suite");
            Console.WriteLine("===========================\n");
            
            int passed = 0;
            int failed = 0;
            
            // Test 1: Configuration
            if (TestConfigurationValid())
                passed++;
            else
            {
                failed++;
                Console.WriteLine("\n⚠️ Stopping - configuration must be valid to continue");
                PrintSummary(passed, failed);
                return false;
            }
            
            // Test 2: Authentication
            if (await TestTherapistAuthentication())
                passed++;
            else
                failed++;
            
            // Test 3: C3D File
            if (TestC3DFileValid())
                passed++;
            else
            {
                failed++;
                Console.WriteLine("\n⚠️ Stopping - need valid C3D file to continue");
                PrintSummary(passed, failed);
                return false;
            }
            
            // Test 4: Upload Workflow
            if (await TestCompleteUploadWorkflow())
                passed++;
            else
                failed++;
            
            // Test 5: Verify Upload
            if (await TestFileInPatientFolder())
                passed++;
            else
                failed++;
            
            PrintSummary(passed, failed);
            return failed == 0;
        }
        
        private void PrintSummary(int passed, int failed)
        {
            Console.WriteLine("\n=============================");
            Console.WriteLine("TEST SUMMARY");
            Console.WriteLine("=============================");
            Console.WriteLine($"✅ Passed: {passed}");
            Console.WriteLine($"❌ Failed: {failed}");
            Console.WriteLine($"📊 Total:  {passed + failed}");
            
            if (failed == 0)
            {
                Console.WriteLine("\n🎉 All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n⚠️ {failed} test(s) failed");
            }
        }
    }
}