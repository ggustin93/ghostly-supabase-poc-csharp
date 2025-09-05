# GHOSTLY+ E2E Tests

Simple, pragmatic E2E tests for the C3D upload workflow. No complex frameworks - just verify it works.

## Quick Start

```bash
# Run all E2E tests
./run-e2e-tests.sh

# Dry run (show test plan without executing)
./run-e2e-tests.sh --dry-run
```

## What Gets Tested

1. **Configuration** - Verifies settings are valid
2. **Authentication** - Therapist can log in
3. **File Validation** - C3D file is valid format
4. **Upload Workflow** - Complete upload process works
5. **Storage Verification** - File appears in patient folder

## Test Structure

```
tests/
├── E2E/
│   ├── TherapistUploadTest.cs   # Main test logic
│   └── RunE2ETests.cs            # Test runner
└── README.md                     # This file
```

## Configuration Required

Set these in `appsettings.json` or environment variables:

```json
{
  "SupabaseUrl": "https://your-project.supabase.co",
  "SupabaseKey": "your-anon-key",
  "BucketName": "emg_data",
  "TestTherapistEmail": "therapist1@example.com",
  "TestTherapistPassword": "test-password"
}
```

## Test File Required

Place a valid C3D file at:
```
c3d-test-samples/Ghostly_Emg_20250310_11-50-16-0578.c3d
```

## Exit Codes

- `0` - All tests passed ✅
- `1` - One or more tests failed ❌

## Manual Test Execution

```bash
# Build project
dotnet build

# Run tests directly
dotnet run --project tests/E2E/RunE2ETests.cs

# With options
dotnet run --project tests/E2E/RunE2ETests.cs -- --dry-run
```

## CI/CD Integration

The test script returns standard exit codes for CI/CD pipelines:

```yaml
# GitHub Actions example
- name: Run E2E Tests
  run: ./run-e2e-tests.sh
```

## Principles

- **KISS**: No complex test frameworks, just basic verification
- **DRY**: Reuses existing client code from main application  
- **Pragmatic**: Tests what matters - can a therapist upload files?
- **Fast**: Complete suite runs in ~10-15 seconds

## Sample Output

```
🧪 GHOSTLY+ E2E Test Suite
===========================

TEST 1: Configuration Validation
---------------------------------
✅ Configuration is valid

TEST 2: Therapist Authentication
---------------------------------
✅ Authenticated as: therapist1@example.com

TEST 3: C3D File Validation
----------------------------
✅ Test file found: Ghostly_Emg_20250310_11-50-16-0578.c3d
✅ Valid C3D header detected

TEST 4: Complete Upload Workflow
---------------------------------
✅ Complete workflow successful

TEST 5: Verify File in Patient Folder
--------------------------------------
✅ Found 3 files in P000 folder

=============================
TEST SUMMARY
=============================
✅ Passed: 5
❌ Failed: 0
📊 Total:  5

🎉 All tests passed!
```