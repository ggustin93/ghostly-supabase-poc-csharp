using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Supabase;

/// <summary>
/// Result of a successful file upload operation
/// </summary>
public class FileUploadResult
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string PatientCode { get; set; }
}

/// <summary>
/// GHOSTLY+ Proof of Concept demonstrating Supabase C# client capabilities:
/// - Authentication with email/password
/// - File upload to Supabase Storage
/// - File listing and download
/// </summary>
public class GhostlyPOC
{
    private readonly Supabase.Client _supabase;
    private bool _isAuthenticated = false;

    public GhostlyPOC(string supabaseUrl, string supabaseKey)
    {
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _supabase = new Supabase.Client(supabaseUrl, supabaseKey, options);
    }

    /// <summary>
    /// Authenticate user with Supabase Auth
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
                Console.WriteLine($"✅ Authenticated as: {session.User.Email}");
                return true;
            }

            Console.WriteLine("❌ Authentication failed");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Auth error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Upload a file to Supabase Storage with patient-specific naming
    /// </summary>
    public async Task<FileUploadResult> UploadFileAsync(string patientCode, string localFilePath)
    {
        if (!_isAuthenticated)
        {
            Console.WriteLine("❌ Not authenticated");
            return null;
        }

        try
        {
            if (!File.Exists(localFilePath))
            {
                Console.WriteLine($"❌ File not found: {localFilePath}");
                return null;
            }

            var fileInfo = new FileInfo(localFilePath);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{patientCode}_session_{timestamp}_{fileInfo.Name}";

            var fileBytes = await File.ReadAllBytesAsync(localFilePath);

            // Upload to Supabase Storage
            var result = await _supabase.Storage
                .From("c3d-files")
                .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions
                {
                    ContentType = "text/plain"
                });

            if (!string.IsNullOrEmpty(result))
            {
                Console.WriteLine($"✅ Uploaded: {fileName}");

                return new FileUploadResult
                {
                    FileName = fileName,
                    FilePath = result,
                    FileSize = fileInfo.Length,
                    UploadedAt = DateTime.UtcNow,
                    PatientCode = patientCode
                };
            }

            Console.WriteLine("❌ Upload failed");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Upload error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download a file from Supabase Storage
    /// </summary>
    public async Task<bool> DownloadFileAsync(string fileName, string localPath)
    {
        if (!_isAuthenticated)
        {
            Console.WriteLine("❌ Not authenticated");
            return false;
        }

        try
        {
            // FIXED: Use specific overload to avoid ambiguity
            var fileBytes = await _supabase.Storage
                .From("c3d-files")
                .Download(fileName, (Supabase.Storage.TransformOptions)null, null);

            await File.WriteAllBytesAsync(localPath, fileBytes);

            Console.WriteLine($"✅ Downloaded: {fileName} ({fileBytes.Length} bytes)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Download error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// List all files in the C3D storage bucket
    /// </summary>
    public async Task<List<Supabase.Storage.FileObject>> ListFilesAsync()
    {
        if (!_isAuthenticated)
        {
            Console.WriteLine("❌ Not authenticated");
            return new List<Supabase.Storage.FileObject>();
        }

        try
        {
            var files = await _supabase.Storage
                .From("c3d-files")
                .List();

            Console.WriteLine($"📂 Found {files.Count} files:");
            foreach (var file in files)
            {
                Console.WriteLine($"   📄 {file.Name}");
            }

            return files;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ List error: {ex.Message}");
            return new List<Supabase.Storage.FileObject>();
        }
    }

    /// <summary>
    /// Create a sample text file simulating EMG data
    /// </summary>
    public async Task<string> CreateSampleFileAsync(string patientCode)
    {
        try
        {
            var fileName = $"sample_{patientCode}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine("/tmp", fileName);

            var content = $@"GHOSTLY+ Sample EMG Data
Patient: {patientCode}
Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

Sample EMG readings:
Time(s), Biceps(µV), Triceps(µV)
0.00, 45.2, 32.1
0.01, 48.6, 35.4
0.02, 52.1, 29.8
0.03, 49.3, 38.7
0.04, 46.8, 33.2
";

            await File.WriteAllTextAsync(filePath, content);
            return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ File creation error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sign out from Supabase Auth
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            await _supabase.Auth.SignOut();
            _isAuthenticated = false;
            Console.WriteLine("👋 Signed out");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Sign out error: {ex.Message}");
        }
    }
}

/// <summary>
/// Main program demonstrating GHOSTLY+ Supabase integration
/// </summary>
public class Program
{
    // ⚠️ REPLACE WITH YOUR ACTUAL SUPABASE CREDENTIALS
    private static readonly string SUPABASE_URL = "https://your-project.supabase.co";
    private static readonly string SUPABASE_ANON_KEY = "your-anon-key";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("🎮 GHOSTLY+ Supabase C# Client POC");
        Console.WriteLine("===================================\n");

        // Check if credentials are configured
        if (SUPABASE_URL.Contains("your-project"))
        {
            Console.WriteLine("⚠️ SETUP REQUIRED:");
            Console.WriteLine("1. Go to https://supabase.com");
            Console.WriteLine("2. Create new project");
            Console.WriteLine("3. Settings > API > Copy URL and anon key");
            Console.WriteLine("4. Replace SUPABASE_URL and SUPABASE_ANON_KEY in code");
            Console.WriteLine("5. Create 'c3d-files' storage bucket");
            Console.WriteLine("6. Create test user in Authentication");
            return;
        }

        var ghostly = new GhostlyPOC(SUPABASE_URL, SUPABASE_ANON_KEY);

        try
        {
            // 1. Authentication
            Console.WriteLine("🔐 Authentication");
            Console.Write("Email: ");
            var email = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();

            var authSuccess = await ghostly.AuthenticateAsync(email, password);
            if (!authSuccess)
            {
                Console.WriteLine("Create a user in Supabase Authentication first!");
                return;
            }

            Console.WriteLine();

            // 2. Create and upload sample file
            Console.WriteLine("📤 File Upload Test");
            var sampleFile = await ghostly.CreateSampleFileAsync("PATIENT_001");
            if (sampleFile != null)
            {
                await ghostly.UploadFileAsync("PATIENT_001", sampleFile);
            }

            Console.WriteLine();

            // 3. List uploaded files
            Console.WriteLine("📂 List Files");
            var files = await ghostly.ListFilesAsync();

            Console.WriteLine();

            // 4. Download test (if files exist)
            if (files.Count > 0)
            {
                Console.WriteLine("📥 Download Test");
                var firstFile = files[0];
                var downloadPath = Path.Combine("/tmp", $"downloaded_{firstFile.Name}");
                await ghostly.DownloadFileAsync(firstFile.Name, downloadPath);
                Console.WriteLine();
            }

            // 5. Sign out
            await ghostly.SignOutAsync();

            Console.WriteLine();
            Console.WriteLine("✅ POC completed successfully!");
            Console.WriteLine();
            Console.WriteLine("Demonstrated features:");
            Console.WriteLine("• Supabase authentication");
            Console.WriteLine("• File upload to storage");
            Console.WriteLine("• File listing");
            Console.WriteLine("• File download");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}

