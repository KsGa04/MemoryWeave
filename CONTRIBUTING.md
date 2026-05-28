# Contributing to MemoryWeave

Thank you for your interest in contributing to MemoryWeave! This document provides guidelines and instructions.

## Code of Conduct

Be respectful, constructive, and professional in all interactions.

## How to Contribute

### Reporting Bugs

1. Use GitHub Issues
2. Include:
   - Clear description of the bug
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment (OS, Python version, .NET version)
   - Logs or error messages

### Suggesting Features

1. Open GitHub Discussion or Issue
2. Describe the feature and use case
3. Discuss implementation approach
4. Get approval before starting implementation

### Pull Requests

1. Fork the repository
2. Create feature branch: `feature/your-feature-name`
3. Make commits with clear messages
4. Add tests for new functionality
5. Ensure tests pass: `pytest` (backend) or `dotnet test` (frontend)
6. Push to your fork
7. Create Pull Request with description
8. Address review feedback
9. Merge when approved

## Development Setup

See [docs/development.md](docs/development.md)

## Code Style

See [docs/development.md](docs/development.md) for Python and C# style guides.

## Testing Requirements

- Backend: Minimum 80% code coverage
- Frontend: Unit tests for ViewModels
- Integration tests for critical paths

## Documentation

- Update relevant documentation when making changes
- Add docstrings to new functions/classes
- Update README if adding new features

## Questions?

Open an issue or discussion in GitHub.
