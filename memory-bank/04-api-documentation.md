# API & Client Documentation

This document provides an overview of the public-facing API exposed by the C# clients through the `ISupaClient` interface. Any class implementing this interface provides a consistent contract for interacting with the Supabase backend.

---

## `ISupaClient` Interface
**File**: `src/Clients/ISupaClient.cs`

This interface defines the core functionalities required for authentication and storage operations in this POC.

### Authentication Methods

---

#### `Task<bool> AuthenticateAsync(string email, string password)`
-   **Purpose**: Authenticates the user with the provided credentials.
-   **Parameters**:
    -   `email`: The user's email address.
    -   `password`: The user's password.
-   **Returns**: `true` if authentication is successful and a session is established; otherwise, `false`.

---

#### `Task SignOutAsync()`
-   **Purpose**: Signs the current user out and clears the session.
-   **Returns**: A `Task` that completes when the sign-out process is finished.

---

### Storage Methods

---

#### `Task<FileUploadResult> UploadFileAsync(string patientCode, string localFilePath)`
-   **Purpose**: Uploads a file to a patient-specific folder in Supabase Storage.
-   **Parameters**:
    -   `patientCode`: The unique identifier for the patient, which will be used as the folder name.
    -   `localFilePath`: The local path to the file to upload.
-   **Returns**: A `FileUploadResult` object containing metadata about the successful upload; `null` if the upload fails.

---

#### `Task<List<ClientFile>> ListFilesAsync(string patientCode = null)`
-   **Purpose**: Lists all files within a specific patient's folder.
-   **Parameters**:
    -   `patientCode`: The patient's code, which corresponds to the folder to be listed.
-   **Returns**: A `List<ClientFile>` containing metadata for each file in the folder. Returns an empty list if the folder is empty or not found.

---

#### `Task<bool> DownloadFileAsync(string fileName, string localPath, string patientCode = null)`
-   **Purpose**: Downloads a file from a patient's folder in Supabase Storage.
-   **Parameters**:
    -   `fileName`: The name of the file to download (e.g., `P001_Test.txt`).
    -   `localPath`: The full local path (including filename) where the file will be saved.
    -   `patientCode`: The patient's code, used to locate the file in its subfolder.
-   **Returns**: `true` if the download is successful; otherwise, `false`.

---

### Security Methods

---

#### `Task<bool> TestRLSProtectionAsync(string email, string password)`
-   **Purpose**: A diagnostic method to verify that RLS policies are effective.
-   **Description**: This method first attempts to list files in an unauthenticated state (expecting 0 results), then authenticates and lists files again (expecting >0 results). It's a self-contained check to ensure RLS blocks anonymous access but permits authenticated access.
-   **Returns**: `true` if the RLS policies behave as expected; otherwise, `false`.

## Authentication API

### Sign In
**Endpoint**: `POST /auth/v1/token?grant_type=password`
**Implementation**: 
```csharp
public async Task<bool> AuthenticateAsync(string email, string password)
```

**Request Body**:
```json
{
    "email": "string",
    "password": "string"
}
```

**Response**:
```json
{
    "access_token": "string",
    "token_type": "bearer",
    "expires_in": 3600,
    "refresh_token": "string",
    "user": {
        "id": "uuid",
        "email": "string"
    }
}
```

### Sign Out
**Endpoint**: `POST /auth/v1/logout`
**Implementation**:
```csharp
public async Task SignOutAsync()
```

## Storage API

### Upload File
**Endpoint**: `PUT /storage/v1/object/{bucket}/{path}`
**Implementation**:
```csharp
public async Task<FileUploadResult> UploadFileAsync(string patientCode, string localFilePath)
```

**Parameters**:
- `bucket`: "emg_data" (configured via environment variables)
- `path`: "{patientCode}/{originalFilename}" (preserves original filename with embedded metadata)

**Response**:
```json
{
    "Key": "string",
    "ETag": "string",
    "Location": "string",
    "Bucket": "string"
}
```

### Download File
**Endpoint**: `GET /storage/v1/object/{bucket}/{path}`
**Implementation**:
```csharp
public async Task<bool> DownloadFileAsync(string fileName, string localPath, string patientCode = null)
```

**Parameters**:
- `bucket`: "emg_data" (configured via environment variables)
- `path`: "{patientCode}/{originalFilename}" (maintains original filename structure)

**Response**: Binary file content

### List Files
**Endpoint**: `POST /storage/v1/object/list/{bucket}`
**Implementation**:
```csharp
public async Task<List<ClientFile>> ListFilesAsync(string patientCode = null)
```

**Request Body**:
```json
{
    "prefix": "string",
    "limit": 100,
    "offset": 0
}
```

**Response**:
```json
[
    {
        "name": "string",
        "id": "string",
        "size": 0,
        "content_type": "string",
        "created_at": "string",
        "updated_at": "string"
    }
]
```

## Database API

This section documents the database interactions that are validated by the RLS test suite. The C# code uses the `postgrest-csharp` library (included in `supabase-csharp`) to perform these operations.

### Therapist Operations

#### Get Therapist Profile
**Endpoint**: `GET /rest/v1/therapists?user_id=eq.{auth.uid()}`
**Implementation**: Implicitly tested in `RlsTestSetup.cs` and used by RLS policies.
```csharp
// Example from tests:
var therapist = (await supabase.From<Therapist>().Get()).Models.FirstOrDefault();
```

**Response**:
```json
{
    "id": "uuid",
    "user_id": "uuid",
    "first_name": "string",
    "last_name": "string"
}
```

### Patient Operations

#### List Therapist's Patients
**Endpoint**: `GET /rest/v1/patients` (RLS policy applies `therapist_id=eq.{id}`)
**Implementation**: Actively used in `MultiTherapistRlsTests.cs`.
```csharp
// Example from tests:
var patientResponse = await supabase.From<Patient>().Get();
```

**Response**:
```json
[
    {
        "id": "uuid",
        "therapist_id": "uuid",
        "patient_code": "string",
        "first_name": "string",
        "last_name": "string",
        "date_of_birth": "date"
    }
]
```

### EMG Session Operations

#### List Patient Sessions
**Endpoint**: `GET /rest/v1/emg_sessions` (RLS policy applies)
**Implementation**: Actively used in `MultiTherapistRlsTests.cs`.
```csharp
// Example from tests:
var sessionResponse = await supabase.From<EmgSession>().Get();
```

**Response**:
```json
[
    {
        "id": "uuid",
        "patient_id": "uuid",
        "file_path": "string",
        "recorded_at": "timestamp",
        "notes": "string"
    }
]
```

#### Create EMG Session
**Endpoint**: `POST /rest/v1/emg_sessions`
**Implementation**: Actively used in `MultiTherapistRlsTests.cs`.
```csharp
// Example from tests:
var response = await supabase.From<EmgSession>().Insert(emgSession);
```

## RLS Policies

The security of the application is enforced through a combination of database and storage-level Row-Level Security (RLS) policies. These policies ensure that therapists can only access data belonging to their assigned patients.

### Database RLS

The database policies leverage a helper function, `public.get_current_therapist_id()`, which securely retrieves the `therapist_id` of the currently authenticated user.

#### Therapists Table
```sql
CREATE POLICY "Allow therapists to manage their own profile"
ON public.therapists FOR ALL
USING (user_id = auth.uid())
WITH CHECK (user_id = auth.uid());
```
- **Rule**: A therapist can only view or modify their own record.

#### Patients Table
```sql
CREATE POLICY "Allow therapists to manage their assigned patients"
ON public.patients FOR ALL
USING (therapist_id = public.get_current_therapist_id())
WITH CHECK (therapist_id = public.get_current_therapist_id());
```
- **Rule**: A therapist can perform any action on patient records only if they are the assigned therapist.

#### EMG Sessions Table
```sql
CREATE POLICY "Allow therapists to manage EMG sessions for assigned patients"
ON public.emg_sessions FOR ALL
USING (
    patient_id IN (
        SELECT id FROM public.patients
        WHERE therapist_id = public.get_current_therapist_id()
    )
)
WITH CHECK (
    patient_id IN (
        SELECT id FROM public.patients
        WHERE therapist_id = public.get_current_therapist_id()
    )
);
```
- **Rule**: A therapist can manage session records only if the session belongs to one of their assigned patients.

### Storage RLS

Storage policies rely on the `public.is_assigned_to_patient(patient_code)` helper function to verify access rights based on the folder structure. The file path is expected to be in the format `{patient_code}/{filename}`.

#### File Access Policies (`emg_data` bucket)
```sql
-- This policy covers SELECT (download/list) operations.
CREATE POLICY "Allow therapists to access files for assigned patients"
ON storage.objects FOR SELECT
USING (
    bucket_id = 'emg_data' AND
    public.is_assigned_to_patient((storage.foldername(name))[1])
);

-- This policy covers INSERT operations.
CREATE POLICY "Allow therapists to upload files for assigned patients"
ON storage.objects FOR INSERT
WITH CHECK (
    bucket_id = 'emg_data' AND
    public.is_assigned_to_patient((storage.foldername(name))[1])
);
```
- **Rule**: Therapists can only perform actions (view, upload, update, delete) on files within a folder that matches the `patient_code` of an assigned patient. Additional policies for `UPDATE` and `DELETE` follow the same pattern.

## Error Handling

### HTTP Status Codes
- 200: Success
- 401: Unauthorized
- 403: Forbidden (RLS violation)
- 404: Not found
- 500: Server error

### Error Response Format
```json
{
    "error": "string",
    "message": "string",
    "statusCode": 0,
    "details": {}
}
```

### Common Error Scenarios

#### Authentication Errors
```json
{
    "error": "invalid_credentials",
    "message": "Invalid email or password",
    "statusCode": 401
}
```

#### RLS Violations
```json
{
    "error": "access_denied",
    "message": "RLS policy violation",
    "statusCode": 403
}
```

#### Storage Errors
```json
{
    "error": "storage_error",
    "message": "File not found or access denied",
    "statusCode": 404
}
```

## API Usage Examples

### Authentication Flow
```csharp
// 1. Initialize client
var ghostly = new GhostlyPOC(supabaseUrl, supabaseKey);

// 2. Authenticate
var success = await ghostly.AuthenticateAsync(email, password);

// 3. Perform operations
if (success)
{
    var files = await ghostly.ListFilesAsync();
}
```

### File Operations Flow
```csharp
// 1. Upload file
var uploadResult = await ghostly.UploadFileAsync(patientCode, localFilePath);

// 2. List files
var files = await ghostly.ListFilesAsync(patientCode);

// 3. Download file
var downloadSuccess = await ghostly.DownloadFileAsync(
    uploadResult.FileName,
    "./downloads/file.txt",
    patientCode
);
```

## Security Considerations

### Authentication
- Always use HTTPS
- Token refresh handling
- Secure credential storage

### RLS
- Policy testing required
- Cross-access prevention
- Audit logging

### File Operations
- Content-type validation
- File size limits
- Path traversal prevention

## Performance Guidelines

### Optimization Tips
1. Use appropriate limits for list operations
2. Implement pagination where needed
3. Cache authentication tokens
4. Handle file uploads in chunks

### Rate Limits
- Authentication: 60 requests per minute
- Storage operations: 100 requests per minute
- Database queries: 1000 requests per minute 