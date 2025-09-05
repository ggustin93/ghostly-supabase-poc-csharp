# Supabase Storage RLS Policy Summary

**Document Version: 2.0**  
**Date:** September 5, 2025

---

## 1. Overview

This document outlines the Row-Level Security (RLS) policies for the `storage.objects` table. These policies secure file access by ensuring users can only interact with objects for which they have explicit authorization.

The current architecture uses the `emg_data` storage bucket with multi-tenant RLS policies applied to the `authenticated` user role. This prevents anonymous access and enforces therapist-specific data isolation.

---

## 2. Policy Details

The following policies are active on the `storage.objects` table.

### 2.1. Multi-Tenant Data Storage (`emg_data`)

This bucket stores sensitive patient data and uses a multi-tenant RLS policy to ensure strict data isolation between therapists.

-   **Policy Name**: `Allow therapist access to assigned patient files`
-   **Target Role**: `authenticated`
-   **Permissions**: `ALL` (SELECT, INSERT, UPDATE, DELETE)
-   **Description**: Grants access only if the operation targets the `emg_data` bucket and the `is_assigned_to_patient()` function returns `true`. This function verifies that the authenticated therapist is assigned to the patient whose data is being accessed.
-   **Security Implication**: Enforces a "need-to-know" access model. A therapist can only interact with files corresponding to their assigned patients, preventing cross-tenant data exposure.

#### SQL Definition:
```sql
CREATE POLICY "Allow therapist access to assigned patient files"
ON storage.objects
FOR ALL
TO authenticated
USING (
    (bucket_id = 'emg_data'::text) AND 
    is_assigned_to_patient((storage.foldername(name))[1])
)
WITH CHECK (
    (bucket_id = 'emg_data'::text) AND 
    is_assigned_to_patient((storage.foldername(name))[1])
);
```

---

## 3. Configuration

### Environment Variables
The bucket configuration is managed through environment variables:
- `BUCKET_NAME` or `BucketName` (appsettings.json): Specifies the target storage bucket (currently "emg_data")
- Configuration priority: Environment variables > appsettings.json > defaults

### Testing
The RLS policies are validated through:
- Multi-therapist isolation tests in `src/RlsTests/MultiTherapistRlsTests.cs`
- End-to-end workflow tests in `tests/E2E/TherapistUploadTest.cs`
- Client-side security validation in both `SupabaseClient` and `CustomHttpClient`

---

## 4. Conclusion

The RLS strategy provides a robust multi-tenant security model that ensures strict data isolation between therapists while maintaining operational flexibility through configurable bucket management. 