using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Supabase;
using GhostlySupaPoc.Models;
using GhostlySupaPoc.Utils;

namespace GhostlySupaPoc.Clients
{
    /// <summary>
    /// Implements the ISupaClient interface using the official Supabase C# library.
    /// This client handles authentication and file storage operations.
    /// </summary>
    public class SupabaseClient : ISupaClient
    {
        private readonly string _bucketName;
        private readonly Supabase.Client _supabase;
        private bool _isAuthenticated = false;

        /// <summary>
        /// Initializes a new instance of the SupabaseClient.
        /// </summary>
        /// <param name="supabaseUrl">The URL of the Supabase project.</param>
        /// <param name="supabaseKey">The anonymous public key for the Supabase project.</param>
        /// <param name="bucketName">The name of the storage bucket to use for file operations.</param>
        public SupabaseClient(string supabaseUrl, string supabaseKey, string bucketName)
        {
            var options = new SupabaseOptions
            {
                // The official client handles token refreshes automatically in the background.
                // When the access token expires, it will use the refresh token to get a new one.
                AutoRefreshToken = true,
                // We are not using real-time features in this POC, so this can be disabled.
                AutoConnectRealtime = false
            };

            _supabase = new Supabase.Client(supabaseUrl, supabaseKey, options);
            _bucketName = bucketName;
        }

        /// <summary>
        /// Authenticates the user via email and password.
        /// </summary>
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
        /// Gets the list of patients assigned to the currently authenticated therapist.
        /// Leverages Supabase RLS policies which automatically filter results based on auth context.
        /// The database policy ensures therapists can only see their own assigned patients.
        /// </summary>
        public async Task<List<Patient>> GetPatientsAsync()
        {
            if (!_isAuthenticated)
            {
                Console.WriteLine("   ❌ Not authenticated - cannot fetch patients");
                return new List<Patient>();
            }

            try
            {
                // This query automatically applies RLS filtering
                // Only patients where therapist_id = current user's therapist_id are returned
                var response = await _supabase.From<Patient>().Get();
                
                if (response?.Models != null && response.Models.Any())
                {
                    Console.WriteLine($"   ✅ Found {response.Models.Count} assigned patient(s) (RLS filtered)");
                    return response.Models;
                }
                
                Console.WriteLine("   ℹ️ No patients assigned to this therapist");
                return new List<Patient>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error fetching patients: {ex.Message}");
                return new List<Patient>();
            }
        }

        /// <summary>
        /// Uploads a file to a patient-specific subfolder in Supabase Storage.
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

                // Validate C3D file format
                var validationResult = C3DValidator.ValidateC3DFile(localFilePath);
                if (!validationResult.IsValid)
                {
                    Console.WriteLine($"   ❌ File validation failed: {validationResult}");
                    return null;
                }
                
                if (validationResult.Warnings.Any())
                {
                    Console.WriteLine($"   ⚠️ File validation warnings: {string.Join(", ", validationResult.Warnings)}");
                }

                var fileInfo = new FileInfo(localFilePath);
                // Preserve original filename to maintain embedded metadata (timestamps, etc.)
                var originalFileName = fileInfo.Name;
                var filePath = $"{patientCode}/{originalFileName}";

                var fileBytes = await File.ReadAllBytesAsync(localFilePath);

                var result = await _supabase.Storage
                    .From(_bucketName)
                    .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions
                    {
                        ContentType = "application/octet-stream" // Binary format for C3D files
                    });

                if (!string.IsNullOrEmpty(result))
                {
                    return new FileUploadResult
                    {
                        FileName = originalFileName,
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
        /// Downloads a file from a patient's subfolder in Supabase Storage.
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
                string downloadPath = GetDownloadPath(fileName, patientCode);

                var fileBytes = await _supabase.Storage
                    .From(_bucketName)
                    .Download(downloadPath, (Supabase.Storage.TransformOptions)null);

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
        /// Constructs the full download path for a file.
        /// </summary>
        private string GetDownloadPath(string fileName, string patientCode)
        {
            if (!string.IsNullOrEmpty(patientCode))
            {
                return $"{patientCode}/{fileName}";
            }

            var parts = fileName.Split('_');
            if (parts.Length > 1)
            {
                return $"{parts[0]}/{fileName}";
            }

            return fileName;
        }

        /// <summary>
        /// Lists files for a specific patient or all files in the bucket.
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
                var searchPath = string.IsNullOrEmpty(patientCode) ? string.Empty : patientCode;
                var files = await _supabase.Storage.From(_bucketName).List(searchPath);

                return files.Select(f => new ClientFile
                {
                    Name = f.Name,
                    Id = f.Id,
                    Size = null, 
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
        /// Signs the user out from Supabase.
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
        /// Verifies that RLS policies are correctly configured by comparing authenticated and unauthenticated access.
        /// </summary>
        public async Task<bool> TestRLSProtectionAsync(string email, string password)
        {
            try
            {
                Console.WriteLine($"🔒 RLS Test (using bucket: {_bucketName})");

                await _supabase.Auth.SignOut();
                var unauthFiles = await _supabase.Storage.From(_bucketName).List();

                if (!await AuthenticateAsync(email, password))
                {
                    Console.WriteLine("   ❌ Authentication failed during RLS test.");
                    return false;
                }
                var authFiles = await _supabase.Storage.From(_bucketName).List();

                Console.WriteLine($"   Unauthenticated: Saw {unauthFiles.Count} files");
                Console.WriteLine($"   Authenticated:   Saw {authFiles.Count} files");

                bool isRlsWorking = unauthFiles.Count == 0 && authFiles.Count >= 0;
                Console.WriteLine($"   {(isRlsWorking ? "✅ RLS policies are effective." : "⚠️ RLS SECURITY ISSUE: Unauthenticated user can see files!")}");

                return isRlsWorking;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ RLS test error: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            // The Supabase client itself doesn't require disposal in this implementation.
            // This method satisfies the IDisposable interface contract.
        }
    }
} 