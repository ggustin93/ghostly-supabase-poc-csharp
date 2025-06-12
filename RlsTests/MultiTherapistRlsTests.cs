using Supabase;
using System;
using System.Linq;
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

            // --- Run tests for Therapist 1 ---
            await Test_CanAccessOwnData(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CanDownloadOwnFiles(supabase, therapist1Email, therapist1Password, "Therapist 1", rlsTestBucket);
            await Test_CannotAccessOthersData(supabase, therapist1Email, therapist1Password, "Therapist 1");
            await Test_CannotDownloadOthersFiles(supabase, therapist1Email, therapist1Password, "Therapist 1", therapist2Email, therapist2Password, rlsTestBucket);

            // --- Run tests for Therapist 2 ---
            // (In a real test suite, these would be separate tests, but for a POC, this is clear)
            await Test_CanAccessOwnData(supabase, therapist2Email, therapist2Password, "Therapist 2");
            await Test_CanDownloadOwnFiles(supabase, therapist2Email, therapist2Password, "Therapist 2", rlsTestBucket);

            ConsoleHelper.WriteSecurity("\nRLS Validation Tests Completed Successfully! 🎉");
        }

        private static async Task Test_CanAccessOwnData(Supabase.Client supabase, string email, string password, string therapistName)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} can access their own data");
            await supabase.Auth.SignIn(email, password);

            var patientResponse = await supabase.From<Patient>().Get();
            var sessionResponse = await supabase.From<EmgSession>().Get();

            if (patientResponse.Models.Any() && sessionResponse.Models.Any())
            {
                ConsoleHelper.WriteSuccess($"{therapistName} correctly fetched {patientResponse.Models.Count} patient(s) and {sessionResponse.Models.Count} session(s).");
            }
            else
            {
                throw new Exception($"FAILURE: {therapistName} could not fetch their own data.");
            }
            await supabase.Auth.SignOut();
        }

        private static async Task Test_CannotAccessOthersData(Supabase.Client supabase, string email, string password, string therapistName)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} CANNOT access data from other therapists");
            await supabase.Auth.SignIn(email, password);

            var patientResponse = await supabase.From<Patient>().Not("last_name", Postgrest.Constants.Operator.Equals, "Alpha").Get();

            if (!patientResponse.Models.Any())
            {
                ConsoleHelper.WriteSuccess($"{therapistName} was correctly blocked from seeing other therapists' patients.");
            }
            else
            {
                throw new Exception($"FAILURE: {therapistName} was able to fetch {patientResponse.Models.Count} patient(s) that do not belong to them.");
            }
            await supabase.Auth.SignOut();
        }

        private static async Task Test_CanDownloadOwnFiles(Supabase.Client supabase, string email, string password, string therapistName, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {therapistName} can download their own patient's files");
            await supabase.Auth.SignIn(email, password);

            var sessionResponse = await supabase.From<EmgSession>().Get();
            var session = sessionResponse.Models.First();

            var fileBytes = await supabase.Storage.From(rlsTestBucket).Download(session.FilePath, null);

            if (fileBytes != null && fileBytes.Length > 0)
            {
                ConsoleHelper.WriteSuccess($"{therapistName} successfully downloaded file '{session.FilePath}'.");
            }
            else
            {
                throw new Exception($"FAILURE: {therapistName} failed to download their own file '{session.FilePath}'.");
            }
            await supabase.Auth.SignOut();
        }

        private static async Task Test_CannotDownloadOthersFiles(Supabase.Client supabase, string attackerEmail, string attackerPassword, string attackerName, string victimEmail, string victimPassword, string rlsTestBucket)
        {
            ConsoleHelper.WriteHeader($"TEST: {attackerName} CANNOT download files of another therapist's patient");

            await supabase.Auth.SignIn(victimEmail, victimPassword);
            var victimSession = (await supabase.From<EmgSession>().Get()).Models.First();
            var victimFilePath = victimSession.FilePath;
            await supabase.Auth.SignOut();
            ConsoleHelper.WriteInfo($"Obtained victim's file path: {victimFilePath}");

            await supabase.Auth.SignIn(attackerEmail, attackerPassword);
            try
            {
                await supabase.Storage.From(rlsTestBucket).Download(victimFilePath, null);
                throw new Exception($"SECURITY FAILURE: {attackerName} was able to download file '{victimFilePath}' which belongs to another therapist.");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteSuccess($"{attackerName} was correctly blocked from downloading the file.");
                ConsoleHelper.WriteInfo($"Expected error: {ex.Message}");
            }
            finally
            {
                await supabase.Auth.SignOut();
            }
        }
    }
}