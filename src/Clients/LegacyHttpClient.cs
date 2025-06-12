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
    public class LegacyHttpClient : IDisposable, ILegacyClient
    {
        // 🗂️ BUCKET CONFIGURATION
        private readonly string _bucketName;

        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private string _accessToken;
        private bool _isAuthenticated = false;

        // JSON options with custom DateTime handling
        private readonly JsonSerializerOptions _jsonOptions;

        public LegacyHttpClient(string supabaseUrl, string supabaseKey, string bucketName)
        {
            _supabaseUrl = supabaseUrl.TrimEnd('/');
            _supabaseKey = supabaseKey;
            _bucketName = bucketName;

            _httpClient = new HttpClient();
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

                var fileInfo = new FileInfo(localFilePath);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

                // 📁 SAME NAMING PATTERN AS SUPABASE CLIENT: PatientCode_HTTP_timestamp.ext
                var fileExtension = Path.GetExtension(fileInfo.Name);
                var baseFileName = $"{patientCode}_HTTP_{timestamp}{fileExtension}";
                var fileName = $"{patientCode}/{baseFileName}";

                var fileBytes = await File.ReadAllBytesAsync(localFilePath);

                var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{fileName}";

                var response = await _httpClient.PutAsync(uploadUrl, new ByteArrayContent(fileBytes));

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"   ✅ Uploaded to: {_bucketName}/{fileName}");

                    return new FileUploadResult
                    {
                        FileName = fileName,
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

                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await File.WriteAllBytesAsync(localPath, fileBytes);
                    Console.WriteLine($"   ✅ Downloaded: {fullFileName} ({fileBytes.Length} bytes)");
                    return true;
                }
                else
                {
                    Console.WriteLine($"   ❌ Download failed: {response.StatusCode}");
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
        /// List all files OR files for a specific patient
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

                var request = new HttpRequestMessage(HttpMethod.Post, listUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("apikey", _supabaseKey);

                // 📁 PATIENT FILTERING: use prefix
                var prefix = string.IsNullOrEmpty(patientCode) ? "" : $"{patientCode}/";

                var requestBody = new
                {
                    prefix = prefix,
                    limit = 100,
                    offset = 0
                };

                var jsonBody = JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // Use custom JSON options with DateTime converter
                        var files = JsonSerializer.Deserialize<List<StorageFile>>(responseBody, _jsonOptions);

                        if (string.IsNullOrEmpty(patientCode))
                        {
                            // Count actual files vs patient subfolders
                            var actualFiles = files.FindAll(f => f.name.Contains("/"));
                            var patientFolders = files.FindAll(f => !f.name.Contains("/"));

                            if (patientFolders.Count > 0)
                            {
                                Console.WriteLine($"   📂 Found {patientFolders.Count} patient subfolders in bucket '{_bucketName}':");
                                foreach (var folder in patientFolders)
                                {
                                    Console.WriteLine($"      📁 {folder.name}");
                                }
                            }

                            if (actualFiles.Count > 0)
                            {
                                Console.WriteLine($"   📄 Found {actualFiles.Count} files:");
                                foreach (var file in actualFiles)
                                {
                                    Console.WriteLine($"      📄 {file.name} (created: {file.created_at:yyyy-MM-dd HH:mm})");
                                }
                            }

                            if (patientFolders.Count == 0 && actualFiles.Count == 0)
                            {
                                Console.WriteLine($"   📂 Bucket '{_bucketName}' is empty");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"   📂 Found {files.Count} files for patient '{patientCode}':");
                            foreach (var file in files)
                            {
                                Console.WriteLine($"      📄 {file.name} (created: {file.created_at:yyyy-MM-dd HH:mm})");
                            }
                        }

                        // Map to the common ClientFile model
                        return files.Select(f => new ClientFile
                        {
                            Name = f.name,
                            Id = f.id,
                            Size = f.size,
                            CreatedAt = f.created_at
                        }).ToList();
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"   ⚠️ JSON parse error: {ex.Message}");
                        Console.WriteLine($"   📄 Raw response: {responseBody}");
                        return new List<ClientFile>();
                    }
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"   ❌ List error: {response.StatusCode} - {errorBody}");
                    return new List<ClientFile>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ List error: {ex.Message}");
                return new List<ClientFile>();
            }
        }

        /// <summary>
        /// Sign out and clear authentication
        /// </summary>
        public async Task SignOutAsync()
        {
            try
            {
                // Optional: call Supabase logout endpoint
                if (_isAuthenticated && !string.IsNullOrEmpty(_accessToken))
                {
                    var logoutUrl = $"{_supabaseUrl}/auth/v1/logout";
                    await _httpClient.PostAsync(logoutUrl, new StringContent("", Encoding.UTF8, "application/json"));
                }

                // Clear local authentication state
                _accessToken = null;
                _isAuthenticated = false;
                _httpClient.DefaultRequestHeaders.Remove("Authorization");

                Console.WriteLine("   ✅ Signed out successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Sign out error: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}