using Supabase;
using Supabase.Storage;
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
    /// Handles the one-time setup for RLS tests.
    /// </summary>
    public static class RlsTestSetup
    {
        public static async Task PrepareTestEnvironment(Supabase.Client supabase, string therapistEmail, string therapistPassword, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"Preparing test environment for: {therapistEmail}");

            try
            {
                var session = await supabase.Auth.SignIn(therapistEmail, therapistPassword);
                if (session?.User == null)
                {
                    throw new Exception($"Authentication failed for {therapistEmail}.");
                }
                ConsoleHelper.WriteSuccess($"Successfully authenticated as {therapistEmail}.");

                var patientResponse = await supabase.From<Patient>().Get();
                var patient = patientResponse.Models.FirstOrDefault();

                if (patient == null)
                {
                    ConsoleHelper.WriteWarning($"No patient found for {therapistEmail}. This might be expected if the therapist has no patients.");
                    return;
                }
                
                // Display detailed patient information
                ConsoleHelper.WriteInfo($"Found assigned patient: {patient.FirstName} {patient.LastName} ({patient.PatientCode})");
                ConsoleHelper.WriteInfo($"  │ Patient ID (DB): {patient.Id}");
                ConsoleHelper.WriteInfo($"  └ Assigned to therapist: {patient.TherapistId}");

                // Create a test file with a readable, timestamp-based name
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"{patient.PatientCode}_EMG-Test_{timestamp}.bin";
                var filePathInBucket = $"{patient.PatientCode}/{fileName}";
                
                // Include session ID in file content for traceability
                var fileContent = Encoding.UTF8.GetBytes(
                    $"GHOSTLY+ Test EMG Data\n" +
                    $"Patient Code: {patient.PatientCode} ({patient.Id})\n" +
                    $"File Name: {fileName}\n" +
                    $"Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Therapist: {therapistEmail}\n" +
                    $"This is a test file created by the automated RLS validation tests."
                );
                var memoryStream = new MemoryStream(fileContent);

                await supabase.Storage.From(rlsTestBucket).Upload(memoryStream.ToArray(), filePathInBucket);
                ConsoleHelper.WriteSuccess($"Successfully uploaded file to: {filePathInBucket}");
                ConsoleHelper.WriteInfo($"  │ Bucket: {rlsTestBucket}");
                ConsoleHelper.WriteInfo($"  │ File Size: {fileContent.Length} bytes");
                ConsoleHelper.WriteInfo($"  └ File Name: {fileName}");

                var emgSession = new EmgSession
                {
                    PatientId = patient.Id,
                    FilePath = filePathInBucket,
                    RecordedAt = DateTime.UtcNow,
                    Notes = $"Test session created by automated setup for {therapistEmail}. File Name: {fileName}"
                };

                var response = await supabase.From<EmgSession>().Insert(emgSession);
                var createdSession = response.Models.FirstOrDefault();
                
                ConsoleHelper.WriteSuccess("Successfully created EMG session metadata record.");
                if (createdSession != null)
                {
                    ConsoleHelper.WriteInfo($"  │ Session ID in DB: {createdSession.Id}");
                    ConsoleHelper.WriteInfo($"  │ Recorded At: {createdSession.RecordedAt:yyyy-MM-dd HH:mm:ss}");
                    ConsoleHelper.WriteInfo($"  └ File Path: {createdSession.FilePath}");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"An error occurred during test setup for {therapistEmail}: {ex.Message}");
                throw;
            }
            finally
            {
                await supabase.Auth.SignOut();
            }
        }
    }
}