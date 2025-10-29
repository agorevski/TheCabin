# Story Pack Validator

A validation utility for The Cabin game that checks story packs, puzzles, and achievements for configuration errors and inconsistencies.

## What It Validates

### Object References
- Verifies that all puzzle step `targetObject` values reference objects that exist in the story pack
- Checks for missing or invalid object IDs

### Location References
- Ensures all `requiredLocation` values reference valid room IDs in the story pack
- Identifies invalid room references

### Flag Consistency
- Validates that `requiredFlags` are set by previous puzzle steps
- Checks for orphaned or undefined flags
- Ensures proper sequential flag dependencies

### Required Items
- Verifies that all `requiredItems` exist in the story pack
- Checks that required items are actually collectable
- Identifies items that can't be obtained by players

### Achievement Mappings
- Validates puzzle `achievementId` references exist in achievement files
- Checks that achievement triggers reference valid puzzle IDs
- Ensures bidirectional mapping consistency

### Object Name Matching
- Identifies potential issues with multi-word object names
- Warns about objects where players might type differently than expected
- Suggests using underscored versions for better matching (e.g., "insulated_coat" vs "coat")

## Usage

### From the tools/StoryPackValidator directory:

```bash
dotnet run
```

### From the project root directory:

```bash
cd tools/StoryPackValidator
dotnet run
```

## Output

The validator provides color-coded output:

- 🔴 **Errors**: Critical issues that will cause game failures
- 🟡 **Warnings**: Potential issues that may cause unexpected behavior
- 🔵 **Info**: Informational messages about the validation process
- 🟢 **Success**: All validations passed

## Example Output

```
=== The Cabin Story Pack Validator ===

Validating theme: arctic_survival...
Validating theme: classic_horror...
Validating theme: cozy_mystery...
Validating theme: fantasy_magic...
Validating theme: sci_fi_isolation...

=== Validation Complete ===
Total Issues Found: 0

✅ All validations passed! No issues found.
```

## Exit Codes

- `0`: No errors found (warnings and info messages are OK)
- `1`: One or more errors found

This makes it suitable for use in CI/CD pipelines.

## Story Pack Themes

The validator checks all five themes:
- arctic_survival
- classic_horror
- cozy_mystery
- fantasy_magic
- sci_fi_isolation

## File Structure

For each theme, the validator expects:
- `story_packs/{theme}.json` - Story pack with rooms and objects
- `story_packs/puzzles_{theme}.json` - Puzzle definitions
- `story_packs/achievements_{theme}.json` - Achievement definitions
