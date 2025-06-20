using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GhostlySupaPoc.Models;

namespace GhostlySupaPoc.Clients
{
    /// <summary>
    /// Defines a common contract for clients interacting with Supabase, ensuring they provide
    /// a consistent set of functionalities for authentication and storage operations.
    /// </summary>
    public interface ISupaClient : IDisposable
    {
        /// <summary>
        /// Authenticates the user with the provided credentials.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>A task that represents the asynchronous operation, returning true if authentication is successful.</returns>
        Task<bool> AuthenticateAsync(string email, string password);
        
        /// <summary>
        /// Uploads a file to a patient-specific folder in Supabase Storage.
        /// </summary>
        /// <param name="patientCode">The unique identifier for the patient.</param>
        /// <param name="localFilePath">The local path to the file to upload.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of the file upload.</returns>
        Task<FileUploadResult> UploadFileAsync(string patientCode, string localFilePath);
        
        /// <summary>
        /// Downloads a file from Supabase Storage.
        /// </summary>
        /// <param name="fileName">The name of the file to download.</param>
        /// <param name="localPath">The local path where the file will be saved.</param>
        /// <param name="patientCode">The patient's code, used to locate the file in its subfolder. Optional.</param>
        /// <returns>A task that represents the asynchronous operation, returning true if the download is successful.</returns>
        Task<bool> DownloadFileAsync(string fileName, string localPath, string patientCode = null);
        
        /// <summary>
        /// Lists files within a specific patient's folder or all files if no patient code is provided.
        /// </summary>
        /// <param name="patientCode">The patient's code to filter the file list. Optional.</param>
        /// <returns>A task that represents the asynchronous operation, containing a list of file metadata.</returns>
        Task<List<ClientFile>> ListFilesAsync(string patientCode = null);
        
        /// <summary>
        /// Signs the current user out.
        /// </summary>
        /// <returns>A task that represents the asynchronous sign-out operation.</returns>
        Task SignOutAsync();
        
        /// <summary>
        /// Tests the effectiveness of Row-Level Security policies by attempting access with and without authentication.
        /// </summary>
        /// <param name="email">The email of the user to test with.</param>
        /// <param name="password">The password of the user to test with.</param>
        /// <returns>A task that represents the asynchronous operation, returning true if RLS policies are working as expected.</returns>
        Task<bool> TestRLSProtectionAsync(string email, string password);
    }
} 