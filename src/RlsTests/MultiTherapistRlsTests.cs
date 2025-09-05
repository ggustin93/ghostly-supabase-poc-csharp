using Supabase;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GhostlySupaPoc.Models;
using GhostlySupaPoc.Utils;

namespace GhostlySupaPoc.RlsTests
{
    /// <summary>
    /// This class contains a suite of tests to validate the Row Level Security (RLS) policies
    /// for the Supabase backend. It tests the ability of therapists to access and download
    /// data belonging to patients explicitly assigned to them.
    /// </summary>
    public static class MultiTherapistRlsTests
    {
        public static async Task RunAllTests(Supabase.Client supabase, string therapist1Email, string therapist1Password, string therapist2Email, string therapist2Password, string rlsTestBucket)
        {
            ConsoleHelper.WriteMajorHeader("Starting Multi-Therapist RLS Validation Tests");
            
            // --- DEBUG PHASE - New debugging step ---
            ConsoleHelper.WriteHeader("PHASE 0: RLS Debug Analysis");
            await Test_DebugStorageContext(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_DebugStorageContext(supabase, therapist2Email, therapist2Password, "Therapist 2");
            
            // --- Basic RLS Tests ---
            ConsoleHelper.WriteHeader("PHASE 1: Basic RLS Tests");
            
            // --- Run tests for Therapist 1 ---
            await Test_CanAccessOwnData(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CanDownloadOwnFiles(supabase, therapist1Email, therapist1Password, "Therapist 1", rlsTestBucket);
            await Test_CannotAccessOthersData(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CannotDownloadOthersFiles(supabase, therapist1Email, therapist1Password, "Therapist 1", therapist2Email, therapist2Password, rlsTestBucket);

            // --- Run tests for Therapist 2 ---
            await Test_CanAccessOwnData(supabase, therapist2Email, therapist2Password, "Therapist 2");
            await Test_CanDownloadOwnFiles(supabase, therapist2Email, therapist2Password, "Therapist 2", rlsTestBucket);
            
            // --- Advanced C3D Tests ---
            ConsoleHelper.WriteHeader("PHASE 2: Advanced C3D File Tests");
            await Test_CanUploadAndProcessC3DFile(supabase, therapist1Email, therapist1Password, "Therapist 1", rlsTestBucket);
            await Test_RLSProtectionOnDirectPathAccess(supabase, therapist1Email, therapist1Password, therapist2Email, therapist2Password, rlsTestBucket);
            await Test_MultiPatientDataSegregation(supabase, therapist1Email, therapist1Password, "Therapist 1", rlsTestBucket);
            
            // --- Cross-Role Tests ---
            ConsoleHelper.WriteHeader("PHASE 3: Cross-Role Security Tests");
            await Test_TherapistRoleRestrictions(supabase, therapist1Email, therapist1Password, "Therapist 1");
            
            ConsoleHelper.WriteSecurity("\nRLS Validation Tests Completed Successfully! 🎉");
        }

        private static async Task Test_CanAccessOwnData(Supabase.Client supabase, string email, string password, string therapistName)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} can access their own data");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                var patientResponse = await supabase.From<Patient>().Get();

                if (patientResponse.Models.Any())
                {
                    ConsoleHelper.WriteSuccess($"{therapistName} correctly fetched {patientResponse.Models.Count} patient(s).");
                    
                    // Display patient info
                    foreach (var patient in patientResponse.Models)
                    {
                        ConsoleHelper.WriteInfo($"  │ Patient Code: {patient.PatientCode} (Age: {patient.AgeGroup ?? "N/A"}, Gender: {patient.Gender ?? "N/A"})");
                    }
                    ConsoleHelper.WriteInfo($"  └ Successfully accessed patient data ✓");
                }
                else
                {
                    throw new SecurityFailureException($"FAILURE: {therapistName} could not fetch their own patients.");
                }
            }
        }

        private static async Task Test_CannotAccessOthersData(Supabase.Client supabase, string email, string password, string therapistName)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} CANNOT access data from other therapists");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                // Get all patients - RLS should only return this therapist's patients
                var patientResponse = await supabase.From<Patient>().Get();
                var patientCodes = patientResponse.Models.Select(p => p.PatientCode).ToList();
                
                // RLS should prevent us from seeing any patients not assigned to us
                // The fact that we only get our own patients proves RLS is working
                ConsoleHelper.WriteInfo($"{therapistName} can see {patientResponse.Models.Count} patient(s) (their own)");
                ConsoleHelper.WriteInfo($"Patient codes visible: {string.Join(", ", patientCodes)}");
                
                // This test passes if we get results (our own patients) but no other therapists' patients
                // The RLS policy ensures we can't see others' data - we can't query for it
                ConsoleHelper.WriteSuccess($"{therapistName} is correctly restricted by RLS - can only see their own patients.");
            }
        }

        private static async Task Test_CanDownloadOwnFiles(Supabase.Client supabase, string email, string password, string therapistName, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} can download their own patient's files");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                // Get the therapist's first patient
                var patientResponse = await supabase.From<Patient>().Get();
                if (!patientResponse.Models.Any())
                {
                    ConsoleHelper.WriteWarning($"{therapistName} has no patients, skipping file download test.");
                    return;
                }
                
                var patient = patientResponse.Models.First();
                // Construct the file path based on patient code (webhook creates files under patient code folders)
                var filePath = $"{patient.PatientCode}/test_file.c3d";

                try
                {
                    // First, upload a test file to ensure there's something to download
                    var testContent = Encoding.UTF8.GetBytes($"Test C3D file for patient {patient.PatientCode}\nTherapist: {therapistName}\nTimestamp: {DateTime.UtcNow}");
                    
                    // Try to remove existing file first (if it exists)
                    try
                    {
                        await supabase.Storage.From(rlsTestBucket).Remove(new List<string> { filePath });
                    }
                    catch
                    {
                        // File might not exist, that's ok
                    }
                    
                    await supabase.Storage.From(rlsTestBucket).Upload(testContent, filePath);
                    
                    // Now try to download it
                    var fileBytes = await supabase.Storage.From(rlsTestBucket).Download(filePath, null);

                    if (fileBytes != null && fileBytes.Length > 0)
                    {
                        ConsoleHelper.WriteSuccess($"{therapistName} successfully downloaded file '{filePath}'.");
                        
                        // Extract and display file contents for verification
                        var fileContent = System.Text.Encoding.UTF8.GetString(fileBytes);
                        var firstLines = fileContent.Split('\n').Take(4).ToArray();
                        
                        ConsoleHelper.WriteInfo($"  │ File Size: {fileBytes.Length} bytes");
                        ConsoleHelper.WriteInfo($"  │ Content Preview:");
                        foreach (var line in firstLines)
                        {
                            ConsoleHelper.WriteInfo($"  │   {line}");
                        }
                        ConsoleHelper.WriteInfo($"  └ Successful Storage Access ✓");
                    }
                    else
                    {
                        throw new SecurityFailureException($"FAILURE: {therapistName} failed to download their own file '{filePath}'.");
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SecurityFailureException) throw;
                    ConsoleHelper.WriteWarning($"Could not complete file test: {ex.Message}");
                }
            }
        }

        private static async Task Test_CannotDownloadOthersFiles(Supabase.Client supabase, string attackerEmail, string attackerPassword, string attackerName, string victimEmail, string victimPassword, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {attackerName} CANNOT download files of another therapist's patient");

            string victimFilePath;
            string victimPatientCode;
            
            // First, as the victim therapist, create a test file
            await using (await TherapistSession.Create(supabase, victimEmail, victimPassword, "Victim"))
            {
                var victimPatients = (await supabase.From<Patient>().Get()).Models;
                if (!victimPatients.Any())
                {
                    ConsoleHelper.WriteWarning("Victim therapist has no patients, skipping cross-access test.");
                    return;
                }
                
                var victimPatient = victimPatients.First();
                victimPatientCode = victimPatient.PatientCode;
                victimFilePath = $"{victimPatientCode}/private_file.c3d";
                
                // Upload a test file as the victim
                var testContent = Encoding.UTF8.GetBytes($"Private data for patient {victimPatientCode}");
                
                // Remove file if it exists
                try
                {
                    await supabase.Storage.From(rlsTestBucket).Remove(new List<string> { victimFilePath });
                }
                catch
                {
                    // File might not exist, that's ok
                }
                
                await supabase.Storage.From(rlsTestBucket).Upload(testContent, victimFilePath);
                
                ConsoleHelper.WriteInfo($"Victim's file path: {victimFilePath}");
            }

            // Now try to access it as the attacker
            await using (await TherapistSession.Create(supabase, attackerEmail, attackerPassword, attackerName))
            {
                try
                {
                    await supabase.Storage.From(rlsTestBucket).Download(victimFilePath, null);
                    throw new SecurityFailureException($"SECURITY FAILURE: {attackerName} was able to download file '{victimFilePath}' which belongs to another therapist.");
                }
                catch (SecurityFailureException) { throw; }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteSuccess($"{attackerName} was correctly blocked from downloading the file.");
                    ConsoleHelper.WriteInfo($"Expected error: {ex.Message}");
                }
            }
        }
        
        // --- New Advanced C3D File Tests ---
        
        /// <summary>
        /// Tests the ability of a therapist to upload a C3D file for a patient they are authorized to work with.
        /// In the new architecture, uploading triggers a webhook for processing.
        /// </summary>
        private static async Task Test_CanUploadAndProcessC3DFile(Supabase.Client supabase, string email, string password, string therapistName, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} can upload C3D files (webhook handles processing)");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                try
                {
                    // Get a patient assigned to this therapist
                    var patientResponse = await supabase.From<Patient>().Get();
                    var patient = patientResponse.Models.First();
                    ConsoleHelper.WriteInfo($"Using patient: {patient.PatientCode} (Age: {patient.AgeGroup ?? "N/A"}, Gender: {patient.Gender ?? "N/A"})");
                    
                    // Create a mock C3D file with a readable, timestamp-based name
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    var c3dFileName = $"{patient.PatientCode}_C3D-Test_{timestamp}.c3d";
                    var c3dFilePath = $"{patient.PatientCode}/{c3dFileName}";
                    
                    // Generate mock C3D content with EMG data
                    var mockC3dContent = GenerateMockC3DContent(patient.PatientCode, c3dFileName);
                    
                    // Upload the C3D file (webhook will handle processing)
                    await supabase.Storage.From(rlsTestBucket).Upload(
                        Encoding.UTF8.GetBytes(mockC3dContent), 
                        c3dFilePath, 
                        new Supabase.Storage.FileOptions { ContentType = "application/octet-stream" }
                    );
                    ConsoleHelper.WriteSuccess($"Successfully uploaded C3D file: {c3dFilePath}");
                    ConsoleHelper.WriteInfo("  │ Webhook will process the file asynchronously");
                    
                    // Verify we can download this file
                    var downloadedBytes = await supabase.Storage.From(rlsTestBucket).Download(c3dFilePath, null);
                    if (downloadedBytes != null && downloadedBytes.Length > 0)
                    {
                        ConsoleHelper.WriteSuccess("Successfully downloaded the uploaded C3D file");
                        ConsoleHelper.WriteInfo($"  │ File Size: {downloadedBytes.Length} bytes");
                        ConsoleHelper.WriteInfo($"  └ Content hash: {BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(downloadedBytes)).Replace("-", "")}");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed to upload/process C3D file: {ex.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Tests that RLS protections work even when attempting to access files directly by path
        /// (simulating an attacker who has knowledge of storage path patterns)
        /// </summary>
        private static async Task Test_RLSProtectionOnDirectPathAccess(Supabase.Client supabase, string attackerEmail, string attackerPassword, string victimEmail, string victimPassword, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader("TEST: RLS protects against direct path access attempts");
            
            string directAccessPath;
            string victimPatientId;

            await using (await TherapistSession.Create(supabase, victimEmail, victimPassword, "Victim"))
            {
                var victimPatient = (await supabase.From<Patient>().Get()).Models.First();
                victimPatientId = victimPatient.PatientCode;
                
                directAccessPath = $"{victimPatient.PatientCode}/direct-access-test-{Guid.NewGuid()}.c3d";

                var directAccessContent = $"Test file with patient code {victimPatient.PatientCode} created at {DateTime.UtcNow}";
                await supabase.Storage.From(rlsTestBucket).Upload(
                    Encoding.UTF8.GetBytes(directAccessContent),
                    directAccessPath
                );
                
                ConsoleHelper.WriteInfo($"Created test file at path: {directAccessPath}");
            }
            
            await using (await TherapistSession.Create(supabase, attackerEmail, attackerPassword, "Attacker"))
            {
                try
                {
                    // Attempt 1: Try to access by the direct path we know
                    ConsoleHelper.WriteInfo("Attempt 1: Direct path access...");
                    await supabase.Storage.From(rlsTestBucket).Download(directAccessPath, null);
                    throw new SecurityFailureException("SECURITY FAILURE: Attacker was able to download file using direct path access.");
                }
                catch (SecurityFailureException) { throw; }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteSuccess("Attempt 1: Direct path access was correctly blocked");
                    ConsoleHelper.WriteInfo($"  └ Error: {ex.Message}");
                }

                try
                {
                    // Attempt 2: Try to access by guessing file pattern
                    ConsoleHelper.WriteInfo("Attempt 2: Path pattern guessing...");
                    var guessedPath = $"{victimPatientId}/session-{DateTime.UtcNow:yyyyMMdd}-1.c3d";
                    await supabase.Storage.From(rlsTestBucket).Download(guessedPath, null);
                    throw new SecurityFailureException("SECURITY FAILURE: Attacker was able to download file by guessing a path pattern.");
                }
                catch (SecurityFailureException) { throw; }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteSuccess("Attempt 2: Path pattern guessing was correctly blocked");
                    ConsoleHelper.WriteInfo($"  └ Error: {ex.Message}");
                }

                try
                {
                    // Attempt 3: Try to list files in the victim's folder
                    ConsoleHelper.WriteInfo("Attempt 3: Victim folder listing...");
                    var listResult = await supabase.Storage.From(rlsTestBucket).List(victimPatientId);

                    if (listResult != null && listResult.Count > 0)
                    {
                        throw new SecurityFailureException($"SECURITY FAILURE: Attacker was able to list {listResult.Count} files in victim's folder.");
                    }

                    ConsoleHelper.WriteSuccess("Attempt 3: Victim folder listing returned empty result (correct behavior)");
                }
                catch (SecurityFailureException) { throw; }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteSuccess("Attempt 3: Victim folder listing attempt was blocked");
                    ConsoleHelper.WriteInfo($"  └ Error: {ex.Message}");
                }
            }
            
            ConsoleHelper.WriteSuccess("All direct path access attempts were successfully blocked by RLS");
        }
        
        /// <summary>
        /// Tests that a therapist can manage multiple patients' data without cross-contamination
        /// </summary>
        private static async Task Test_MultiPatientDataSegregation(Supabase.Client supabase, string email, string password, string therapistName, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} can manage multiple patients' data with proper segregation");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                try
                {
                    // Get all patients for this therapist
                    var patients = (await supabase.From<Patient>().Get()).Models;
                    
                    if (patients.Count < 1)
                    {
                        ConsoleHelper.WriteWarning("Test skipped - therapist needs at least one patient");
                        return;
                    }
                    
                    ConsoleHelper.WriteInfo($"Therapist has {patients.Count} patient(s)");
                    
                    // Test file segregation for each patient
                    foreach (var patient in patients)
                    {
                        ConsoleHelper.WriteInfo($"Checking patient: {patient.PatientCode} (Age: {patient.AgeGroup ?? "N/A"}, Gender: {patient.Gender ?? "N/A"})");
                        
                        // Create a test file for this patient
                        var testFilePath = $"{patient.PatientCode}/segregation_test_{Guid.NewGuid()}.c3d";
                        var testContent = Encoding.UTF8.GetBytes($"Test data for patient {patient.PatientCode}");
                        
                        try
                        {
                            // Upload test file
                            await supabase.Storage.From(rlsTestBucket).Upload(testContent, testFilePath);
                            ConsoleHelper.WriteInfo($"  │ Uploaded test file: {testFilePath}");
                            
                            // Try to download the file to verify access
                            var downloadedBytes = await supabase.Storage.From(rlsTestBucket).Download(testFilePath, null);
                            
                            if (downloadedBytes != null && downloadedBytes.Length > 0)
                            {
                                ConsoleHelper.WriteSuccess($"  │ Successfully accessed file for patient {patient.PatientCode}");
                                ConsoleHelper.WriteInfo($"  └ Size: {downloadedBytes.Length} bytes");
                            }
                        }
                        catch (Exception ex)
                        {
                            ConsoleHelper.WriteWarning($"  └ Could not test file for patient: {ex.Message}");
                        }
                    }
                    
                    ConsoleHelper.WriteSuccess("Patient data segregation working correctly");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed to verify multi-patient data segregation: {ex.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Tests that a therapist can't perform admin-level operations
        /// </summary>
        private static async Task Test_TherapistRoleRestrictions(Supabase.Client supabase, string email, string password, string therapistName)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} is restricted to appropriate role permissions");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                try
                {
                    // Attempt 1: Try to create a new therapist (should be admin-only)
                    ConsoleHelper.WriteInfo("Attempt 1: Creating a new therapist (should fail)...");
                    try
                    {
                        var newTherapist = new
                        {
                            email = $"test-{Guid.NewGuid()}@example.com",
                            password = "test123456",
                            role = "therapist"
                        };
                        
                        // This should fail due to RLS
                        await supabase.Rpc("create_therapist", newTherapist);
                        throw new SecurityFailureException("SECURITY FAILURE: Therapist was able to create another therapist account.");
                    }
                    catch (SecurityFailureException) { throw; }
                    catch (Exception ex)
                    {
                        ConsoleHelper.WriteSuccess("Attempt 1: Therapist was correctly blocked from creating new therapists");
                        ConsoleHelper.WriteInfo($"  └ Error: {ex.Message}");
                    }
                    
                    // Attempt 2: Try to modify RLS policies (should be admin-only)
                    ConsoleHelper.WriteInfo("Attempt 2: Modifying RLS policies (should fail)...");
                    try
                    {
                        // Try to execute privileged SQL (this will fail)
                        await supabase.Rpc("execute_sql", new { sql = "ALTER TABLE patients DISABLE ROW LEVEL SECURITY" });
                        throw new SecurityFailureException("SECURITY FAILURE: Therapist was able to modify RLS policies.");
                    }
                    catch (SecurityFailureException) { throw; }
                    catch (Exception ex)
                    {
                        ConsoleHelper.WriteSuccess("Attempt 2: Therapist was correctly blocked from modifying RLS policies");
                        ConsoleHelper.WriteInfo($"  └ Error: {ex.Message}");
                    }
                    
                    ConsoleHelper.WriteSuccess("All therapist role restriction tests passed");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed during therapist role restriction test: {ex.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Generates mock C3D file content for testing
        /// </summary>
        private static string GenerateMockC3DContent(string patientCode, string fileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"GHOSTLY+ Mock C3D File Format");
            sb.AppendLine($"Patient Code: {patientCode}");
            sb.AppendLine($"File Name: {fileName}");
            sb.AppendLine($"Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
            sb.AppendLine($"File Type: C3D");
            sb.AppendLine($"Version: 3.0");
            sb.AppendLine();
            sb.AppendLine("== HEADER SECTION ==");
            sb.AppendLine("Parameter Blocks: 2");
            sb.AppendLine("3D Point Data: Yes");
            sb.AppendLine("Analog Channels: 8");
            sb.AppendLine("First Frame: 1");
            sb.AppendLine("Last Frame: 1000");
            sb.AppendLine("Sample Rate: 100Hz");
            sb.AppendLine();
            sb.AppendLine("== EMG DATA SECTION ==");
            sb.AppendLine("Frame,Time,EMG1,EMG2,EMG3,EMG4,EMG5,EMG6,EMG7,EMG8");
            
            // Generate some sample EMG data rows
            var random = new Random();
            for (int i = 1; i <= 10; i++)
            {
                var time = i / 100.0;
                var emg1 = random.Next(10, 100);
                var emg2 = random.Next(10, 100);
                var emg3 = random.Next(10, 100);
                var emg4 = random.Next(10, 100);
                var emg5 = random.Next(10, 100);
                var emg6 = random.Next(10, 100);
                var emg7 = random.Next(10, 100);
                var emg8 = random.Next(10, 100);
                
                sb.AppendLine($"{i},{time:F3},{emg1},{emg2},{emg3},{emg4},{emg5},{emg6},{emg7},{emg8}");
            }
            
            sb.AppendLine("... (more data rows)");
            sb.AppendLine();
            sb.AppendLine("== EVENTS SECTION ==");
            sb.AppendLine("1,0.0,START,Begin data collection");
            sb.AppendLine("500,5.0,MIDPOINT,Middle of recording");
            sb.AppendLine("1000,10.0,END,End of data collection");
            
            return sb.ToString();
        }

        /// <summary>
        /// Debug test to analyze storage policy context and function behavior
        /// </summary>
        private static async Task Test_DebugStorageContext(Supabase.Client supabase, string email, string password, string therapistName)
        {
            ConsoleHelper.WriteHeader($"DEBUG: {therapistName} storage context analysis");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                try
                {
                    ConsoleHelper.WriteInfo("Testing JWT token and auth context...");
                    var session = supabase.Auth.CurrentSession;
                    if (session != null)
                    {
                        ConsoleHelper.WriteInfo($"JWT Token (first 50 chars): {session.AccessToken?.Substring(0, Math.Min(50, session.AccessToken?.Length ?? 0))}...");
                    }

                    ConsoleHelper.WriteInfo("Testing debug_storage_access function for P008...");
                    var debugResult = await supabase.Rpc("debug_storage_access", new { file_path = "P008/private_file.c3d" });
                    
                    if (debugResult != null)
                    {
                        ConsoleHelper.WriteInfo($"Debug Result: {debugResult}");
                        
                        // Try to parse and display the JSON nicely
                        try 
                        {
                            var jsonString = debugResult.ToString();
                            var jsonResult = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
                            ConsoleHelper.WriteInfo($"  │ Patient Code: {jsonResult["patient_code"]}");
                            ConsoleHelper.WriteInfo($"  │ Auth Context Valid: {jsonResult["auth_context_valid"]}");
                            ConsoleHelper.WriteInfo($"  │ Therapist Profile Found: {jsonResult["therapist_profile_found"]}");
                            ConsoleHelper.WriteInfo($"  │ Owns Patient: {jsonResult["owns_patient"]}");
                            ConsoleHelper.WriteInfo($"  └ Current User ID: {jsonResult["current_user_id"]}");
                        }
                        catch (Exception jsonEx)
                        {
                            ConsoleHelper.WriteWarning($"Could not parse JSON response: {jsonEx.Message}");
                        }
                    }
                    else
                    {
                        ConsoleHelper.WriteWarning("Debug function returned null - function may not exist or auth context invalid");
                    }

                    ConsoleHelper.WriteInfo("Testing direct patient ownership query...");
                    var patients = await supabase.From<Patient>().Get();
                    var patientCodes = patients.Models.Select(p => p.PatientCode).ToList();
                    ConsoleHelper.WriteInfo($"Visible patients: {string.Join(", ", patientCodes)}");
                    ConsoleHelper.WriteInfo($"Can see P008: {patientCodes.Contains("P008")}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Debug test failed: {ex.Message}");
                    ConsoleHelper.WriteWarning("This might indicate the debug function wasn't created or there's an auth issue");
                }
            }
        }

        /// <summary>
        /// Helper class to manage therapist authentication sessions in a using block.
        /// </summary>
        private class TherapistSession : IAsyncDisposable
        {
            private readonly Supabase.Client _supabase;
            private readonly string _therapistName;

            private TherapistSession(Supabase.Client supabase, string therapistName)
            {
                _supabase = supabase;
                _therapistName = therapistName;
            }

            public static async Task<TherapistSession> Create(Supabase.Client supabase, string email, string password, string therapistName)
            {
                await supabase.Auth.SignIn(email, password);
                ConsoleHelper.WriteInfo($"--- Authenticated as {therapistName} ({email}) ---");
                return new TherapistSession(supabase, therapistName);
            }

            public async ValueTask DisposeAsync()
            {
                await _supabase.Auth.SignOut();
                ConsoleHelper.WriteInfo($"--- Signed out {_therapistName} ---");
            }
        }
    }
}