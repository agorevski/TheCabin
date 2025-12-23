# ADR-0002: JSON-Based Story Pack Format

## Status

Accepted

## Date

2025-12-23

## Context

The Cabin is a text adventure game that needs to support multiple themed content packs (stories). We needed a format for defining rooms, objects, puzzles, and narrative content that is:
- Easy to create and edit
- Validateable at build time
- Extensible for new content types
- Portable across platforms

## Decision

We use JSON files for story pack definitions, with a defined schema for validation. Story packs are stored in the `/story_packs` directory and validated by the `StoryPackValidator` tool.

## Consequences

### Positive

- Human-readable and editable with any text editor
- Easy to version control and diff
- Schema validation catches errors early
- Can be generated dynamically by GPT/LLM
- Lightweight and fast to parse

### Negative

- No compile-time type checking (mitigated by schema validation)
- Large story packs may have performance implications
- Comments not natively supported in JSON

### Neutral

- Story pack authors need to understand the JSON schema
- Tooling needed for validation (StoryPackValidator)

## Alternatives Considered

1. **YAML** - More readable but parsing libraries are heavier and less performant.
2. **XML** - Verbose and less developer-friendly.
3. **SQLite database** - More complex to edit and version control.
4. **C# code** - Requires compilation, not suitable for user-generated content.

## References

- [Story Pack Schema](../../story_packs/)
- [StoryPackValidator Tool](../../tools/StoryPackValidator/)
