using System.Text.Json;

namespace StoryPackValidator;

public class Validator
{
    private readonly string _storyPacksPath;
    private readonly List<ValidationIssue> _issues = new();
    
    private readonly string[] _themeNames = 
    {
        "arctic_survival",
        "classic_horror",
        "cozy_mystery",
        "fantasy_magic",
        "sci_fi_isolation"
    };

    public Validator(string storyPacksPath)
    {
        _storyPacksPath = storyPacksPath;
    }

    public async Task<List<ValidationIssue>> ValidateAllAsync()
    {
        _issues.Clear();

        foreach (var theme in _themeNames)
        {
            Console.WriteLine($"Validating theme: {theme}...");
            await ValidateThemeAsync(theme);
        }

        return _issues;
    }

    private async Task ValidateThemeAsync(string theme)
    {
        // Load files
        var storyPackPath = Path.Combine(_storyPacksPath, $"{theme}.json");
        var puzzlePath = Path.Combine(_storyPacksPath, $"puzzles_{theme}.json");
        var achievementPath = Path.Combine(_storyPacksPath, $"achievements_{theme}.json");

        // Check file existence
        if (!File.Exists(storyPackPath))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "File Missing",
                Message = $"Story pack file not found: {theme}.json",
                File = storyPackPath
            });
            return;
        }

        if (!File.Exists(puzzlePath))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Warning",
                Category = "File Missing",
                Message = $"Puzzle file not found: puzzles_{theme}.json",
                File = puzzlePath
            });
        }

        if (!File.Exists(achievementPath))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Warning",
                Category = "File Missing",
                Message = $"Achievement file not found: achievements_{theme}.json",
                File = achievementPath
            });
        }

        // Load and parse files
        StoryPack? storyPack = null;
        List<Puzzle> puzzles = new();
        List<Achievement> achievements = new();

        try
        {
            var storyPackJson = await File.ReadAllTextAsync(storyPackPath);
            storyPack = JsonSerializer.Deserialize<StoryPack>(storyPackJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Parse Error",
                Message = $"Failed to parse story pack: {ex.Message}",
                File = storyPackPath
            });
            return;
        }

        if (File.Exists(puzzlePath))
        {
            try
            {
                var puzzleJson = await File.ReadAllTextAsync(puzzlePath);
                puzzles = JsonSerializer.Deserialize<List<Puzzle>>(puzzleJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }
            catch (Exception ex)
            {
                _issues.Add(new ValidationIssue
                {
                    Severity = "Error",
                    Category = "Parse Error",
                    Message = $"Failed to parse puzzle file: {ex.Message}",
                    File = puzzlePath
                });
            }
        }

        if (File.Exists(achievementPath))
        {
            try
            {
                var achievementJson = await File.ReadAllTextAsync(achievementPath);
                achievements = JsonSerializer.Deserialize<List<Achievement>>(achievementJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }
            catch (Exception ex)
            {
                _issues.Add(new ValidationIssue
                {
                    Severity = "Error",
                    Category = "Parse Error",
                    Message = $"Failed to parse achievement file: {ex.Message}",
                    File = achievementPath
                });
            }
        }

        if (storyPack == null) return;

        // Run validations
        ValidateObjectReferences(theme, storyPack, puzzles);
        ValidateLocationReferences(theme, storyPack, puzzles);
        ValidateFlagConsistency(theme, puzzles);
        ValidateRequiredItems(theme, storyPack, puzzles);
        ValidateAchievementMappings(theme, puzzles, achievements);
        ValidateObjectNameMatching(theme, storyPack, puzzles);
    }

    private void ValidateObjectReferences(string theme, StoryPack storyPack, List<Puzzle> puzzles)
    {
        foreach (var puzzle in puzzles)
        {
            foreach (var step in puzzle.Steps)
            {
                if (string.IsNullOrEmpty(step.TargetObject))
                    continue;

                var targetObject = step.TargetObject;
                
                // Check if object exists in story pack
                if (!storyPack.Objects.ContainsKey(targetObject))
                {
                    _issues.Add(new ValidationIssue
                    {
                        Severity = "Error",
                        Category = "Object Reference",
                        Message = $"Puzzle step references non-existent object '{targetObject}'",
                        File = $"puzzles_{theme}.json",
                        Location = $"Puzzle: {puzzle.Id}, Step: {step.Id}"
                    });
                }
            }
        }
    }

    private void ValidateLocationReferences(string theme, StoryPack storyPack, List<Puzzle> puzzles)
    {
        var roomIds = storyPack.Rooms.Select(r => r.Id).ToHashSet();

        foreach (var puzzle in puzzles)
        {
            foreach (var step in puzzle.Steps)
            {
                if (string.IsNullOrEmpty(step.RequiredLocation))
                    continue;

                if (!roomIds.Contains(step.RequiredLocation))
                {
                    _issues.Add(new ValidationIssue
                    {
                        Severity = "Error",
                        Category = "Location Reference",
                        Message = $"Puzzle step requires non-existent location '{step.RequiredLocation}'",
                        File = $"puzzles_{theme}.json",
                        Location = $"Puzzle: {puzzle.Id}, Step: {step.Id}"
                    });
                }
            }
        }
    }

    private void ValidateFlagConsistency(string theme, List<Puzzle> puzzles)
    {
        foreach (var puzzle in puzzles)
        {
            var definedFlags = puzzle.Steps
                .Where(s => !string.IsNullOrEmpty(s.CompletionFlag))
                .Select(s => s.CompletionFlag)
                .ToHashSet();

            foreach (var step in puzzle.Steps)
            {
                foreach (var requiredFlag in step.RequiredFlags)
                {
                    // Check if required flag is set by a previous step
                    var previousSteps = puzzle.Steps.Where(s => s.StepNumber < step.StepNumber).ToList();
                    var isPreviouslyDefined = previousSteps.Any(s => s.CompletionFlag == requiredFlag);

                    if (!isPreviouslyDefined)
                    {
                        _issues.Add(new ValidationIssue
                        {
                            Severity = "Warning",
                            Category = "Flag Consistency",
                            Message = $"Step requires flag '{requiredFlag}' that is not set by any previous step",
                            File = $"puzzles_{theme}.json",
                            Location = $"Puzzle: {puzzle.Id}, Step: {step.Id}"
                        });
                    }
                }
            }
        }
    }

    private void ValidateRequiredItems(string theme, StoryPack storyPack, List<Puzzle> puzzles)
    {
        foreach (var puzzle in puzzles)
        {
            foreach (var step in puzzle.Steps)
            {
                foreach (var requiredItem in step.RequiredItems)
                {
                    // Check if item exists in story pack
                    if (!storyPack.Objects.ContainsKey(requiredItem))
                    {
                        _issues.Add(new ValidationIssue
                        {
                            Severity = "Error",
                            Category = "Item Reference",
                            Message = $"Step requires non-existent item '{requiredItem}'",
                            File = $"puzzles_{theme}.json",
                            Location = $"Puzzle: {puzzle.Id}, Step: {step.Id}"
                        });
                        continue;
                    }

                    // Check if item is collectable
                    var obj = storyPack.Objects[requiredItem];
                    if (!obj.IsCollectable)
                    {
                        _issues.Add(new ValidationIssue
                        {
                            Severity = "Error",
                            Category = "Item Reference",
                            Message = $"Step requires item '{requiredItem}' that is not collectable",
                            File = $"puzzles_{theme}.json",
                            Location = $"Puzzle: {puzzle.Id}, Step: {step.Id}"
                        });
                    }
                }
            }
        }
    }

    private void ValidateAchievementMappings(string theme, List<Puzzle> puzzles, List<Achievement> achievements)
    {
        var achievementIds = achievements.Select(a => a.Id).ToHashSet();

        foreach (var puzzle in puzzles)
        {
            if (!string.IsNullOrEmpty(puzzle.AchievementId) && !achievementIds.Contains(puzzle.AchievementId))
            {
                _issues.Add(new ValidationIssue
                {
                    Severity = "Error",
                    Category = "Achievement Mapping",
                    Message = $"Puzzle references non-existent achievement '{puzzle.AchievementId}'",
                    File = $"puzzles_{theme}.json",
                    Location = $"Puzzle: {puzzle.Id}"
                });
            }
        }

        // Check if achievements reference valid puzzles
        var puzzleIds = puzzles.Select(p => p.Id).ToHashSet();
        
        foreach (var achievement in achievements)
        {
            if (achievement.Trigger?.Type == "PuzzleSolved" && 
                !string.IsNullOrEmpty(achievement.Trigger.PuzzleId) &&
                !puzzleIds.Contains(achievement.Trigger.PuzzleId))
            {
                _issues.Add(new ValidationIssue
                {
                    Severity = "Error",
                    Category = "Achievement Mapping",
                    Message = $"Achievement references non-existent puzzle '{achievement.Trigger.PuzzleId}'",
                    File = $"achievements_{theme}.json",
                    Location = $"Achievement: {achievement.Id}"
                });
            }
        }
    }

    private void ValidateObjectNameMatching(string theme, StoryPack storyPack, List<Puzzle> puzzles)
    {
        foreach (var puzzle in puzzles)
        {
            foreach (var step in puzzle.Steps)
            {
                if (string.IsNullOrEmpty(step.TargetObject))
                    continue;

                if (!storyPack.Objects.TryGetValue(step.TargetObject, out var obj))
                    continue;

                // Check if object name contains spaces/multiple words
                if (obj.Name.Contains(' '))
                {
                    var nameWithUnderscore = obj.Name.ToLowerInvariant().Replace(' ', '_');
                    
                    // If the object ID doesn't match the underscored version of the name, warn
                    if (step.TargetObject != nameWithUnderscore && !step.TargetObject.Contains('_'))
                    {
                        _issues.Add(new ValidationIssue
                        {
                            Severity = "Warning",
                            Category = "Object Name Matching",
                            Message = $"Object '{obj.Name}' has multi-word name. Puzzle uses '{step.TargetObject}' but players might type '{nameWithUnderscore}'. Consider using '{nameWithUnderscore}' in puzzle.",
                            File = $"puzzles_{theme}.json",
                            Location = $"Puzzle: {puzzle.Id}, Step: {step.Id}"
                        });
                    }
                }
            }
        }
    }
}
