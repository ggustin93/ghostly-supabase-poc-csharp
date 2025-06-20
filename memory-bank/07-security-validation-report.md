# Security Validation Report

**Document Purpose:** This document serves as a living reference of all security controls validated by the project's automated test suite. It provides a clear, evidence-based overview of the application's security posture.

This report is based directly on the implemented tests in `RlsTests/MultiTherapistRlsTests.cs` and the client-side security checks. Each validated control links to the specific test case that provides the evidence.

---

## 1. Test Environment Preparation

Before running the main test suite, a setup process validates the baseline capabilities for each test user under RLS.

- **Test Case**: `RlsTestSetup.PrepareTestEnvironment`
  - **Description**: Confirms each therapist can authenticate, find their assigned patient, upload an initial test file to storage, and create a corresponding metadata record in the database. This validates the foundational `INSERT` and `SELECT` permissions required for the test suite.
  - **Status**: **Validated**

---

## 2. Core RLS Policies: Data Isolation

This is the core of the application's security model. The following tests confirm that the Row-Level Security (RLS) policies are correctly isolating data between different therapists.

### ✅ **Database Record Isolation**
A therapist can only read or write database records (e.g., `patients`, `emg_sessions`) that are directly associated with their own `therapist_id`.

- **Test Case**: `Test_CanAccessOwnData`
  - **Description**: Verifies that a therapist can successfully query patients and EMG sessions that belong exclusively to them.
  - **Status**: **Validated**

- **Test Case**: `Test_CannotAccessOthersData`
  - **Description**: Verifies that a therapist's query for records belonging to other therapists correctly returns zero results, preventing horizontal privilege escalation.
  - **Status**: **Validated**

### ✅ **Storage File Isolation**
A therapist can only access files in Supabase Storage that belong to their own assigned patients. The RLS policies on `storage.objects` are tested for `SELECT` (download/list) and `INSERT` (upload) operations.

- **Test Case**: `Test_CanDownloadOwnFiles`
  - **Description**: Confirms a therapist can successfully download a file when the file path corresponds to one of their own patients.
  - **Status**: **Validated**

- **Test Case**: `Test_CannotDownloadOthersFiles`
  - **Description**: Confirms that an attempt by an authenticated therapist to download a file belonging to another therapist's patient will fail, returning an "Object not found" error as if the object does not exist.
  - **Status**: **Validated**

### ✅ **Multi-Patient Data Segregation**
- **Test Case**: `Test_MultiPatientDataSegregation`
  - **Description**: Verifies that a therapist assigned to multiple patients can correctly access data and files for each patient without any data leakage or incorrect associations. This ensures that RLS policies scale correctly across multiple patient relationships for a single therapist.
  - **Status**: **Validated**

---

## 3. End-to-End & Advanced Scenarios

These tests validate complete user workflows and defend against specific attack vectors.

### ✅ **End-to-End Workflow Under RLS**
- **Test Case**: `Test_CanUploadAndProcessC3DFile`
  - **Description**: Ensures a therapist can perform a multi-step operation: upload a file to a patient's designated folder and simultaneously create the corresponding metadata record in the `emg_sessions` table. This validates that both Storage (`INSERT`) and Database (`INSERT`) RLS policies work in concert.
  - **Status**: **Validated**

### ✅ **Protection Against Insecure Direct Object References (IDOR)**
- **Test Case**: `Test_RLSProtectionOnDirectPathAccess`
  - **Description**: An "attacker" therapist attempts to access a "victim" therapist's resources using a known or guessed file path. The test validates that attempts to **download by direct path**, **list folder contents**, or access via **guessed paths** all fail as expected.
  - **Status**: **Validated**

---

## 4. Role-Based Access Control (RBAC)

This test ensures that a user with a `therapist` role cannot perform actions reserved for higher-privileged roles.

- **Test Case**: `Test_TherapistRoleRestrictions`
  - **Description**: An authenticated therapist attempts to call privileged RPC functions (`create_therapist`, `execute_sql`). The test confirms these attempts fail because the functions are not exposed to the `therapist` role.
  - **Status**: **Validated**

---

## 5. Client-Side Authentication Checks

These tests are performed by each client to ensure basic session security.

### ✅ **Unauthenticated Access Prevention**
- **Test Case**: `TestRLSProtectionAsync` (in `SupabaseClient` and `CustomHttpClient`)
  - **Description**: Before authenticating, each client attempts to list files from storage. The test validates that this attempt returns zero results, confirming that RLS correctly blocks access for anonymous users.
  - **Status**: **Validated**

---

## 6. Future Security Enhancements (Not Yet Implemented)

The following are essential security measures that should be considered for a production environment but are not yet covered by the automated test suite.

- [ ] **Token Refresh Logic**: Implement and test the token refresh mechanism in the `CustomHttpClient` to handle expired access tokens gracefully.
- [ ] **Input Validation**: Add rigorous validation for all user-provided input to prevent any form of injection attack.
- [ ] **File Upload Security**:
  - [ ] **Type Validation**: Restrict uploads to specific, safe file extensions (e.g., `.c3d`).
  - [ ] **Size Validation**: Enforce a maximum file size to prevent resource exhaustion.
- [ ] **Brute-Force Protection**: Investigate and confirm Supabase's built-in defenses against repeated failed login attempts.
- [ ] **Dependency Vulnerability Scanning**: Integrate a tool like GitHub's Dependabot to automatically scan for vulnerabilities in third-party packages.

---

This validation report confirms that the core security principles of the application—particularly multi-tenant data isolation—are effectively implemented and tested. It should be updated whenever new security-related tests are added to the suite. 