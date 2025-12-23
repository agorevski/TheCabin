# API Reference

This section contains the auto-generated API documentation for The Cabin project.

## Namespaces

- **TheCabin.Core** - Core game engine, state management, and domain models
- **TheCabin.Infrastructure** - External service integrations (voice, storage, etc.)
- **TheCabin.Console** - Console application for development and testing
- **TheCabin.Maui** - Cross-platform UI application

## Getting Started

To regenerate the API documentation:

```bash
# Install DocFX (if not already installed)
dotnet tool install -g docfx

# Generate documentation
docfx docfx.json

# Serve locally
docfx docfx.json --serve
```

## XML Documentation

All public APIs should include XML documentation comments. Example:

```csharp
/// <summary>
/// Represents a room in the game world.
/// </summary>
/// <param name="id">Unique identifier for the room.</param>
/// <param name="description">Narrative description shown to the player.</param>
public class Room
{
    // ...
}
```
