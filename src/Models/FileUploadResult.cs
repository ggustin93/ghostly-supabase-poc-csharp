using System;

namespace GhostlySupaPoc.Models
{
    /// <summary>
    /// Represents the result of a successful file upload operation.
    /// </summary>
    public class FileUploadResult
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public string PatientCode { get; set; }
    }
} 