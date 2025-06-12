# GHOSTLY+ Supabase C# POC

GHOSTLY+ (Gamified Healthcare Optimization System with Therapist-Level Security) is a .NET 7 Proof of Concept (POC) demonstrating the capabilities of Supabase for building secure, multi-tenant medical applications. The primary focus is on implementing robust Row-Level Security (RLS) to ensure that therapists can only access data belonging to their own assigned patients.

This repository serves as a technical showcase for comparing different client implementations and validating complex security models with Supabase.

## ✨ Key Features

- **Multi-Therapist Authentication**: Secure sign-in for multiple therapist users.
- **Segregated Patient Data**: Strict data isolation enforced at the database level using RLS.
- **Secure File Storage**: Patient files are stored in a private bucket with access controlled by storage RLS policies.
- **Client Implementation Comparison**: Includes a legacy Supabase C# client and a raw HTTP client for performance and capability analysis.
- **Automated RLS Test Suite**: A comprehensive set of tests to validate all security policies and prevent data leakage.

## 🚀 Technology Stack

- **Backend**: [Supabase](https://supabase.com/) (Authentication, Database, Storage)
- **Database**: PostgreSQL with Row-Level Security
- **Language**: C# on .NET 7
- **Primary Libraries**:
  - `supabase-csharp` for client interaction
  - `dotenv.net` for environment configuration

## ⚙️ Getting Started

### Prerequisites

- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- A [Supabase](https://supabase.com/) account and a new project.
- [Supabase CLI](https://supabase.com/docs/guides/cli) (for managing database migrations).

### 1. Configuration

1.  **Clone the repository:**
    ```bash
    git clone <repository-url>
    cd ghostly-supabase-poc-csharp
    ```

2.  **Set up your Supabase project:**
    - In your Supabase project dashboard, go to `Project Settings` > `API`.
    - Find your `Project URL` and `anon` (public) key.

3.  **Create a `.env` file:**
    - In the root of the repository, create a file named `.env`.
    - Add your Supabase credentials and test user details to this file. Use `.env.example` as a template:
      ```env
      SUPABASE_URL="https://your-project-url.supabase.co"
      SUPABASE_ANON_KEY="your-public-anon-key"

      # Credentials for the RLS test suite
      THERAPIST_1_EMAIL="therapist1@example.com"
      THERAPIST_1_PASSWORD="your-secure-password"
      THERAPIST_2_EMAIL="therapist2@example.com"
      THERAPIST_2_PASSWORD="your-secure-password"
      ```

### 2. Database Migrations

Once the Supabase CLI is installed and you have logged in (`supabase login`), link your project and apply the database migrations. This will set up the required tables, RLS policies, and seed data.

```bash
# Link your remote Supabase project (only needs to be done once)
supabase link --project-ref <your-project-ref>

# Reset the remote database and apply all local migrations
supabase db reset
```

### 3. Running the Application

After configuration, you can build and run the application from your terminal:

```bash
dotnet run
```

This will launch the interactive console, where you can choose which tests to run.

## 🛡️ Running the Tests

The application provides a menu to run different test scenarios:

- **1 & 2. Legacy Clients**: Test the individual client implementations.
- **3. Comparison Mode**: Run both clients back-to-back to compare their performance and behavior.
- **4. Cleanup**: Remove locally generated test files.
- **5. Multi-Therapist RLS Test**: **This is the main security test.** It runs a comprehensive suite to validate all RLS policies, ensuring therapists can only access their own data and are blocked from unauthorized access.

## 📂 Repository Structure

The project is organized into a clean, feature-oriented structure:

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
│   └── migrations/   # SQL migration scripts for database setup
├── .env.example      # Example environment file
├── .gitignore
├── ghostly-supabase-poc-csharp.sln
└── memory-bank/      # In-depth project documentation
``` 