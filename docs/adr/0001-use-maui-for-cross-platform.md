# ADR-0001: Use .NET MAUI for Cross-Platform Development

## Status

Accepted

## Date

2025-12-23

## Context

The Cabin needs to run on multiple platforms (Android and Windows) with a native-feeling UI and access to platform-specific APIs like speech recognition and text-to-speech. We needed to choose a cross-platform framework that would allow code sharing while maintaining native performance.

## Decision

We chose .NET MAUI (Multi-platform App UI) as our cross-platform framework for building The Cabin.

## Consequences

### Positive

- Single codebase for Android and Windows with native performance
- Full access to .NET ecosystem and C# language features
- Native platform API access through dependency injection
- Strong tooling support in Visual Studio
- Shared business logic in TheCabin.Core library

### Negative

- Larger app bundle size compared to native-only apps
- Some platform-specific features require custom handlers
- Debugging platform-specific issues can be complex

### Neutral

- Team must be proficient in C# and .NET
- UI is defined in XAML or C# markup

## Alternatives Considered

1. **Flutter** - Cross-platform with good performance, but Dart is less familiar to the team and .NET ecosystem access is limited.
2. **React Native** - JavaScript-based, but native module integration for voice features would be complex.
3. **Native per platform** - Maximum control but requires maintaining separate codebases.

## References

- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [The Cabin Game Design Document](../../The_Cabin_Game_Design_Doc.md)
