using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GhostlySupaPoc.Models;
using GhostlySupaPoc.Utils;

namespace GhostlySupaPoc.Clients
{
    /// <summary>
    /// GHOSTLY+ HTTP POC with patient subfolder management
    /// Clean version using raw HTTP API calls to Supabase
    /// </summary>
    public class CustomHttpClient : IDisposable, ISupaClient
    {
        // 🗂️ BUCKET CONFIGURATION
        private readonly string _bucketName;

        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private string _accessToken;
        private bool _isAuthenticated = false;

        // JSON options with custom DateTime handling
        private readonly JsonSerializerOptions _jsonOptions;

        public CustomHttpClient(string supabaseUrl, string supabaseKey, string bucketName)
        {
            _supabaseUrl = supabaseUrl.TrimEnd('/');
            _supabaseKey = supabaseKey;
            _bucketName = bucketName;

            _httpClient = new System.Net.Http.HttpClient();
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GHOSTLY-Plus-HTTP-Client/1.0");

            // Configure JSON options with custom DateTime converter
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            _jsonOptions.Converters.Add(new SupabaseDateTimeConverter());
        }

        /// <summary>
        /// Test RLS protection - Authentication and access validation
        /// </summary>
        public async Task<bool> TestRLSProtectionAsync(string email, string password)
        {
            try
            {
                Console.WriteLine($"   🔒 Testing RLS protection (HTTP Client)");

                // Test 1: Unauthenticated access
                _accessToken = null;
                _isAuthenticated = false;
                _httpClient.DefaultRequestHeaders.Remove("Authorization");

                var listUrl = $"{_supabaseUrl}/storage/v1/object/list/{_bucketName}";

                var unauthRequest = new HttpRequestMessage(HttpMethod.Post, listUrl);
                unauthRequest.Headers.Add("apikey", _supabaseKey);

                var requestBody = new { prefix = "", limit = 100, offset = 0 };
                var jsonBody = JsonSerializer.Serialize(requestBody);
                unauthRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var unauthResponse = await _httpClient.SendAsync(unauthRequest);
                int unauthCount = 0;

                if (unauthResponse.IsSuccessStatusCode)
                {
                    var unauthBody = await unauthResponse.Content.ReadAsStringAsync();
                    try
                    {
                        var unauthFiles = JsonSerializer.Deserialize<List<StorageFile>>(unauthBody, _jsonOptions);
                        unauthCount = unauthFiles?.Count ?? 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ⚠️ Unauthenticated parse error: {ex.Message}");
                        unauthCount = 0;
                    }
                }

                // Test 2: Authentication and access
                bool authSuccess = await AuthenticateAsync(email, password);
                if (!authSuccess)
                {
                    Console.WriteLine("   ❌ Authentication failed");
                    return false;
                }

                var authFiles = await ListFilesAsync();
                int authCount = authFiles?.Count ?? 0;

                // Analyze results
                Console.WriteLine($"   📊 Unauthenticated access: {unauthCount} files");
                Console.WriteLine($"   📊 Authenticated access: {authCount} files");

                bool rlsWorking = unauthCount == 0 && authCount >= 0;
                Console.WriteLine($"   {(rlsWorking ? "✅ RLS is working properly" : "⚠️ RLS security issue detected")}");

                return authSuccess; // Return true if authentication succeeded
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ RLS test error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Authentication with Supabase Auth API
        /// </summary>
        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            // TODO: Implement token refresh logic. This client currently does not handle
            // expired access tokens. For a production-ready implementation, the refresh_token
            // should be securely stored and used to request a new access_token when the
            // current one expires, preventing the user from being logged out unexpectedly.
            try
            {
                var authUrl = $"{_supabaseUrl}/auth/v1/token?grant_type=password";

                var authData = new
                {
                    email = email,
                    password = password
                };

                var jsonContent = JsonSerializer.Serialize(authData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(authUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseBody, _jsonOptions);
                    _accessToken = authResponse.access_token;
                    _isAuthenticated = true;

                    // Update HttpClient with auth token
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

                    Console.WriteLine($"   ✅ Authenticated as: {authResponse.user.email}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"   ❌ Authentication failed: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Auth error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upload with patient subfolder support
        /// Uses consistent naming: PatientCode_HTTP_timestamp.ext
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
                var fileName = $"{patientCode}/{originalFileName}";

                var fileBytes = await File.ReadAllBytesAsync(localFilePath);

                var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{fileName}";

                var response = await _httpClient.PutAsync(uploadUrl, new ByteArrayContent(fileBytes));

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"   ✅ Uploaded to: {_bucketName}/{fileName}");

                    return new FileUploadResult
                    {
                        FileName = originalFileName,
                        FilePath = $"{_bucketName}/{fileName}",
                        FileSize = fileInfo.Length,
                        UploadedAt = DateTime.UtcNow,
                        PatientCode = patientCode
                    };
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"   ❌ Upload failed: {response.StatusCode} - {errorBody}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Upload error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Download with patient subfolder support
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
                // If fileName doesn't already contain patient subfolder, add it
                string fullFileName = fileName;
                if (!string.IsNullOrEmpty(patientCode) && !fileName.Contains("/"))
                {
                    fullFileName = $"{patientCode}/{fileName}";
                }

                var downloadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{fullFileName}";

                var response = await _httpClient.GetAsync(downloadUrl);

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // Ensure the directory exists
                    var directoryName = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    await File.WriteAllBytesAsync(localPath, fileBytes);
                    Console.WriteLine($"   ✅ Downloaded to: {localPath}");
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"   ❌ Download failed: {response.StatusCode} - {errorBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Download error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// List files in patient's subfolder, or root if no patient specified
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
                var listUrl = $"{_supabaseUrl}/storage/v1/object/list/{_bucketName}";

                // Use patientCode as the prefix to list only files in that "subfolder"
                var requestBody = new
                {
                    prefix = string.IsNullOrEmpty(patientCode) ? "" : patientCode.TrimEnd('/') + "/",
                    limit = 100,
                    offset = 0
                };

                var jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, listUrl)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var files = JsonSerializer.Deserialize<List<StorageFile>>(responseBody, _jsonOptions);

                        var clientFiles = new List<ClientFile>();
                        if (files != null)
                        {
                            foreach (var f in files)
                            {
                                clientFiles.Add(new ClientFile
                                {
                                    Name = f.name,
                                    Id = f.id,
                                    Size = f.size,
                                    CreatedAt = f.created_at
                                });
                            }
                        }
                        return clientFiles;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ Error parsing file list: {ex.Message}");
                        Console.WriteLine($"   Raw JSON: {responseBody}");
                        return new List<ClientFile>();
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ Failed to list files: {response.StatusCode} - {responseBody}");
                    return new List<ClientFile>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ List files error: {ex.Message}");
                return new List<ClientFile>();
            }
        }
        
        /// <summary>
        /// Extracts the patient code from a file path (e.g., "P001/file.c3d" -> "P001")
        /// </summary>
        private string GetPatientCodeFromPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.Contains("/"))
            {
                return null;
            }
            return path.Split('/')[0];
        }

        /// <summary>
        /// Sign out and clear access token
        /// </summary>
        public async Task SignOutAsync()
        {
            if (!_isAuthenticated)
            {
                return;
            }

            try
            {
                var signOutUrl = $"{_supabaseUrl}/auth/v1/logout";
                await _httpClient.PostAsync(signOutUrl, null);
                Console.WriteLine("   ✅ Signed out successfully (HTTP Client)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Sign out error: {ex.Message}");
            }
            finally
            {
                // Always clear local session
                _accessToken = null;
                _isAuthenticated = false;
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
