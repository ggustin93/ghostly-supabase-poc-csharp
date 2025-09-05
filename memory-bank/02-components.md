# Components Documentation

This document provides an overview of the key components in the `src/` directory, explaining their purpose and relationships.

```mermaid
graph TD
    subgraph "Application Core"
        Main["main.cs"]
    end

    subgraph "Client Implementations"
        ISupaClient["ISupaClient.cs"]
        SupabaseClient["Clients/SupabaseClient.cs"]
        CustomHttpClient["Clients/CustomHttpClient.cs"]
    end

    subgraph "Security & Testing"
        RlsTests["RlsTests/"]
    end

    subgraph "Supporting Code"
        Config["Config/"]
        Configuration["Configuration/"]
        Models["Models/"]
        Utils["Utils/"]
    end
    
    subgraph "Examples & Testing"
        MobileExample["MobileUploadExample.cs"]
        E2ETests["tests/E2E/"]
    end

    Main --> ISupaClient
    Main --> RlsTests
    Main --> Config
    ISupaClient <|-- SupabaseClient
    ISupaClient <|-- CustomHttpClient
    SupabaseClient --> Models
    CustomHttpClient --> Models
    RlsTests --> Models
```

---

## 1. Application Core (`main.cs`)
-   **Purpose**: Acts as the application's entry point and central orchestrator.
-   **Responsibilities**:
    -   Presents the interactive user menu.
    -   Initializes the client configurations based on user selection.
    -   Calls the appropriate test suites (`RunTestSequence` or `RunRlsPoc`).

## 2. Client Implementations (`src/Clients/`)
This directory contains the interchangeable strategies for communicating with Supabase.

-   **`ISupaClient.cs`**: Defines the common interface that both client implementations adhere to. This ensures they can be used interchangeably by the rest of the application.
-   **`SupabaseClient.cs`**: The implementation that uses the official `supabase-csharp` library. It wraps the library's methods to conform to the `ISupaClient` interface.
-   **`CustomHttpClient.cs`**: The implementation that uses the raw .NET `HttpClient`. This client manually constructs HTTP requests, manages authorization headers, and parses JSON responses.

## 3. RLS Test Suite (`src/RlsTests/`)
-   **Purpose**: Contains the comprehensive test suite for validating the multi-tenant Row-Level Security model.
-   **Responsibilities**:
    -   **`MultiTherapistRlsTests.cs`**: The core test logic. It runs a series of tests to confirm that therapists can only access data belonging to their assigned patients and are blocked from all unauthorized access.
    -   **`RlsTestSetup.cs`**: A helper class that prepares the test environment by creating necessary data (patients, files, etc.) for each therapist before the tests run.

## 4. Supporting Components

-   **`src/Config/`**: Contains `TestConfig.cs`, a simplified wrapper around `SimpleConfig` for test-specific configuration.
-   **`src/Configuration/`**: Contains `SimpleConfig.cs`, the centralized configuration system that loads from environment variables with `appsettings.json` fallback.
-   **`src/Models/`**: Contains C# Plain Old CLR Objects (POCOs) that map to database tables and represent API responses (e.g., `Patient.cs`, `EmgSession.cs`, `TherapySession.cs`, `UserProfile.cs`).
-   **`src/Utils/`**: Contains shared utilities including C3D file validation (`C3DValidator.cs`), console helpers, and common operations.

## 5. Examples & Testing

-   **`src/MobileUploadExample.cs`**: Demonstrates mobile-style file upload workflow with real-world C3D file processing.
-   **`tests/E2E/TherapistUploadTest.cs`**: Comprehensive end-to-end test suite covering configuration validation, authentication, file operations, and workflow validation.
-   **`c3d-test-samples/`**: Real biomechanical C3D files (1.1MB) used for testing file upload and processing workflows.
