using Supabase;
using Supabase.Storage;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GhostlySupaPoc.Models;

namespace GhostlySupaPoc.RlsTests
{
    /// <summary>
    /// Handles the one-time setup for RLS tests.
    /// </summary>
    public static class RlsTestSetup
    {
        public static async Task PrepareTestEnvironment(Supabase.Client supabase, string therapistEmail, string therapistPassword, string rlsTestBucket)
        {
            Console.WriteLine($"\n--- Preparing test environment for: {therapistEmail} ---");

            try
            {
                var session = await supabase.Auth.SignIn(therapistEmail, therapistPassword);
                if (session?.User == null)
                {
                    throw new Exception($"Authentication failed for {therapistEmail}.");
                }
                Console.WriteLine($"Successfully authenticated as {therapistEmail}.");

                var patientResponse = await supabase.From<Patient>().Get();
                var patient = patientResponse.Models.FirstOrDefault();

                if (patient == null)
                {
                    Console.WriteLine($"No patient found for {therapistEmail}. This might be expected if the therapist has no patients.");
                    return;
                }
                Console.WriteLine($"Found assigned patient: {patient.FirstName} {patient.LastName} (ID: {patient.Id})");

                var fileName = $"test-session-{Guid.NewGuid()}.bin";
                var filePathInBucket = $"{patient.Id}/{fileName}";
                var fileContent = Encoding.UTF8.GetBytes($"This is a test EMG file for patient {patient.Id} at {DateTime.UtcNow}");
                var memoryStream = new MemoryStream(fileContent);

                await supabase.Storage.From(rlsTestBucket).Upload(memoryStream.ToArray(), filePathInBucket);
                Console.WriteLine($"Successfully uploaded file to: {filePathInBucket}");

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
                throw;
            }
            finally
            {
                await supabase.Auth.SignOut();
            }
        }
    }
}