# Filename Preservation Documentation

**Document Purpose**: Details the critical business requirement for preserving original filenames during upload operations to maintain embedded metadata essential for data extraction.

---

## 1. Business Requirement

Original filenames for C3D biomechanical files must be preserved during upload operations to maintain embedded metadata that is essential for data processing and analysis.

### Example Filename Structure
```
Ghostly_Emg_20250310_11-50-16-0578.c3d
│       │   │                    │
│       │   │                    └── Session identifier
│       │   └── Timestamp: March 10, 2025, 11:50:16
│       └── Data type: EMG (electromyography)
└── System/organization identifier
```

---

## 2. Implementation

### Before (Generated Filenames)
```csharp
// Previous implementation generated new filenames
var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
var fileName = $"{patientCode}_SUPABASE_{timestamp}.c3d";
// Result: "P000_SUPABASE_20250905_110855.c3d"
```

### After (Preserved Filenames)
```csharp
// Current implementation preserves original filenames
var originalFileName = fileInfo.Name;
var filePath = $"{patientCode}/{originalFileName}";
// Result: "P000/Ghostly_Emg_20250310_11-50-16-0578.c3d"
```

---

## 3. Modified Components

### Client Implementations
- **`src/Clients/SupabaseClient.cs`**: Updated upload logic to preserve original filenames
- **`src/Clients/CustomHttpClient.cs`**: Updated upload logic to preserve original filenames

### Test Data
- **`c3d-test-samples/Ghostly_Emg_20250310_11-50-16-0578.c3d`**: Real 1.1MB C3D file with embedded timestamp metadata

---

## 4. Metadata Extraction Requirements

### Embedded Information
- **Timestamp**: Date and time of recording session
- **Data Type**: EMG, motion capture, force plate data
- **Session ID**: Unique identifier for the recording session
- **System ID**: Originating system or organization

### Processing Dependencies
Data extraction algorithms rely on filename patterns to:
- Parse recording timestamps for chronological analysis
- Identify data types for appropriate processing pipelines
- Correlate sessions across multiple data files
- Track data provenance and quality metrics

---

## 5. Validation

### Test Coverage
- **E2E Tests**: Verify filename preservation in `tests/E2E/TherapistUploadTest.cs`
- **Client Tests**: Both `SupabaseClient` and `CustomHttpClient` implementations tested
- **Real Data**: Testing performed with actual 1.1MB C3D biomechanical data files

### Success Criteria
- Original filename maintained in storage path structure
- Embedded metadata accessible for extraction
- No loss of timestamp or session information
- Compatible with existing data processing workflows