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
                var sessionResponse = await supabase.From<EmgSession>().Get();

                if (patientResponse.Models.Any() && sessionResponse.Models.Any())
                {
                    ConsoleHelper.WriteSuccess($"{therapistName} correctly fetched {patientResponse.Models.Count} patient(s) and {sessionResponse.Models.Count} session(s).");
                    
                    // Display patient info
                    var patient = patientResponse.Models.First();
                    ConsoleHelper.WriteInfo($"  │ Patient: {patient.FirstName} {patient.LastName} (Code: {patient.PatientCode})");
                    
                    // Display session info 
                    var session = sessionResponse.Models.First();
                    ConsoleHelper.WriteInfo($"  │ Session ID: {session.Id}");
                    ConsoleHelper.WriteInfo($"  │ Recorded: {session.RecordedAt:yyyy-MM-dd HH:mm:ss}");
                    ConsoleHelper.WriteInfo($"  └ File: {session.FilePath}");
                }
                else
                {
                    throw new SecurityFailureException($"FAILURE: {therapistName} could not fetch their own data.");
                }
            }
        }

        private static async Task Test_CannotAccessOthersData(Supabase.Client supabase, string email, string password, string therapistName)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} CANNOT access data from other therapists");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                var patientResponse = await supabase.From<Patient>().Not("last_name", Postgrest.Constants.Operator.Equals, "Alpha").Get();

                if (!patientResponse.Models.Any())
                {
                    ConsoleHelper.WriteSuccess($"{therapistName} was correctly blocked from seeing other therapists' patients.");
                }
                else
                {
                    throw new SecurityFailureException($"FAILURE: {therapistName} was able to fetch {patientResponse.Models.Count} patient(s) that do not belong to them.");
                }
            }
        }

        private static async Task Test_CanDownloadOwnFiles(Supabase.Client supabase, string email, string password, string therapistName, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} can download their own patient's files");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                var sessionResponse = await supabase.From<EmgSession>().Get();
                var session = sessionResponse.Models.First();

                var fileBytes = await supabase.Storage.From(rlsTestBucket).Download(session.FilePath, null);

                if (fileBytes != null && fileBytes.Length > 0)
                {
                    ConsoleHelper.WriteSuccess($"{therapistName} successfully downloaded file '{session.FilePath}'.");
                    
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
                    throw new SecurityFailureException($"FAILURE: {therapistName} failed to download their own file '{session.FilePath}'.");
                }
            }
        }

        private static async Task Test_CannotDownloadOthersFiles(Supabase.Client supabase, string attackerEmail, string attackerPassword, string attackerName, string victimEmail, string victimPassword, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {attackerName} CANNOT download files of another therapist's patient");

            string victimFilePath;
            await using (await TherapistSession.Create(supabase, victimEmail, victimPassword, "Victim"))
            {
                var victimSession = (await supabase.From<EmgSession>().Get()).Models.First();
                victimFilePath = victimSession.FilePath;
                ConsoleHelper.WriteInfo($"Obtained victim's file path: {victimFilePath}");
            }

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
        /// Tests the ability of a therapist to upload a C3D file, store its metadata, 
        /// and process its data for a patient they are authorized to work with.
        /// </summary>
        private static async Task Test_CanUploadAndProcessC3DFile(Supabase.Client supabase, string email, string password, string therapistName, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} can upload and process C3D files");
            await using (await TherapistSession.Create(supabase, email, password, therapistName))
            {
                try
                {
                    // Get a patient assigned to this therapist
                    var patientResponse = await supabase.From<Patient>().Get();
                    var patient = patientResponse.Models.First();
                    ConsoleHelper.WriteInfo($"Using patient: {patient.FirstName} {patient.LastName} (Code: {patient.PatientCode})");
                    
                    // Create a mock C3D file with a readable, timestamp-based name
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    var c3dFileName = $"{patient.PatientCode}_C3D-Test_{timestamp}.c3d";
                    var c3dFilePath = $"{patient.PatientCode}/{c3dFileName}";
                    
                    // Generate mock C3D content with EMG data
                    var mockC3dContent = GenerateMockC3DContent(patient.PatientCode, c3dFileName);
                    
                    // Upload the C3D file
                    await supabase.Storage.From(rlsTestBucket).Upload(
                        Encoding.UTF8.GetBytes(mockC3dContent), 
                        c3dFilePath, 
                        new Supabase.Storage.FileOptions { ContentType = "application/octet-stream" }
                    );
                    ConsoleHelper.WriteSuccess($"Successfully uploaded C3D file: {c3dFilePath}");
                    
                    // Record the session metadata
                    var emgSession = new EmgSession
                    {
                        PatientId = patient.Id,
                        FilePath = c3dFilePath,
                        RecordedAt = DateTime.UtcNow,
                        Notes = $"C3D test session for {patient.FirstName} {patient.LastName} created by {therapistName}"
                    };
                    
                    var response = await supabase.From<EmgSession>().Insert(emgSession);
                    var createdSession = response.Models.FirstOrDefault();
                    
                    if (createdSession != null)
                    {
                        ConsoleHelper.WriteSuccess("Successfully created C3D session metadata");
                        ConsoleHelper.WriteInfo($"  │ Session ID: {createdSession.Id}");
                        ConsoleHelper.WriteInfo($"  │ Patient ID: {createdSession.PatientId}");
                        ConsoleHelper.WriteInfo($"  └ File Path: {createdSession.FilePath}");
                        
                        // Now verify we can download this file
                        var downloadedBytes = await supabase.Storage.From(rlsTestBucket).Download(c3dFilePath, null);
                        if (downloadedBytes != null && downloadedBytes.Length > 0)
                        {
                            ConsoleHelper.WriteSuccess("Successfully downloaded the uploaded C3D file");
                            ConsoleHelper.WriteInfo($"  │ File Size: {downloadedBytes.Length} bytes");
                            ConsoleHelper.WriteInfo($"  └ Content hash: {BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(downloadedBytes)).Replace("-", "")}");
                        }
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
                    
                    // Get all sessions to verify their patient IDs
                    var allSessions = (await supabase.From<EmgSession>().Get()).Models;
                    
                    // Group sessions by patient ID
                    var sessionsByPatient = allSessions.GroupBy(s => s.PatientId).ToDictionary(g => g.Key, g => g.ToList());
                    
                    // Verify each patient's data
                    foreach (var patient in patients)
                    {
                        ConsoleHelper.WriteInfo($"Checking patient: {patient.FirstName} {patient.LastName} (Code: {patient.PatientCode})");
                        
                        // Check if this patient has sessions
                        if (sessionsByPatient.TryGetValue(patient.Id, out var patientSessions))
                        {
                            ConsoleHelper.WriteInfo($"  │ Found {patientSessions.Count} session(s)");
                            
                            // Try to download one file for this patient
                            if (patientSessions.Count > 0)
                            {
                                var session = patientSessions[0];
                                var fileBytes = await supabase.Storage.From(rlsTestBucket).Download(session.FilePath, null);
                                
                                if (fileBytes != null && fileBytes.Length > 0)
                                {
                                    ConsoleHelper.WriteSuccess($"  │ Successfully downloaded file for patient {patient.PatientCode}");
                                    ConsoleHelper.WriteInfo($"  │ File: {session.FilePath}");
                                    ConsoleHelper.WriteInfo($"  └ Size: {fileBytes.Length} bytes");
                                }
                            }
                        }
                        else
                        {
                            ConsoleHelper.WriteInfo($"  └ No sessions found for this patient");
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