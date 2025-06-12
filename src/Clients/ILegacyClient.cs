using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GhostlySupaPoc.Models;

namespace GhostlySupaPoc.Clients
{
    /// <summary>
    /// Defines a common contract for legacy POC clients, ensuring they provide
    /// a consistent set of functionalities for testing and comparison.
    /// </summary>
    public interface ILegacyClient : IDisposable
    {
        Task<bool> AuthenticateAsync(string email, string password);
        
        Task<FileUploadResult> UploadFileAsync(string patientCode, string localFilePath);
        
        Task<bool> DownloadFileAsync(string fileName, string localPath, string patientCode = null);
        
        Task<List<ClientFile>> ListFilesAsync(string patientCode = null);
        
        Task SignOutAsync();
        
        Task<bool> TestRLSProtectionAsync(string email, string password);
    }
} 