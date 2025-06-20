# Supabase Storage RLS Policy Summary

**Document Version: 1.0**  
**Author:** Senior Software Engineer (AI Assistant)  
**Date:** June 20, 2025

---

## 1. Overview

This document provides a definitive summary of the Row-Level Security (RLS) policies implemented for the `storage.objects` table within the Supabase project. These policies form the cornerstone of the application's data security model, ensuring that users can only access files and folders for which they have explicit authorization.

The security architecture is designed around two distinct storage buckets, each serving a unique purpose and protected by its own set of rules. All policies are applied to the `authenticated` user role, which is a best practice to prevent unintentional access by anonymous users.

---

## 2. Policy Details

The following policies are active on the `storage.objects` table.

### 2.1. General Access Bucket (`c3d-files`)

This bucket is configured for general-purpose file operations within the Proof-of-Concept application. It serves as the target for the client comparison tests.

-   **Policy Name**: `Allow authenticated access to c3d-files`
-   **Target Role**: `authenticated`
-   **Permissions**: `ALL` (SELECT, INSERT, UPDATE, DELETE)
-   **Description**: This is a broad policy that grants any successfully authenticated user full permissions to read, write, update, and delete any object within the `c3d-files` bucket. It provides a simple, secure baseline for general authenticated access without exposing the bucket to the public.
-   **Security Implication**: The primary security boundary is authentication. Once a user is logged in, they have full control over this bucket's contents.

#### SQL Definition:
```sql
CREATE POLICY "Allow authenticated access to c3d-files"
ON storage.objects
FOR ALL
TO authenticated
USING (bucket_id = 'c3d-files'::text)
WITH CHECK (bucket_id = 'c3d-files'::text);
```

---

### 2.2. High-Security Multi-Tenant Bucket (`emg_data`)

This bucket is designed for a multi-tenant environment where sensitive patient data is stored. The RLS policy ensures strict data isolation between different therapists.

-   **Policy Name**: `Allow therapist access to assigned patient files`
-   **Target Role**: `authenticated`
-   **Permissions**: `ALL` (SELECT, INSERT, UPDATE, DELETE)
-   **Description**: This policy is significantly more restrictive. It grants access only if two conditions are met:
    1.  The operation targets the `emg_data` bucket.
    2.  The `is_assigned_to_patient()` security function returns `true`. This function checks if the currently authenticated therapist is officially assigned to the patient whose data is being accessed (determined by the folder name, e.g., `P005/`).
-   **Security Implication**: This policy enforces a "need-to-know" basis for data access. A therapist can only interact with files in folders corresponding to patients they manage, effectively preventing any cross-tenant data exposure.

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

## 3. Conclusion

This two-bucket, two-policy RLS strategy provides a robust and flexible security model for the application. It successfully segregates the general-purpose file operations of the POC from the high-security, multi-tenant requirements of the core application logic, all while adhering to the principle of least privilege for authenticated users. 