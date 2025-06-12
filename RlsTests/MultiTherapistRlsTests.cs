using NUnit.Framework;
using Supabase;
using Supabase.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;
using GhostlySupaPoc.Models;

namespace GhostlySupaPoc.RlsTests
{
    /// <summary>
    /// This class contains a suite of tests to validate the Row Level Security (RLS) policies
    /// for the Supabase backend. It tests the ability of therapists to access and download
    /// data belonging to patients explicitly assigned to them.
    /// </summary>
    public static class MultiTherapistRlsTests
    {
        public static async Task RunAllTests(Client supabase, string therapist1Email, string therapist1Password, string therapist2Email, string therapist2Password, string rlsTestBucket)
        {
            Console.WriteLine("\n\n=============================================");
            Console.WriteLine("= Starting Multi-Therapist RLS Validation Tests =");
            Console.WriteLine("=============================================");

            // --- Run tests for Therapist 1 ---
            await Test_CanAccessOwnData(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CanDownloadOwnFiles(supabase, therapist1Email, therapist1Password, "Therapist 1", rlsTestBucket);
            await Test_CannotAccessOthersData(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CannotDownloadOthersFiles(supabase, therapist1Email, therapist1Password, "Therapist 1", therapist2Email, therapist2Password, rlsTestBucket);

            // --- Run tests for Therapist 2 ---
            // (In a real test suite, these would be separate tests, but for a POC, this is clear)
            await Test_CanAccessOwnData(supabase, therapist2Email, therapist2Password, "Therapist 2");
            await Test_CanDownloadOwnFiles(supabase, therapist2Email, therapist2Password, "Therapist 2", rlsTestBucket);

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

        private static async Task Test_CanDownloadOwnFiles(Client supabase, string email, string password, string therapistName, string rlsTestBucket)
        {
            Console.WriteLine($"\n--- TEST: {therapistName} can download their own patient's files ---");
            await supabase.Auth.SignIn(email, password);

            var sessionResponse = await supabase.From<EmgSession>().Get();
            var session = sessionResponse.Models.First();

            var fileBytes = await supabase.Storage.From(rlsTestBucket).Download(session.FilePath);

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

        private static async Task Test_CannotDownloadOthersFiles(Client supabase, string attackerEmail, string attackerPassword, string attackerName, string victimEmail, string victimPassword, string rlsTestBucket)
        {
            Console.WriteLine($"\n--- TEST: {attackerName} CANNOT download files of another therapist's patient ---");

            await supabase.Auth.SignIn(victimEmail, victimPassword);
            var victimSession = (await supabase.From<EmgSession>().Get()).Models.First();
            var victimFilePath = victimSession.FilePath;
            await supabase.Auth.SignOut();
            Console.WriteLine($"Obtained victim's file path: {victimFilePath}");

            await supabase.Auth.SignIn(attackerEmail, attackerPassword);
            try
            {
                await supabase.Storage.From(rlsTestBucket).Download(victimFilePath);
                throw new Exception($"SECURITY FAILURE: {attackerName} was able to download file '{victimFilePath}' which belongs to another therapist.");
            }
            catch (Supabase.Storage.Exceptions.StorageException ex)
            {
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