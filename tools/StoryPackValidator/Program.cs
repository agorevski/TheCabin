using System.Text.Json;
using StoryPackValidator;

Console.WriteLine("=== The Cabin Story Pack Validator ===\n");

// Get the story_packs directory (relative to the validator tool)
var storyPacksPath = Path.Combine("..", "..", "story_packs");

if (!Directory.Exists(storyPacksPath))
{
    Console.WriteLine($"ERROR: Story packs directory not found at: {Path.GetFullPath(storyPacksPath)}");
    return 1;
}

var validator = new Validator(storyPacksPath);
var issues = await validator.ValidateAllAsync();

// Display results
Console.WriteLine($"\n=== Validation Complete ===");
Console.WriteLine($"Total Issues Found: {issues.Count}\n");

var errors = issues.Where(i => i.Severity == "Error").ToList();
var warnings = issues.Where(i => i.Severity == "Warning").ToList();
var infos = issues.Where(i => i.Severity == "Info").ToList();

if (errors.Any())
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n❌ ERRORS ({errors.Count}):");
    Console.ResetColor();
    foreach (var error in errors)
    {
        Console.WriteLine($"  [{error.Category}] {error.Message}");
        if (!string.IsNullOrEmpty(error.File))
            Console.WriteLine($"    File: {error.File}");
        if (!string.IsNullOrEmpty(error.Location))
            Console.WriteLine($"    Location: {error.Location}");
        Console.WriteLine();
    }
}

if (warnings.Any())
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n⚠️  WARNINGS ({warnings.Count}):");
    Console.ResetColor();
    foreach (var warning in warnings)
    {
        Console.WriteLine($"  [{warning.Category}] {warning.Message}");
        if (!string.IsNullOrEmpty(warning.File))
            Console.WriteLine($"    File: {warning.File}");
        if (!string.IsNullOrEmpty(warning.Location))
            Console.WriteLine($"    Location: {warning.Location}");
        Console.WriteLine();
    }
}

if (infos.Any())
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\nℹ️  INFO ({infos.Count}):");
    Console.ResetColor();
    foreach (var info in infos)
    {
        Console.WriteLine($"  [{info.Category}] {info.Message}");
        if (!string.IsNullOrEmpty(info.File))
            Console.WriteLine($"    File: {info.File}");
        Console.WriteLine();
    }
}

if (!issues.Any())
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ All validations passed! No issues found.");
    Console.ResetColor();
}

return errors.Any() ? 1 : 0;
