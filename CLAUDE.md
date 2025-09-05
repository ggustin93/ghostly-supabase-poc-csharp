# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 7 console application that demonstrates secure multi-tenant architecture using Supabase with Row-Level Security (RLS). The project validates two C# Supabase client implementations and includes comprehensive RLS security testing.

## Development Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the interactive console application
dotnet run

# Clean build artifacts
dotnet clean
```

### Database Management (Supabase Cloud)
```bash
# Link to Supabase Cloud project (one-time setup)
supabase link --project-ref <your-project-ref>

# Apply all local migrations to cloud database (resets remote database)
supabase db reset

# Push migrations to cloud without resetting
supabase db push

# Pull remote schema changes
supabase db pull
```

Note: This project uses Supabase Cloud instance, not local Supabase.

## Architecture Overview

### Core Domain Model
The application implements a medical data management system with strict multi-tenant isolation:

1. **Authentication Layer** (`auth.users`) - Supabase authentication for therapist users
2. **Data Layer** - PostgreSQL with RLS policies:
   - `therapists` - Links auth users to application users (1:1 with auth.users)
   - `patients` - Patient records owned by therapists (N:1 with therapists)
   - `emg_sessions` - EMG session data for patients (N:1 with patients)
3. **Storage Layer** - Supabase Storage with RLS for file segregation

### Client Architecture
The project compares two Supabase client implementations:

- **SupabaseClient** (`src/Clients/SupabaseClient.cs`) - Wrapper around official `supabase-csharp` library
- **CustomHttpClient** (`src/Clients/CustomHttpClient.cs`) - Direct HTTP API implementation

Both implement `ISupaClient` interface for consistent testing.

### Security Model
Multi-tenant isolation is enforced at multiple levels:

1. **Database RLS Policies** - Therapists can only access their own patients' data
2. **Storage RLS Policies** - File access restricted by patient ownership
3. **Application-Level Validation** - SecurityFailureException thrown on policy violations

## Key Technical Patterns

### Environment Configuration
- Uses `DotEnv.Load()` for local development
- Configuration centralized in `TestConfig` class
- Required environment variables:
  - `SUPABASE_URL`
  - `SUPABASE_ANON_KEY`
  - Storage buckets: `c3d-files` (legacy), `emg_data` (RLS testing)

### Testing Architecture
Two main test suites accessible via interactive menu:

1. **Client Comparison Suite** - Validates both client implementations
2. **Multi-Therapist RLS Suite** - Comprehensive security validation

Test setup and teardown managed by `RlsTestSetup` class.

### Storage Organization
Files organized hierarchically: `{bucket}/{patient_code}/{filename}`

Patient codes follow format: `PAT001`, `PAT002`, etc.

## Important Implementation Notes

### RLS Policy Implementation
When modifying RLS policies:
1. Update SQL in `supabase/migrations/`
2. Test with both therapist accounts
3. Validate cross-therapist access is blocked
4. Check both database and storage policies

### Client Selection
- Use `SupabaseClient` for standard operations (better error handling)
- Use `CustomHttpClient` for debugging API interactions
- Both clients must pass the same test suite

### Error Handling
- `SecurityFailureException` indicates RLS policy violations
- All database operations should handle RLS policy errors gracefully
- Storage operations return appropriate error codes for access violations

## Project Structure Reference

```
src/
├── Clients/          # Supabase client implementations
├── Config/           # Environment and test configuration
├── Models/           # Data models and DTOs
├── RlsTests/         # RLS security test suite
├── Utils/            # Helper utilities
└── main.cs           # Entry point with interactive menu

supabase/migrations/  # Database schema and RLS policies
memory-bank/          # Detailed technical documentation
```