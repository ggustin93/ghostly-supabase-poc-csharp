# Supabase Storage RLS Policy Summary

**Document Version: 1.0**  
**Date:** June 20, 2025

---

## 1. Overview

This document outlines the Row-Level Security (RLS) policies for the `storage.objects` table. These policies secure file access by ensuring users can only interact with objects for which they have explicit authorization.

The architecture uses two storage buckets, `c3d-files` and `emg_data`, each with distinct policies applied to the `authenticated` user role. This prevents anonymous access and enforces role-based permissions.

---

## 2. Policy Details

The following policies are active on the `storage.objects` table.

### 2.1. General Access Bucket (`c3d-files`)

This bucket supports general-purpose file operations and is the target for client comparison tests.

-   **Policy Name**: `Allow authenticated access to c3d-files`
-   **Target Role**: `authenticated`
-   **Permissions**: `ALL` (SELECT, INSERT, UPDATE, DELETE)
-   **Description**: Grants any authenticated user full permissions to objects within the `c3d-files` bucket. Access is restricted to authenticated users only.

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

## 3. Conclusion

The RLS strategy segregates general-purpose file storage from high-security, multi-tenant data, providing a robust and flexible security model that adheres to the principle of least privilege. 