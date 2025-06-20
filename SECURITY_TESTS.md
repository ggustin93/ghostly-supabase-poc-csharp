# GHOSTLY+ Security Validation Report

This document outlines the security controls that have been validated through the automated test suite in this project. It is based directly on the implemented tests in `RlsTests/MultiTherapistRlsTests.cs` and the client-side security checks.

---

## 1. Authorization & Access Control

This is the core of the application's security model. The following tests confirm that the Row-Level Security (RLS) policies are correctly isolating data between different therapists.

### ✅ **Database Data Isolation**
A therapist can only read or write database records that are directly associated with their own `therapist_id`.

- **Test Case**: `Test_CanAccessOwnData`
  - **Description**: Verifies that a therapist can successfully query patients and EMG sessions that belong to them.
  - **Status**: **Validated**

- **Test Case**: `Test_CannotAccessOthersData`
  - **Description**: Verifies that a therapist's query for patients explicitly excludes records belonging to other therapists, returning zero results.
  - **Status**: **Validated**

### ✅ **Storage Access Control**
A therapist can only access files in Supabase Storage that belong to their own assigned patients. The RLS policies on the `storage.objects` table are tested for both `SELECT` (download/list) and `INSERT` (upload) operations.

- **Test Case**: `Test_CanDownloadOwnFiles`
  - **Description**: Confirms a therapist can successfully download a file when the file path corresponds to one of their own patients.
  - **Status**: **Validated**

- **Test Case**: `Test_CannotDownloadOthersFiles`
  - **Description**: Confirms that an attempt by an authenticated therapist to download a file belonging to another therapist's patient will fail.
  - **Status**: **Validated**

### ✅ **End-to-End Workflow Under RLS**
This test validates the entire workflow of creating a new data record and its associated file under active RLS policies.

- **Test Case**: `Test_CanUploadAndProcessC3DFile`
  - **Description**: Ensures a therapist can upload a file to a patient's designated folder and simultaneously create the corresponding metadata record in the `emg_sessions` table. This validates both `INSERT` policies (Storage and Database).
  - **Status**: **Validated**

### ✅ **Protection Against Insecure Direct Object References (IDOR)**
These tests ensure that an attacker cannot bypass RLS policies even if they know or can guess the direct path to a resource.

- **Test Case**: `Test_RLSProtectionOnDirectPathAccess`
  - **Description**: An "attacker" therapist attempts to access a "victim" therapist's resources using a known file path. The test validates that attempts to **download**, **list**, or access via **guessed paths** all fail as expected.
  - **Status**: **Validated**

### ✅ **Role-Based Privilege Restriction**
This test ensures that a user with a `therapist` role cannot perform actions that should be reserved for a higher-privileged role like an administrator.

- **Test Case**: `Test_TherapistRoleRestrictions`
  - **Description**: An authenticated therapist attempts to call RPC functions that would typically be admin-only (e.g., creating another user, altering RLS policies). The test confirms these attempts fail.
  - **Status**: **Validated**

---

## 2. Authentication

These tests are performed by each client to ensure basic session security.

### ✅ **Unauthenticated Access Prevention**
An unauthenticated user (i.e., a user without a valid JWT) must be blocked from accessing any data.

- **Test Case**: `TestRLSProtectionAsync` (in `SupabaseClient` and `CustomHttpClient`)
  - **Description**: Before authenticating, each client attempts to list files from storage. The test validates that this attempt returns zero results, confirming that RLS blocks access for anonymous users.
  - **Status**: **Validated**

---

## 3. Future Security Enhancements (Not Yet Implemented)

The following are essential security measures that should be considered for a production environment but are not yet covered by the automated test suite.

- [ ] **Token Refresh Logic**: Implement and test the token refresh mechanism in the `CustomHttpClient` to handle expired access tokens gracefully.
- [ ] **Input Validation**: Add rigorous validation for all user-provided input to prevent any form of injection attack.
- [ ] **File Upload Security**:
  - [ ] **Type Validation**: Restrict uploads to specific, safe file extensions (e.g., `.c3d`).
  - [ ] **Size Validation**: Enforce a maximum file size to prevent resource exhaustion.
- [ ] **Brute-Force Protection**: Investigate and confirm Supabase's built-in defenses against repeated failed login attempts.
- [ ] **Dependency Vulnerability Scanning**: Integrate a tool like GitHub's Dependabot to automatically scan for vulnerabilities in third-party packages. 