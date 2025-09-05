# GHOSTLY+ Project Overview

## 1. Project Description
GHOSTLY+ is a .NET 9.0 LTS application that demonstrates secure, multi-tenant medical data management using Supabase. The core of the project is a console application that tests and demonstrates Supabase's capabilities for:
-   **Authentication**: Secure sign-in for multiple therapist users.
-   **Segregated Data Storage**: Using Supabase Storage and PostgreSQL to ensure patient data is strictly isolated between therapists.
-   **Row-Level Security (RLS)**: Implementing database and storage policies to enforce the multi-tenant security model.
-   **Client Implementation**: Comparing the official `supabase-csharp` client against a raw `HttpClient` implementation.

## 2. Key Objectives & Scope
The primary goal is to produce a well-documented, secure, and testable foundation for a multi-tenant medical application.

-   **In Scope**:
    -   Implementing a robust, test-driven RLS security model.
    -   Comparing two different C# client implementations.
    -   Creating comprehensive documentation for architecture, security, and setup.

-   **Current Status**:
    -   Multi-tenant RLS security implementation and validation
    -   C3D file processing capability (biomechanical data)
    -   Environment configuration (Repl.it + appsettings.json)
    -   E2E test suite implementation (5 test scenarios)
    -   Framework updated to .NET 9.0 LTS

## 3. Technology Stack
-   **Backend Platform**: Supabase (Authentication, PostgreSQL Database, Storage)
-   **Language & Framework**: C# on .NET 9.0 LTS (upgraded from 7.0)
-   **Key Libraries**:
    -   `supabase-csharp`: The official community client for Supabase.
    -   `dotenv.net`: For managing environment variables in development.

## 4. Key Stakeholders
-   **Primary Users**: Therapists managing patient data.
-   **System Administrators**: Responsible for user management and system configuration.
-   **Development Team**: Responsible for implementing and maintaining the system.

## Timeline and Milestones
**Phase 1: Initial POC Development**
- Basic Supabase integration
- File upload/download functionality  
- Patient folder organization
- Framework upgrade to .NET 9.0 LTS
- Configuration system simplification

**Phase 2: Multi-Therapist RLS Implementation**
- Database schema setup with RLS policies
- Storage access control implementation
- Testing suite development (5 test scenarios)
- Documentation and security review
- C3D file processing implementation
- Environment validation

## Repository Structure
The project has been refactored into a clean, feature-oriented structure.

```
.
├── src/
│   ├── Clients/      # Legacy client implementations
│   ├── Config/       # Environment configuration
│   ├── Models/       # C# data models (POCOs)
│   ├── RlsTests/     # The multi-therapist RLS test suite
│   ├── Utils/        # Shared utility classes
│   └── main.cs       # Application entry point
├── supabase/
│   └── migrations/   # SQL migration scripts
├── .gitignore
├── ghostly-supabase-poc-csharp.sln
└── memory-bank/      # Project documentation
```

## Getting Started
1. Prerequisites:
   - .NET 9.0 SDK (or compatible runtime)
   - Supabase account and project  
   - Git

2. Environment Setup:
   ```bash
   # Clone the repository
   git clone [repository-url]
   cd ghostly-supabase-poc-csharp

   # Set environment variables
   export SUPABASE_URL="your-project-url"
   export SUPABASE_ANON_KEY="your-anon-key"
   ```

3. Supabase Configuration:
   - Create a new Supabase project
   - Create 'c3d-files' bucket in Storage
   - Enable Row Level Security
   - Create test user accounts

4. Build and Run:
   ```bash
   dotnet build
   dotnet run
   ``` 