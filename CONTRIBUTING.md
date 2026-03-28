# Contributing to SharpHSQL

Thank you for your interest in contributing to SharpHSQL! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Submitting Changes](#submitting-changes)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Enhancements](#suggesting-enhancements)

## Code of Conduct

This project adheres to a code of conduct that all contributors are expected to follow:

- **Be respectful**: Treat all contributors with respect and courtesy
- **Be collaborative**: Work together constructively
- **Be professional**: Keep discussions focused on technical matters
- **Be inclusive**: Welcome newcomers and help them get started

## How Can I Contribute?

There are many ways to contribute to SharpHSQL:

### 1. Report Bugs
Found a bug? [Open an issue](https://github.com/andresvettori/sharphsql/issues/new) with:
- Clear title and description
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, etc.)
- Code samples if applicable

### 2. Suggest Enhancements
Have an idea for a new feature? [Create an enhancement issue](https://github.com/andresvettori/sharphsql/issues/new) with:
- Clear description of the feature
- Use cases and benefits
- Possible implementation approach
- Any related examples or references

### 3. Submit Pull Requests
Ready to code? Great! See the [Submitting Changes](#submitting-changes) section below.

### 4. Improve Documentation
Documentation is always welcome:
- Fix typos or unclear explanations
- Add examples
- Improve API documentation
- Translate documentation (future)

### 5. Answer Questions
Help others by:
- Responding to issues
- Participating in discussions
- Helping newcomers get started

## Development Setup

### Prerequisites

- **.NET SDK 6.0 or higher** - [Download](https://dotnet.microsoft.com/download)
- **Git** - [Download](https://git-scm.com/downloads)
- **IDE**: Visual Studio 2022, VS Code, or Rider (recommended)
- **PowerShell** - For running build scripts

### Getting Started

1. **Fork the repository**
   ```bash
   # Click "Fork" on GitHub, then clone your fork
   git clone https://github.com/YOUR-USERNAME/sharphsql.git
   cd sharphsql/sourceCode/src/Main/Source/SharpHSQL
   ```

2. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/andresvettori/sharphsql.git
   ```

3. **Create a branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```

4. **Restore dependencies**
   ```bash
   dotnet restore SharpHSQL.sln
   ```

5. **Build the solution**
   ```bash
   dotnet build SharpHSQL.sln
   # or use the build script
   ./build.ps1
   ```

6. **Run tests**
   ```bash
   dotnet test SharpHSQL.Tests/SharpHSQL.Tests.csproj
   # or
   ./build.ps1 -Configuration Debug
   ```

## Project Structure

```
SharpHSQL/
├── build.ps1                    # Build automation script
├── GitVersion.yml               # Version configuration
├── README.md                    # Project overview
├── CHANGELOG.md                 # Version history
├── CONTRIBUTING.md              # This file
├── LICENSE                      # BSD 3-Clause License
├── SharpHSQL.sln               # Solution file
│
├── SharpHSQL/                   # Main library
│   ├── SharpHsql.csproj        # Project file
│   ├── AssemblyInfo.cs         # Assembly metadata
│   ├── Database.cs             # Core database engine
│   ├── Parser.cs               # SQL parser
│   ├── Expression.cs           # Expression evaluator
│   ├── Function.cs             # Function handler
│   ├── Library.cs              # Built-in functions
│   └── Provider/               # ADO.NET provider
│       ├── SharpHsqlConnection.cs
│       ├── SharpHsqlCommand.cs
│       ├── SharpHsqlReader.cs
│       └── ...
│
├── SharpHSQL.Tests/             # Unit tests
│   ├── DatabaseConnectionTests.cs
│   ├── ProviderConnectionTests.cs
│   ├── DataTypeTests.cs
│   └── ...
│
├── SharpHSQL.Reliability.Tests/ # Reliability tests
│   └── ...
│
└── docs/                        # Documentation
    ├── QuickStart.md
    └── Technical-Reference.md
```

## Coding Standards

### C# Style Guide

We follow standard C# conventions with some specific guidelines:

#### Naming Conventions
```csharp
// Classes, Methods, Properties: PascalCase
public class DatabaseConnection
{
    public string ConnectionString { get; set; }
    public void OpenConnection() { }
}

// Local variables, parameters: camelCase
public void ProcessData(int userId, string userName)
{
    var resultSet = GetResults();
}

// Private fields: _camelCase (with underscore prefix)
private string _connectionString;
private int _timeout;

// Constants: PascalCase
private const int DefaultTimeout = 30;
```

#### Code Formatting
- **Indentation**: 4 spaces (no tabs)
- **Braces**: Opening brace on same line for methods, new line for types
- **Line length**: Aim for 120 characters max
- **Regions**: Minimize use of #region; prefer logical file organization

#### Best Practices
```csharp
// ✓ Good: Use using statements for IDisposable
using var connection = new SharpHsqlConnection(connectionString);
connection.Open();

// ✗ Bad: Manual disposal
var connection = new SharpHsqlConnection(connectionString);
try
{
    connection.Open();
}
finally
{
    connection.Close();
}

// ✓ Good: Meaningful variable names
var activeUsers = GetActiveUsers();

// ✗ Bad: Cryptic names
var au = GetActiveUsers();

// ✓ Good: Early returns for validation
if (user == null)
    return;
    
ProcessUser(user);

// ✗ Bad: Nested conditions
if (user != null)
{
    ProcessUser(user);
}
```

### Documentation

#### XML Documentation
All public APIs must have XML documentation:

```csharp
/// <summary>
/// Executes a SQL query and returns the results.
/// </summary>
/// <param name="sql">The SQL statement to execute.</param>
/// <param name="channel">The channel to use for execution.</param>
/// <returns>A Result object containing the query results.</returns>
/// <exception cref="ArgumentNullException">Thrown when sql or channel is null.</exception>
/// <example>
/// <code>
/// var result = database.Execute("SELECT * FROM Users", channel);
/// </code>
/// </example>
public Result Execute(string sql, Channel channel)
{
    // Implementation
}
```

#### Code Comments
- Use comments to explain **why**, not **what**
- Keep comments up-to-date with code changes
- Avoid obvious comments

```csharp
// ✓ Good: Explains reasoning
// Use a larger buffer for network operations to reduce round trips
var bufferSize = 8192;

// ✗ Bad: States the obvious
// Set buffer size to 8192
var bufferSize = 8192;
```

## Testing Guidelines

### Writing Tests

All new features and bug fixes should include tests:

```csharp
[Fact]
public void Connection_OpensSuccessfully()
{
    // Arrange
    var connectionString = "Initial Catalog=.;User Id=sa;Pwd=";
    using var connection = new SharpHsqlConnection(connectionString);
    
    // Act
    connection.Open();
    
    // Assert
    Assert.Equal(ConnectionState.Open, connection.State);
}

[Theory]
[InlineData("Alice", "alice@example.com")]
[InlineData("Bob", "bob@example.com")]
public void Insert_WithDifferentUsers_Succeeds(string name, string email)
{
    // Arrange & Act & Assert
    using var connection = new SharpHsqlConnection("Initial Catalog=.;User Id=sa;Pwd=");
    connection.Open();
    
    using var cmd = new SharpHsqlCommand($"INSERT INTO Users VALUES (1, '{name}', '{email}')", connection);
    int affected = cmd.ExecuteNonQuery();
    
    Assert.Equal(1, affected);
}
```

### Test Organization
- One test class per production class
- Use descriptive test names: `MethodName_Scenario_ExpectedResult`
- Arrange-Act-Assert pattern
- Clean up resources in `Dispose()` method
- Use in-memory databases for tests

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~DatabaseConnectionTests"

# Run with coverage (requires coverage tool)
dotnet test /p:CollectCoverage=true
```

## Submitting Changes

### Pull Request Process

1. **Ensure tests pass**
   ```bash
   ./build.ps1 -Configuration Release
   ```

2. **Update documentation**
   - Update README.md if adding features
   - Add/update XML documentation
   - Update CHANGELOG.md in "Unreleased" section

3. **Commit your changes**
   ```bash
   git add .
   git commit -m "Add feature: brief description"
   ```
   
   **Commit Message Format:**
   ```
   <type>: <subject>
   
   <body>
   
   <footer>
   ```
   
   **Types:**
   - `feat`: New feature
   - `fix`: Bug fix
   - `docs`: Documentation changes
   - `style`: Code style changes (formatting)
   - `refactor`: Code refactoring
   - `test`: Adding or updating tests
   - `chore`: Maintenance tasks
   
   **Example:**
   ```
   feat: Add LIMIT clause support for SELECT queries
   
   - Implemented LIMIT parsing in Parser.cs
   - Added limitStart and limitCount to Select class
   - Updated GetResult to respect limit parameters
   - Added tests for various LIMIT scenarios
   
   Closes #123
   ```

4. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

5. **Create Pull Request**
   - Go to GitHub and create a PR from your branch
   - Fill out the PR template
   - Link related issues
   - Request review

### Pull Request Checklist

- [ ] Code follows the project's style guidelines
- [ ] All tests pass locally
- [ ] New code has appropriate test coverage
- [ ] XML documentation is complete and accurate
- [ ] CHANGELOG.md is updated
- [ ] No breaking changes (or clearly documented)
- [ ] Commit messages are clear and descriptive

### Review Process

1. Maintainers will review your PR
2. Address any feedback or requested changes
3. Once approved, your PR will be merged
4. Your contribution will be included in the next release

## Reporting Bugs

### Before Submitting

1. **Check existing issues** - Your bug may already be reported
2. **Update to latest version** - The bug may already be fixed
3. **Reproduce the bug** - Ensure it's consistent

### Bug Report Template

```markdown
**Describe the bug**
A clear description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Create connection with '...'
2. Execute query '...'
3. See error

**Expected behavior**
What you expected to happen.

**Actual behavior**
What actually happened.

**Code sample**
```csharp
// Minimal code to reproduce
var connection = new SharpHsqlConnection("...");
connection.Open();
// ...
```

**Environment:**
- OS: [e.g., Windows 11, macOS 13, Ubuntu 22.04]
- .NET Version: [e.g., .NET 6.0, .NET Framework 4.8]
- SharpHSQL Version: [e.g., 2.0.0]

**Additional context**
Any other relevant information.
```

## Suggesting Enhancements

### Enhancement Template

```markdown
**Is your feature request related to a problem?**
A clear description of the problem.

**Describe the solution you'd like**
A clear description of what you want to happen.

**Describe alternatives you've considered**
Any alternative solutions or features you've considered.

**Use cases**
Specific scenarios where this feature would be useful.

**Example usage**
```csharp
// How you envision using the feature
var result = database.Execute("NEW SQL SYNTAX", channel);
```

**Additional context**
Any other context or screenshots about the feature request.
```

## Getting Help

- **Documentation**: Check [docs/](docs/) first
- **Discussions**: Ask questions on [GitHub Discussions](https://github.com/andresvettori/sharphsql/discussions)
- **Issues**: Search existing [issues](https://github.com/andresvettori/sharphsql/issues)

## Recognition

Contributors will be:
- Listed in release notes
- Credited in the project
- Eligible for contributor badge

## License

By contributing, you agree that your contributions will be licensed under the BSD 3-Clause License.

---

Thank you for contributing to SharpHSQL! 🎉
