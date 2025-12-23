# Contributing to The Cabin

Thank you for your interest in contributing to The Cabin! This document provides guidelines and instructions for contributing to the project.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Environment Setup](#development-environment-setup)
- [Coding Standards](#coding-standards)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting Guidelines](#issue-reporting-guidelines)

## Code of Conduct

Please be respectful and constructive in all interactions. We welcome contributors of all experience levels and backgrounds.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/TheCabin.git
   cd TheCabin
   ```
3. **Add the upstream remote**:
   ```bash
   git remote add upstream https://github.com/agorevski/TheCabin.git
   ```

## Development Environment Setup

### Prerequisites

#### Required for All Platforms

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or higher
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.8+) with the following workloads:
  - .NET MAUI development
  - Mobile development with .NET

#### For Android Development

- Android SDK (API Level 23-34)
- Android device or emulator configured

#### For Windows Development

- Windows 10 (version 1809 / build 17763) or higher
- Windows 10 SDK (installed with Visual Studio MAUI workload)

### Initial Setup

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run tests to verify setup
dotnet test
```

Alternatively, use the provided PowerShell script:

```powershell
.\build-and-test.ps1
```

### Project Structure Overview

```text
TheCabin/
├── src/
│   ├── TheCabin.Maui/           # MAUI app (Android & Windows)
│   ├── TheCabin.Core/           # Game engine and domain logic
│   ├── TheCabin.Infrastructure/ # Data access and external services
│   └── TheCabin.Console/        # Console test harness
├── tests/
│   ├── TheCabin.Core.Tests/
│   └── TheCabin.Infrastructure.Tests/
├── story_packs/                 # JSON theme definitions
├── docs/                        # Comprehensive documentation
└── tools/                       # Development utilities
```

## Coding Standards

### General Guidelines

- Follow standard [C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful and descriptive variable, method, and class names
- Keep methods focused and reasonably sized
- Prefer composition over inheritance where appropriate

### Documentation

- Add XML documentation comments for all public APIs
- Update relevant documentation in `docs/` when making significant changes
- Include inline comments only when code behavior isn't immediately obvious

### Code Style

- Use `var` when the type is obvious from the right-hand side
- Use expression-bodied members for simple properties and methods
- Use pattern matching where it improves readability
- Organize `using` statements: System namespaces first, then third-party, then project namespaces

### Testing Requirements

- Write unit tests for all new functionality
- Maintain or improve existing test coverage
- Tests should be independent and repeatable
- Use descriptive test method names that explain the scenario being tested

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/TheCabin.Core.Tests/
```

## Pull Request Process

### Before Submitting

1. **Create a feature branch** from `main`:
   ```bash
   git checkout main
   git pull upstream main
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes** following the coding standards above

3. **Test your changes**:
   ```bash
   dotnet build
   dotnet test
   ```

4. **Commit your changes** with clear, descriptive messages:
   ```bash
   git commit -m "Add feature: brief description of changes"
   ```

### Submitting Your PR

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Open a Pull Request** against the `main` branch

3. **Fill out the PR template** (if available) with:
   - Description of changes
   - Related issue numbers (if applicable)
   - Testing performed
   - Screenshots (for UI changes)

### PR Review Process

- All PRs require at least one approving review
- Address review feedback promptly
- Keep PRs focused and reasonably sized
- Large changes should be broken into smaller, logical PRs when possible

### After Merge

- Delete your feature branch
- Pull the latest `main` to keep your local repository up to date

## Issue Reporting Guidelines

### Bug Reports

When reporting bugs, please include:

1. **Clear title** describing the issue
2. **Environment details**:
   - Operating system and version
   - .NET SDK version
   - Target platform (Android/Windows)
3. **Steps to reproduce** the issue
4. **Expected behavior** vs **actual behavior**
5. **Screenshots or logs** if applicable
6. **Story pack** being used (if relevant)

### Feature Requests

For feature requests, please describe:

1. **The problem** you're trying to solve
2. **Your proposed solution**
3. **Alternative solutions** you've considered
4. **Additional context** (mockups, examples, etc.)

### Issue Labels

- `bug` - Something isn't working
- `enhancement` - New feature or improvement
- `documentation` - Documentation improvements
- `good first issue` - Good for newcomers
- `help wanted` - Extra attention is needed

---

## Questions?

If you have questions about contributing, feel free to:
- Open a [GitHub Issue](https://github.com/agorevski/TheCabin/issues)
- Review existing [documentation](docs/)

Thank you for contributing to The Cabin! 🎮
