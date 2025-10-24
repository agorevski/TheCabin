using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Services;

/// <summary>
/// Local rule-based command parser (fallback when LLM is unavailable)
/// </summary>
public class LocalCommandParser : ILocalCommandParser
{
    private static readonly Dictionary<string, List<string>> VerbSynonyms = new()
    {
        ["take"] = new() { "get", "grab", "pickup", "pick", "collect" },
        ["drop"] = new() { "leave", "discard", "put", "place" },
        ["use"] = new() { "activate", "employ", "utilize", "apply" },
        ["open"] = new() { "unlock", "unseal" },
        ["close"] = new() { "shut", "lock", "seal" },
        ["examine"] = new() { "inspect", "check", "study", "investigate", "x" },
        ["look"] = new() { "see", "view", "observe", "peek", "l" },
        ["go"] = new() { "move", "walk", "run", "travel", "head" },
        ["inventory"] = new() { "inv", "i", "items", "carrying" },
        ["help"] = new() { "?", "commands", "what" }
    };
    
    private static readonly HashSet<string> FillerWords = new()
    {
        "the", "a", "an", "to", "at", "in", "on", "with", "and", "or"
    };
    
    public bool CanHandle(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;
        
        var words = input.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return false;
        
        var firstWord = words[0];
        
        // Check if first word matches any known verb or synonym
        foreach (var verb in VerbSynonyms.Keys)
        {
            if (verb == firstWord || VerbSynonyms[verb].Contains(firstWord))
                return true;
        }
        
        return false;
    }
    
    public Task<ParsedCommand> ParseAsync(string input, GameContext context)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.FromResult(new ParsedCommand
            {
                Verb = "help",
                Confidence = 0.5,
                RawInput = input
            });
        }
        
        var words = input.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        
        if (words.Count == 0)
        {
            return Task.FromResult(CreateHelpCommand(input));
        }
        
        // Normalize verb
        var verb = NormalizeVerb(words[0]);
        
        // Extract object and target
        string? obj = null;
        string? target = null;
        
        if (words.Count > 1)
        {
            // Remove filler words but keep "on" and "in" for pattern matching
            var filtered = words.Skip(1)
                .Where(w => !FillerWords.Contains(w) || w == "on" || w == "in")
                .ToList();
            
            if (filtered.Count > 0)
            {
                obj = filtered[0];
                
                // Check for "use X on Y" pattern
                if (filtered.Count >= 3 && filtered[1] == "on")
                {
                    target = filtered[2];
                }
                // Check for "put X in Y" pattern
                else if (filtered.Count >= 3 && filtered[1] == "in")
                {
                    target = filtered[2];
                }
            }
        }
        
        // Calculate confidence based on match quality
        var confidence = CalculateConfidence(verb, obj, context);
        
        return Task.FromResult(new ParsedCommand
        {
            Verb = verb,
            Object = obj,
            Target = target,
            Confidence = confidence,
            RawInput = input,
            Timestamp = DateTime.UtcNow
        });
    }
    
    private string NormalizeVerb(string word)
    {
        // Direct match
        if (VerbSynonyms.ContainsKey(word))
            return word;
        
        // Find in synonyms
        foreach (var kvp in VerbSynonyms)
        {
            if (kvp.Value.Contains(word))
                return kvp.Key;
        }
        
        // Unknown verb - return as-is
        return word;
    }
    
    private double CalculateConfidence(string verb, string? obj, GameContext context)
    {
        double confidence = 0.6; // Base confidence for local parsing
        
        // Boost confidence if verb is well-known
        if (VerbSynonyms.ContainsKey(verb))
        {
            confidence += 0.1;
        }
        
        // Boost confidence if object matches context
        if (obj != null)
        {
            // Check if object is in visible objects
            if (context.VisibleObjects.Any(v => v.Contains(obj, StringComparison.OrdinalIgnoreCase)))
            {
                confidence += 0.15;
            }
            // Check if object is in inventory
            else if (context.InventoryItems.Any(i => i.Contains(obj, StringComparison.OrdinalIgnoreCase)))
            {
                confidence += 0.15;
            }
        }
        
        // Directional commands get high confidence
        if (verb == "go" && obj != null)
        {
            var directions = new[] { "north", "south", "east", "west", "up", "down", 
                                    "n", "s", "e", "w", "u", "d",
                                    "northeast", "northwest", "southeast", "southwest",
                                    "ne", "nw", "se", "sw" };
            
            if (directions.Contains(obj))
            {
                confidence = 0.9;
            }
        }
        
        // System commands get high confidence
        if (verb == "inventory" || verb == "help" || verb == "look")
        {
            confidence = 0.85;
        }
        
        return Math.Min(confidence, 1.0);
    }
    
    private ParsedCommand CreateHelpCommand(string input)
    {
        return new ParsedCommand
        {
            Verb = "help",
            Confidence = 0.5,
            RawInput = input,
            Timestamp = DateTime.UtcNow
        };
    }
}
