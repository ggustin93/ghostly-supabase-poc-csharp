# Project Overview

## Name
GHOSTLY+ (Gamified Healthcare Optimization System with Therapist-Level Security)

## Description
GHOSTLY+ is a medical application designed to manage EMG (Electromyography) data for multiple therapists and their patients. The system provides secure storage and access to medical data through Supabase, implementing strict Row-Level Security (RLS) to ensure data privacy and compliance with medical data handling requirements.

The project starts as a Proof of Concept (POC) to evaluate Supabase's capabilities for:
- Authentication and authorization
- Secure file storage with patient-specific organization
- Row-Level Security implementation for multi-therapist access control
- Performance and scalability assessment

## Key Stakeholders
- Therapists: Primary users who manage patient data and EMG sessions
- Patients: Subjects whose medical data is being stored and analyzed
- System Administrators: Manage user accounts and system configuration
- Development Team: Implementing and maintaining the system

## Timeline and Milestones
Phase 1: Initial POC Development (Current)
- ✅ Basic Supabase integration
- ✅ File upload/download functionality
- ✅ Patient folder organization

Phase 2: Multi-Therapist RLS Implementation
- Database schema setup with RLS policies
- Storage access control implementation
- Comprehensive testing suite
- Documentation and security review

## Technology Stack
### Backend
- .NET 7.0
- C# 
- Supabase Platform
  - Authentication
  - Storage
  - Database (PostgreSQL)
  - Row Level Security

### Libraries
- supabase-csharp (v0.16.2)
- System.Net.Http for raw API implementation

### Development Tools
- Visual Studio Code / Visual Studio
- Git for version control
- Supabase Dashboard for configuration

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
   - .NET 7.0 SDK
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