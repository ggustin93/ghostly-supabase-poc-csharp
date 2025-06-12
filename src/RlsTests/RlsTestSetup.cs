using Supabase;
using Supabase.Storage;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Postgrest.Models;
using Postgrest.Attributes;
using GhostlySupaPoc.Models; // Use the centralized models

namespace GhostlySupaPoc.RlsTests
{
    /// <summary>
    /// Handles the one-time setup for RLS tests.
    /// This class is responsible for uploading test files for each therapist's patient
    /// and creating the corresponding metadata in the emg_sessions table.
    /// This ensures our test environment is ready for validation.
    /// </summary>
    public static class RlsTestSetup
    {
        public static async Task PrepareTestEnvironment(Client supabase, string therapistEmail, string therapistPassword)
        {
            Console.WriteLine($"\n--- Preparing test environment for: {therapistEmail} ---");

            try
            {
                // 1. Authenticate as the therapist
                var session = await supabase.Auth.SignIn(therapistEmail, therapistPassword);
                if (session?.User == null)
                {
                    throw new Exception($"Authentication failed for {therapistEmail}.");
                }
                Console.WriteLine($"Successfully authenticated as {therapistEmail}.");

                // 2. Get the patient assigned to this therapist
                var patientResponse = await supabase.From<Patient>().Get();
                var patient = patientResponse.Models.FirstOrDefault();

                if (patient == null)
                {
                    Console.WriteLine($"No patient found for {therapistEmail}. This might be expected if the therapist has no patients.");
                    return; // Exit if no patient is assigned
                }
                Console.WriteLine($"Found assigned patient: {patient.FirstName} {patient.LastName} (ID: {patient.Id})");

                // 3. Prepare a dummy file for upload
                var fileName = $"test-session-{Guid.NewGuid()}.bin";
                var filePathInBucket = $"{patient.Id}/{fileName}";
                var fileContent = Encoding.UTF8.GetBytes($"This is a test EMG file for patient {patient.Id} at {DateTime.UtcNow}");
                var memoryStream = new MemoryStream(fileContent);

                // 4. Upload the file to the 'emg_data' bucket
                // The RLS storage policy will be enforced here.
                await supabase.Storage.From("emg_data").Upload(memoryStream, filePathInBucket);
                Console.WriteLine($"Successfully uploaded file to: {filePathInBucket}");

                // 5. Create the metadata record in emg_sessions
                var emgSession = new EmgSession
                {
                    PatientId = patient.Id,
                    FilePath = filePathInBucket,
                    RecordedAt = DateTime.UtcNow,
                    Notes = $"Test session created by automated setup for {therapistEmail}."
                };

                await supabase.From<EmgSession>().Insert(emgSession);
                Console.WriteLine("Successfully created EMG session metadata record.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during test setup for {therapistEmail}: {ex.Message}");
                throw; // Re-throw to fail the setup process if something goes wrong
            }
            finally
            {
                // Ensure we sign out to leave a clean state for the next run
                await supabase.Auth.SignOut();
            }
        }
    }
} 