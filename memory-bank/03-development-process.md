# Development Process

## Development Workflow

### 1. Development Phases

#### Phase 1: Initial POC (Current)
1. Basic Setup
   - ✅ Project initialization
   - ✅ Supabase integration
   - ✅ Environment configuration

2. Core Features
   - ✅ Authentication implementation
   - ✅ File storage operations
   - ✅ Basic RLS testing

3. Client Comparison
   - ✅ Supabase SDK implementation
   - ✅ HTTP client implementation
   - ✅ Performance comparison

#### Phase 2: Multi-Therapist RLS (✅ Completed)
1. Database Setup
   - ✅ Create tables (therapists, patients, sessions)
   - ✅ Enable RLS on all tables
   - ✅ Implement RLS policies

2. Storage Enhancement
   - ✅ Implement storage RLS policies
   - ✅ Update folder structure
   - ✅ Add access controls

3. Testing & Documentation
   - ✅ Comprehensive RLS testing
   - ✅ Performance analysis
   - ✅ Security documentation

## Branching Strategy

### Main Branches
- `main`: Production-ready code
- `develop`: Development integration
- `feature/*`: Individual features
- `bugfix/*`: Bug fixes
- `release/*`: Release preparation

### Branch Naming Convention
```
feature/add-rls-policies
feature/multi-therapist-support
bugfix/auth-token-refresh
release/v1.0.0
```

## Commit Guidelines

### Commit Message Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Formatting
- `refactor`: Code restructuring
- `test`: Testing
- `chore`: Maintenance

### Example
```
feat(auth): implement multi-therapist authentication

- Add therapist role validation
- Implement session management
- Update RLS policies

Closes #123
```

## Development Environment Setup

### Prerequisites
1. Development Tools
   ```bash
   # Install .NET 7.0 SDK
   brew install dotnet@7.0
   
   # Install Git
   brew install git
   ```

2. Supabase Setup
   ```bash
   # Install Supabase CLI
   brew install supabase/tap/supabase
   
   # Login to Supabase
   supabase login
   ```

### Configuration
1. Environment Variables
   ```bash
   # Development
   export SUPABASE_URL="https://your-project.supabase.co"
   export SUPABASE_ANON_KEY="your-anon-key"
   
   # Testing
   export SUPABASE_TEST_EMAIL="test@example.com"
   export SUPABASE_TEST_PASSWORD="your-test-password"
   ```

2. IDE Setup (VS Code)
   ```json
   {
     "omnisharp.enableRoslynAnalyzers": true,
     "omnisharp.enableEditorConfigSupport": true
   }
   ```

## Testing Process

### 1. Local Testing
```bash
# Build project
dotnet build

# Run tests
dotnet run
```

### 2. RLS Testing
The project includes a comprehensive, automated RLS test suite (`MultiTherapistRlsTests.cs`) that is run by selecting option `5` from the main menu.

The suite validates:
1.  **Data Isolation**: Therapists can only access their own data.
2.  **Storage Policies**: Therapists are blocked from accessing files of other therapists' patients.
3.  **End-to-End Workflows**: Secure file upload, metadata creation, and download.
4.  **Role Security**: Therapists cannot perform admin-level actions.

### 3. Performance Testing
- File upload/download speeds
- Authentication response times
- RLS policy impact

## Deployment Process

### 1. Database Migration
```sql
-- Apply schema changes
BEGIN;
-- Run migration scripts
COMMIT;
```

### 2. Storage Setup
1. Create buckets
2. Configure RLS policies
3. Verify access patterns

### 3. Application Deployment
```bash
# Build release
dotnet publish -c Release

# Deploy artifacts
dotnet run --project <path-to-project>
```

## Code Review Process

### Review Checklist
1. Code Quality
   - [ ] Follows C# conventions
   - [ ] Proper error handling
   - [ ] Efficient database queries
   - [ ] RLS policy verification

2. Security
   - [ ] Authentication checks
   - [ ] RLS policy testing
   - [ ] Input validation
   - [ ] Error message security

3. Documentation
   - [ ] Code comments
   - [ ] API documentation
   - [ ] Security notes
   - [ ] Test coverage

## Monitoring and Maintenance

### 1. Performance Monitoring
- Query execution times
- File operation latency
- Authentication response times

### 2. Security Monitoring
- Failed authentication attempts
- RLS policy violations
- Storage access patterns

### 3. Error Tracking
- Application exceptions
- Database errors
- Storage operation failures

## Documentation Standards

### 1. Code Documentation
```csharp
/// <summary>
/// Uploads a file to patient's folder with RLS validation
/// </summary>
/// <param name="patientCode">Patient identifier</param>
/// <param name="filePath">Local file path</param>
/// <returns>Upload result with metadata</returns>
```

### 2. API Documentation
- Endpoint descriptions
- Request/response formats
- Authentication requirements
- RLS considerations

### 3. Security Documentation
- RLS policy documentation
- Access control matrices
- Security test results
- Audit procedures

## Issue Management

### Issue Categories
1. Feature Requests
   - New functionality
   - Enhancements
   - Integration requests

2. Bug Reports
   - Security issues
   - Functional bugs
   - Performance problems

3. Documentation
   - Updates needed
   - Clarifications
   - Examples required

### Issue Template
```markdown
## Description
[Issue description]

## Steps to Reproduce
1. [Step 1]
2. [Step 2]
3. [Step 3]

## Expected Behavior
[What should happen]

## Current Behavior
[What actually happens]

## Additional Context
[Screenshots, logs, etc.]
```

## Release Process

### 1. Pre-release Checklist
- [ ] All tests passing
- [ ] Documentation updated
- [ ] RLS policies verified
- [ ] Performance benchmarks met

### 2. Release Steps
1. Version bump
2. Update changelog
3. Create release branch
4. Deploy database changes
5. Deploy application
6. Tag release

### 3. Post-release
- Monitor performance
- Track error rates
- Gather feedback
- Plan next iteration 

## Deployment
Currently, this is a console application and does not have a formal deployment process. Future web or desktop versions will have მათი deployment strategies documented here.

## Refactoring Strategy
The project has evolved from a simple Proof of Concept to a more structured application. Key refactoring principles include:
- **Separation of Concerns:** Code is organized into distinct layers (Models, Clients, Tests, Utils) to improve clarity and reduce coupling.
- **Centralized Models:** All data structures are defined in a single `Models` directory to ensure consistency.
- **Isolating Legacy Code:** The original POC client implementations are preserved but moved into a `Clients` directory to clearly separate them from the newer, more robust RLS implementation. This allows for continued comparison while focusing development on the new architecture. 