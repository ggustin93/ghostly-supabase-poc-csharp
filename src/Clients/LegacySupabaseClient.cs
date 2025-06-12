using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Supabase;
using GhostlySupaPoc.Models;

/// <summary>
/// GHOSTLY+ Proof of Concept demonstrating Supabase C# client capabilities:
/// - Authentication with email/password
/// - File upload to Supabase Storage with patient subfolders
/// - File listing and download
/// Uses Repl.it Secrets for secure credential management
/// </summary>
namespace GhostlySupaPoc.Clients
{
    /// <summary>
    /// Legacy Proof of Concept using the official Supabase C# client library
    /// This class retains the original POC logic for comparison purposes.
    /// </summary>
    public class LegacySupabaseClient : ILegacyClient
    {
        // 🗂️ BUCKET CONFIGURATION - Change this to test different buckets
        private readonly string _bucketName;

        private readonly Supabase.Client _supabase;
        private bool _isAuthenticated = false;

        public LegacySupabaseClient(string supabaseUrl, string supabaseKey, string bucketName)
        {
            var options = new SupabaseOptions
            {
                // Automatically refresh JWT tokens when they expire (recommended for long-running applications)
                AutoRefreshToken = true,
                // Disable realtime connections since we're only doing file operations (saves resources)
                AutoConnectRealtime = false
            };

            _supabase = new Supabase.Client(supabaseUrl, supabaseKey, options);
            _bucketName = bucketName;
        }

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            try
            {
                await _supabase.InitializeAsync();
                var session = await _supabase.Auth.SignIn(email, password);

                if (session?.User != null)
                {
                    _isAuthenticated = true;
                    Console.WriteLine($"   ✅ Authenticated as: {session.User.Email}");
                    return true;
                }

                Console.WriteLine("   ❌ Authentication failed - invalid credentials");
                return false;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("invalid_credentials") || ex.Message.Contains("Invalid login"))
                {
                    Console.WriteLine("   ❌ Invalid email or password");
                }
                else
                {
                    Console.WriteLine($"   ❌ Auth error: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Upload a file to Supabase Storage with patient-specific subfolder
        /// </summary>
        public async Task<FileUploadResult> UploadFileAsync(string patientCode, string localFilePath)
        {
            if (!_isAuthenticated)
            {
                Console.WriteLine("   ❌ Not authenticated");
                return null;
            }

            try
            {
                if (!File.Exists(localFilePath))
                {
                    Console.WriteLine($"   ❌ File not found: {localFilePath}");
                    return null;
                }

                var fileInfo = new FileInfo(localFilePath);

                // Create filename: P001_SUPABASE_20250611_143022.txt
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"{patientCode}_SUPABASE_{timestamp}.txt";
                var filePath = $"{patientCode}/{fileName}"; // Direct patient folder (no "patients" parent)

                var fileBytes = await File.ReadAllBytesAsync(localFilePath);

                // Upload to configured bucket with patient subfolder
                var result = await _supabase.Storage
                    .From(_bucketName)
                    .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions
                    {
                        ContentType = "text/plain"
                    });

                if (!string.IsNullOrEmpty(result))
                {
                    Console.WriteLine($"   ✅ Uploaded: {filePath} (to bucket: {_bucketName})");

                    return new FileUploadResult
                    {
                        FileName = fileName,
                        FilePath = result,
                        FileSize = fileInfo.Length,
                        UploadedAt = DateTime.UtcNow,
                        PatientCode = patientCode
                    };
                }

                Console.WriteLine("   ❌ Upload failed");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Upload error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Download a file from patient's subfolder in Supabase Storage
        /// </summary>
        public async Task<bool> DownloadFileAsync(string fileName, string localPath, string patientCode = null)
        {
            if (!_isAuthenticated)
            {
                Console.WriteLine("   ❌ Not authenticated");
                return false;
            }

            try
            {
                // If patientCode is provided, use subfolder structure
                string downloadPath;
                if (!string.IsNullOrEmpty(patientCode))
                {
                    downloadPath = $"{patientCode}/{fileName}"; // Direct patient folder
                }
                else
                {
                    // Try to extract patient code from filename (e.g., P001_SUPABASE_20250611_143022.txt)
                    var parts = fileName.Split('_');
                    if (parts.Length >= 1)
                    {
                        var extractedPatientCode = parts[0]; // P001 (first part before underscore)
                        downloadPath = $"{extractedPatientCode}/{fileName}";
                    }
                    else
                    {
                        downloadPath = fileName; // Fallback to root
                    }
                }

                var fileBytes = await _supabase.Storage
                    .From(_bucketName)
                    .Download(downloadPath, (Supabase.Storage.TransformOptions)null, null);

                // Ensure the directory exists before writing the file
                var directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(localPath, fileBytes);

                Console.WriteLine($"   ✅ Downloaded: {downloadPath} ({fileBytes.Length} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Download error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// List files for a specific patient or all patients
        /// </summary>
        public async Task<List<ClientFile>> ListFilesAsync(string patientCode = null)
        {
            if (!_isAuthenticated)
            {
                Console.WriteLine("   ❌ Not authenticated");
                return new List<ClientFile>();
            }

            try
            {
                List<Supabase.Storage.FileObject> files;

                if (!string.IsNullOrEmpty(patientCode))
                {
                    // List files for specific patient
                    var patientFolder = patientCode; // Direct patient folder
                    files = await _supabase.Storage
                        .From(_bucketName)
                        .List(patientFolder);

                    Console.WriteLine($"   📂 Found {files.Count} files for patient '{patientCode}' in bucket '{_bucketName}':");
                }
                else
                {
                    // List all files and organize by patient folders
                    files = await _supabase.Storage
                        .From(_bucketName)
                        .List();

                    Console.WriteLine($"   📂 Found {files.Count} total files/folders in bucket '{_bucketName}':");

                    // Group and display by patient folders
                    await DisplayOrganizedFilesAsync(files);
                }

                foreach (var file in files)
                {
                    Console.WriteLine($"      📄 {file.Name}");
                }

                // Map to the common ClientFile model
                return files.Select(f => new ClientFile
                {
                    Name = f.Name,
                    Id = f.Id,
                    Size = f.Size,
                    CreatedAt = f.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ List error: {ex.Message}");
                return new List<ClientFile>();
            }
        }

        /// <summary>
        /// Sign out from Supabase Auth and clear authentication state
        /// </summary>
        public async Task SignOutAsync()
        {
            try
            {
                await _supabase.Auth.SignOut();
                _isAuthenticated = false;
                Console.WriteLine("   ✅ Signed out");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Sign out error: {ex.Message}");
            }
        }

        /// <summary>
        /// Comprehensive RLS test comparing authenticated vs unauthenticated access
        /// Validates that Row Level Security policies properly restrict file access
        /// </summary>
        public async Task<bool> TestRLSProtectionAsync(string email, string password)
        {
            try
            {
                Console.WriteLine($"🔒 RLS Test (using bucket: {_bucketName})");

                // Test unauthenticated access - should see 0 files due to RLS policy
                await _supabase.Auth.SignOut();
                var unauthFiles = await _supabase.Storage.From(_bucketName).List();

                // Test authenticated access - should see actual files if any exist
                if (!await AuthenticateAsync(email, password))
                {
                    Console.WriteLine("   ❌ Authentication failed");
                    return false; // Early return on auth failure
                }
                var authFiles = await _supabase.Storage.From(_bucketName).List();

                // Analyze results - RLS working if unauthenticated sees 0, authenticated sees ≥0
                Console.WriteLine($"   Unauthenticated: {unauthFiles.Count} files");
                Console.WriteLine($"   Authenticated: {authFiles.Count} files");

                bool rlsWorking = unauthFiles.Count == 0 && authFiles.Count >= 0;
                Console.WriteLine($"   {(rlsWorking ? "✅ RLS Working" : "⚠️ RLS Security Issue")}");

                return rlsWorking;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ RLS test error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Display files organized by patient folders (recursive-like view)
        /// </summary>
        private async Task DisplayOrganizedFilesAsync(List<Supabase.Storage.FileObject> allFiles)
        {
            try
            {
                var patientFolders = new List<string>();
                var rootFiles = new List<string>();

                // Separate patient folders from root files
                foreach (var file in allFiles)
                {
                    if (file.Name.Contains("/"))
                    {
                        // This is likely a folder or file in subfolder
                        var parts = file.Name.Split('/');
                        var folderName = parts[0];
                        if (!patientFolders.Contains(folderName))
                        {
                            patientFolders.Add(folderName);
                        }
                    }
                    else
                    {
                        // Root level file
                        rootFiles.Add(file.Name);
                    }
                }

                // Display root files first
                if (rootFiles.Count > 0)
                {
                    Console.WriteLine("   📁 Root files:");
                    foreach (var rootFile in rootFiles)
                    {
                        Console.WriteLine($"      📄 {rootFile}");
                    }
                }

                // Display each patient folder with its contents
                foreach (var patientFolder in patientFolders.OrderBy(x => x))
                {
                    Console.WriteLine($"   📁 {patientFolder}/");

                    try
                    {
                        var patientFiles = await _supabase.Storage
                            .From(_bucketName)
                            .List(patientFolder);

                        foreach (var patientFile in patientFiles.OrderBy(x => x.Name))
                        {
                            Console.WriteLine($"      📄 {patientFile.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"      ⚠️ Could not list files in {patientFolder}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Error organizing file display: {ex.Message}");
                // Fallback to simple listing
                foreach (var file in allFiles)
                {
                    Console.WriteLine($"      📄 {file.Name}");
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose for the Supabase client in this context.
            // Implemented to satisfy the ILegacyClient interface.
        }
    }
}