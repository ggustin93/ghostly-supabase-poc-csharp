using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GhostlySupaPoc.Utils
{
    /// <summary>
    /// Validates C3D (Coordinate 3D) files used in biomechanics and motion capture.
    /// C3D files have a specific binary format with header and parameter sections.
    /// </summary>
    public static class C3DValidator
    {
        private const int MIN_FILE_SIZE = 512; // C3D files have at least 512-byte header
        private const int MAX_FILE_SIZE = 50 * 1024 * 1024; // 50MB max for this application
        
        /// <summary>
        /// Validates a C3D file stream for format compliance and size limits.
        /// </summary>
        /// <param name="fileStream">The file stream to validate</param>
        /// <param name="fileName">The name of the file being validated</param>
        /// <returns>A ValidationResult containing validation status and any errors/warnings</returns>
        public static ValidationResult ValidateC3DFile(Stream fileStream, string fileName)
        {
            var result = new ValidationResult();
            
            // Check extension
            if (!fileName.EndsWith(".c3d", StringComparison.OrdinalIgnoreCase))
            {
                result.AddError($"Invalid file extension. Expected .c3d, got {Path.GetExtension(fileName)}");
                return result;
            }
            
            // Check size
            if (fileStream.Length < MIN_FILE_SIZE)
            {
                result.AddError($"File too small. C3D files must be at least {MIN_FILE_SIZE} bytes, got {fileStream.Length} bytes");
                return result;
            }
            
            if (fileStream.Length > MAX_FILE_SIZE)
            {
                result.AddError($"File too large. Maximum size is {MAX_FILE_SIZE / (1024 * 1024)}MB, got {fileStream.Length / (1024 * 1024)}MB");
                return result;
            }
            
            // Validate C3D header structure
            try
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                byte[] header = new byte[4];
                int bytesRead = fileStream.Read(header, 0, 4);
                
                if (bytesRead < 4)
                {
                    result.AddError("Could not read file header - file may be corrupted");
                    return result;
                }
                
                // C3D files typically have a parameter section indicator at byte 1
                // The value 0x50 (80 decimal) is common for the parameter section
                if (header[1] != 0x50)
                {
                    // Some C3D files might have different values, so this is a warning not an error
                    result.AddWarning($"Unexpected header byte at position 1: 0x{header[1]:X2}. File may not be a valid C3D file.");
                    
                    // Additional check: Parameter section usually starts at block 2 (byte 512)
                    if (fileStream.Length >= 512)
                    {
                        fileStream.Seek(512, SeekOrigin.Begin);
                        byte paramIndicator = (byte)fileStream.ReadByte();
                        
                        // Parameter blocks typically have values between 1-100
                        if (paramIndicator < 1 || paramIndicator > 100)
                        {
                            result.AddError($"Invalid parameter section indicator at byte 512: {paramIndicator}. This does not appear to be a valid C3D file.");
                            return result;
                        }
                    }
                }
                
                // Reset stream position for subsequent operations
                fileStream.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to validate C3D structure: {ex.Message}");
                return result;
            }
            
            result.IsValid = !result.Errors.Any();
            return result;
        }
        
        /// <summary>
        /// Validates a C3D file from a file path.
        /// </summary>
        public static ValidationResult ValidateC3DFile(string filePath)
        {
            try
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    return ValidateC3DFile(fileStream, Path.GetFileName(filePath));
                }
            }
            catch (Exception ex)
            {
                var result = new ValidationResult();
                result.AddError($"Could not open file for validation: {ex.Message}");
                return result;
            }
        }
    }
    
    /// <summary>
    /// Represents the result of a file validation operation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        
        public string ErrorMessage => Errors.Any() ? string.Join("; ", Errors) : string.Empty;
        
        public void AddError(string message)
        {
            Errors.Add(message);
            IsValid = false;
        }
        
        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }
        
        public override string ToString()
        {
            if (IsValid)
            {
                return Warnings.Any() 
                    ? $"Valid with warnings: {string.Join("; ", Warnings)}" 
                    : "Valid";
            }
            
            return $"Invalid: {string.Join("; ", Errors)}";
        }
    }
}