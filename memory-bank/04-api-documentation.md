# API Documentation

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
- `bucket`: "c3d-files"
- `path`: "{patientCode}/{filename}"

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
- `bucket`: "c3d-files"
- `path`: "{patientCode}/{filename}"

**Response**: Binary file content

### List Files
**Endpoint**: `POST /storage/v1/object/list/{bucket}`
**Implementation**:
```csharp
public async Task<List<StorageFile>> ListFilesAsync(string patientCode = null)
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

## Database API (Phase 2)

### Therapist Operations

#### Get Therapist Profile
**Endpoint**: `GET /rest/v1/therapists?email=eq.{email}`
**Implementation**: (Planned)
```csharp
public async Task<Therapist> GetTherapistProfileAsync()
```

**Response**:
```json
{
    "id": "uuid",
    "email": "string",
    "name": "string",
    "hospital": "string",
    "created_at": "timestamp"
}
```

### Patient Operations

#### List Therapist's Patients
**Endpoint**: `GET /rest/v1/patients?therapist_id=eq.{id}`
**Implementation**: (Planned)
```csharp
public async Task<List<Patient>> GetMyPatientsAsync()
```

**Response**:
```json
[
    {
        "id": "uuid",
        "patient_code": "string",
        "therapist_id": "uuid",
        "name": "string",
        "age": 0,
        "hospital": "string",
        "created_at": "timestamp"
    }
]
```

#### Get Patient Details
**Endpoint**: `GET /rest/v1/patients?patient_code=eq.{code}`
**Implementation**: (Planned)
```csharp
public async Task<Patient> GetPatientDetailsAsync(string patientCode)
```

### EMG Session Operations

#### List Patient Sessions
**Endpoint**: `GET /rest/v1/emg_sessions?patient_id=eq.{id}`
**Implementation**: (Planned)
```csharp
public async Task<List<EMGSession>> GetPatientSessionsAsync(string patientCode)
```

**Response**:
```json
[
    {
        "id": "uuid",
        "patient_id": "uuid",
        "therapist_id": "uuid",
        "file_path": "string",
        "session_date": "timestamp",
        "game_level": 0,
        "score": 0,
        "duration_minutes": 0
    }
]
```

## RLS Policies

### Database RLS

#### Therapist Profile Access
```sql
CREATE POLICY "therapist_own_profile" ON therapists
    FOR ALL USING (auth.email() = email);
```

#### Patient Data Access
```sql
CREATE POLICY "therapist_own_patients" ON patients
    FOR ALL USING (
        therapist_id = (
            SELECT id FROM therapists WHERE email = auth.email()
        )
    );
```

#### Session Data Access
```sql
CREATE POLICY "therapist_patient_sessions" ON emg_sessions
    FOR ALL USING (
        therapist_id = (
            SELECT id FROM therapists WHERE email = auth.email()
        )
        AND patient_id IN (
            SELECT id FROM patients 
            WHERE therapist_id = (
                SELECT id FROM therapists WHERE email = auth.email()
            )
        )
    );
```

### Storage RLS

#### File Access Policy
```sql
((storage.foldername(name))[1] IN (
    SELECT p.patient_code 
    FROM patients p
    JOIN therapists t ON t.id = p.therapist_id
    WHERE t.email = auth.email()
))
```

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