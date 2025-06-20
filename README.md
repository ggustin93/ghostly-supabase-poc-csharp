# GHOSTLY+ Supabase C# Proof of Concept

This repository contains a .NET 7 console application designed to test and validate the use of [Supabase](https://supabase.com/) for building secure, multi-tenant applications. It provides a practical foundation for understanding Supabase's core features, particularly its powerful Row-Level Security (RLS) system.

The project is structured around two primary goals:
1.  **Client Implementation Comparison**: To analyze and compare the official `supabase-csharp` client against a raw C# `HttpClient` implementation for authentication and storage operations.
2.  **RLS Policy Validation**: To provide a comprehensive test suite that validates a multi-therapist security model, ensuring that data access is strictly segregated between tenants.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- A [Supabase](https://supabase.com/) account and a project.
- [Supabase CLI](https://supabase.com/docs/guides/cli) (for managing database migrations).

### Configuration
1.  **Clone & Configure**: Clone the repository and create a `.env` file in the root directory. Use `.env.example` as a template for your Supabase URL, anon key, and test user credentials.

2.  **Database Setup**: Link your Supabase project and apply the database migrations using the Supabase CLI. This will create the necessary tables, RLS policies, and seed data.
    ```bash
    # Link your remote Supabase project (only needs to be done once)
    supabase link --project-ref <your-project-ref>

    # Apply all local migrations to a fresh database
    supabase db reset
    ```

### Running the Application
Build and run the project from your terminal. The interactive menu will guide you through the available test suites.
```bash
dotnet run
```

---

## 📂 Project Structure

The repository is organized to separate concerns, making it easy to navigate and understand the different components of the POC.

```
.
├── src/
│   ├── Clients/      # Contains the two client implementations for Supabase.
│   │   ├── SupabaseClient.cs   # Wrapper for the official `supabase-csharp` library.
│   │   └── CustomHttpClient.cs # A raw HTTP client for direct API interaction.
│   ├── Config/       # Manages environment variables and test configuration.
│   ├── Models/       # C# data models (POCOs) for database tables.
│   ├── RlsTests/     # The multi-therapist RLS test suite.
│   ├── Utils/        # Shared utility classes for helpers and exceptions.
│   └── main.cs       # The application entry point and interactive menu.
│
├── supabase/
│   └── migrations/   # SQL scripts for database schema, RLS policies, and seed data.
│
├── memory-bank/      # Contains detailed, long-term project documentation.
│
├── .env.example      # Example environment file.
├── README.md         # This file.
└── RLS_POLICY_SUMMARY.md # A detailed summary of the active RLS policies.
```

---

## 🛡️ Core Test Scenarios

The application's main menu provides access to two key testing scenarios:

1.  **Client Comparison Mode**: This mode runs a sequence of tests (Authentication, RLS check, Upload, List, Download, Sign Out) for both the official and the raw HTTP clients against a general-purpose storage bucket (`c3d-files`). This is useful for comparing behavior and performance.

2.  **Multi-Therapist RLS Test Suite**: This is the primary security validation suite. It uses a dedicated, high-security bucket (`emg_data`) to run a series of tests that confirm a `therapist` user can *only* access data and files belonging to their explicitly assigned patients. It validates the core multi-tenant security model of the application. 