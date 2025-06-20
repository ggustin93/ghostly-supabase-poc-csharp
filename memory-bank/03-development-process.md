# Development Process

This document outlines the standard development workflow, branching strategy, and commit guidelines for the GHOSTLY+ project.

---

## 1. Development Workflow
The development process follows a standard feature-branch workflow.

1.  **Create an Issue**: Before starting work, ensure there is a corresponding issue that defines the feature or bug.
2.  **Create a Branch**: Create a new branch from `main` using the naming convention below (e.g., `feature/add-new-test-case`).
3.  **Implement Changes**: Write the code, ensuring it adheres to the project's coding standards and architectural patterns.
4.  **Write or Update Tests**: All new features or bug fixes must be covered by corresponding tests. For security-related changes, update the RLS test suite.
5.  **Update Documentation**: If the changes affect the system architecture, component interactions, or security model, update the relevant documents in the `/memory-bank`.
6.  **Submit a Pull Request**: Push the branch and open a pull request, linking it to the original issue.

---

## 2. Branching & Commit Strategy

### Branching
-   **`main`**: The primary branch containing stable, production-ready code.
-   **`feature/*`**: For developing new features (e.g., `feature/add-reporting`).
-   **`bugfix/*`**: For fixing bugs on released code (e.g., `bugfix/fix-auth-error`).
-   **`refactor/*`**: For non-functional changes to improve code quality.

### Commit Guidelines
This project uses the [Conventional Commits](https://www.conventionalcommits.org/) specification. This creates a clear and explicit commit history.

-   **Format**: `<type>(<scope>): <subject>`
-   **Types**: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`.
-   **Scope**: The part of the codebase affected (e.g., `auth`, `rls`, `docs`).

**Example Commit Message:**
```
feat(storage): add file versioning policy

Implements a new RLS policy to handle file versioning in the `emg_data` bucket. The policy ensures that therapists can only access the latest version of a file unless explicitly requested.

- Adds a `version` column to the `storage.objects` metadata.
- Updates the RLS security function to check for the version number.
```

---

## 3. Testing Process

### Local Development & Testing
All development and testing can be done locally using the .NET CLI.

1.  **Build the project**:
    ```bash
    dotnet build
    ```
2.  **Run the application**:
    ```bash
    dotnet run
    ```
    The interactive menu provides access to all test suites, including the critical **Multi-Therapist RLS Test Suite**.

### Supabase Environment
All migrations for setting up the database schema and RLS policies are located in the `/supabase/migrations` directory. To reset the remote database to a clean state for testing, use the Supabase CLI:
```bash
supabase db reset
```

---

## 4. Code Review & Documentation

### Code Review
All pull requests require at least one approval. The review should focus on:
-   **Correctness**: Does the code solve the problem?
-   **Security**: Does it introduce any vulnerabilities? Are RLS policies respected?
-   **Test Coverage**: Is the new code adequately tested?
-   **Clarity**: Is the code and its documentation easy to understand?

### Documentation
Documentation is a critical part of this project. Any changes that alter the system's behavior or structure **must** be reflected in the `/memory-bank` documentation. 