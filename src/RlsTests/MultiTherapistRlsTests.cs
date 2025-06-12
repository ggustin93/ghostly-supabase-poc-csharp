using Supabase;
using System;
using System.Linq;
using System.Threading.Tasks;
using GhostlySupaPoc.Models; // Use the centralized models

namespace GhostlySupaPoc.RlsTests
{
    /// <summary>
    /// Contains tests to validate the multi-therapist RLS policies.
    /// These tests ensure that a therapist can only access data and files
    /// belonging to patients explicitly assigned to them.
    /// </summary>
    public static class MultiTherapistRlsTests
    {
        public static async Task RunAllTests(Client supabase, string therapist1Email, string therapist1Password, string therapist2Email, string therapist2Password)
        {
            Console.WriteLine("\n\n=============================================");
            Console.WriteLine("= Starting Multi-Therapist RLS Validation Tests =");
            Console.WriteLine("=============================================");

            // --- Run tests for Therapist 1 ---
            await Test_CanAccessOwnData(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CanDownloadOwnFiles(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CannotAccessOthersData(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CannotDownloadOthersFiles(supabase, therapist1Email, therapist1Password, "Therapist 1", therapist2Email, therapist2Password);

            // --- Run tests for Therapist 2 ---
            // (In a real test suite, these would be separate tests, but for a POC, this is clear)
            await Test_CanAccessOwnData(supabase, therapist2Email, therapist2Password, "Therapist 2");
            await Test_CanDownloadOwnFiles(supabase, therapist2Email, therapist2Password, "Therapist 2");

            Console.WriteLine("\n\nRLS Validation Tests Completed.");
        }

        private static async Task Test_CanAccessOwnData(Client supabase, string email, string password, string therapistName)
        {
            Console.WriteLine($"\n--- TEST: {therapistName} can access their own data ---");
            await supabase.Auth.SignIn(email, password);

            var patientResponse = await supabase.From<Patient>().Get();
            var sessionResponse = await supabase.From<EmgSession>().Get();

            if (patientResponse.Models.Any() && sessionResponse.Models.Any())
            {
                Console.WriteLine($"SUCCESS: {therapistName} correctly fetched {patientResponse.Models.Count} patient(s) and {sessionResponse.Models.Count} session(s).");
            }
            else
            {
                throw new Exception($"FAILURE: {therapistName} could not fetch their own data.");
            }
            await supabase.Auth.SignOut();
        }

        private static async Task Test_CannotAccessOthersData(Client supabase, string email, string password, string therapistName)
        {
            Console.WriteLine($"\n--- TEST: {therapistName} CANNOT access data from other therapists ---");
            await supabase.Auth.SignIn(email, password);

            // This should return 0 patients, as the RLS policy on the server will override the filter.
            var patientResponse = await supabase.From<Patient>().Filter("last_name", Postgrest.Constants.Operator.Not, "eq", "Alpha").Get();

            if (!patientResponse.Models.Any())
            {
                Console.WriteLine($"SUCCESS: {therapistName} was correctly blocked from seeing other therapists' patients.");
            }
            else
            {
                throw new Exception($"FAILURE: {therapistName} was able to fetch {patientResponse.Models.Count} patient(s) that do not belong to them.");
            }
            await supabase.Auth.SignOut();
        }

        private static async Task Test_CanDownloadOwnFiles(Client supabase, string email, string password, string therapistName)
        {
            Console.WriteLine($"\n--- TEST: {therapistName} can download their own patient's files ---");
            await supabase.Auth.SignIn(email, password);

            var sessionResponse = await supabase.From<EmgSession>().Get();
            var session = sessionResponse.Models.First();

            var fileBytes = await supabase.Storage.From("emg_data").Download(session.FilePath);

            if (fileBytes != null && fileBytes.Length > 0)
            {
                Console.WriteLine($"SUCCESS: {therapistName} successfully downloaded file '{session.FilePath}'.");
            }
            else
            {
                throw new Exception($"FAILURE: {therapistName} failed to download their own file '{session.FilePath}'.");
            }
            await supabase.Auth.SignOut();
        }

        private static async Task Test_CannotDownloadOthersFiles(Client supabase, string attackerEmail, string attackerPassword, string attackerName, string victimEmail, string victimPassword)
        {
            Console.WriteLine($"\n--- TEST: {attackerName} CANNOT download files of another therapist's patient ---");

            // First, get the file path of the victim's file
            await supabase.Auth.SignIn(victimEmail, victimPassword);
            var victimSession = (await supabase.From<EmgSession>().Get()).Models.First();
            var victimFilePath = victimSession.FilePath;
            await supabase.Auth.SignOut();
            Console.WriteLine($"Obtained victim's file path: {victimFilePath}");

            // Now, log in as the attacker and try to download it
            await supabase.Auth.SignIn(attackerEmail, attackerPassword);
            try
            {
                await supabase.Storage.From("emg_data").Download(victimFilePath);
                // If we get here, the download succeeded, which is a security failure.
                throw new Exception($"SECURITY FAILURE: {attackerName} was able to download file '{victimFilePath}' which belongs to another therapist.");
            }
            catch (Supabase.Storage.Exceptions.StorageException ex)
            {
                // We expect a specific error, usually "The resource was not found" or a 4xx error.
                Console.WriteLine($"SUCCESS: {attackerName} was correctly blocked from downloading the file. Received expected error: {ex.Message}");
            }
            catch(Exception e)
            {
                 throw new Exception($"SECURITY FAILURE: {attackerName} was able to download file '{victimFilePath}' which belongs to another therapist.");
            }
            finally
            {
                await supabase.Auth.SignOut();
            }
        }
    }
} 