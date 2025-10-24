using System.Text.Json;
using TheCabin.Core.Interfaces;
using TheCabin.Core.Models;

namespace TheCabin.Core.Services;

/// <summary>
/// LLM-based command parser using OpenAI API
/// </summary>
public class LlmCommandParserService : ICommandParserService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILocalCommandParser _fallbackParser;
    
    private const string SystemPrompt = @"You are a command parser for a text adventure game. 
Parse the player's input into a structured JSON command.

Available verbs: go, take, drop, use, open, close, examine, look, read, push, pull, inventory, help

Output format:
{
  ""verb"": ""action verb"",
  ""object"": ""target object or null"",
  ""target"": ""secondary object or null"",
  ""context"": ""additional context"",
  ""confidence"": 0.0-1.0
}

Examples:
Input: 'go north' → {""verb"":""go"",""object"":""north"",""confidence"":0.95}
Input: 'pick up the lantern' → {""verb"":""take"",""object"":""lantern"",""confidence"":0.9}
Input: 'use key on door' → {""verb"":""use"",""object"":""key"",""target"":""door"",""confidence"":0.85}
Input: 'look around' → {""verb"":""look"",""object"":null,""confidence"":0.95}";
    
    public LlmCommandParserService(
        HttpClient httpClient,
        string apiKey,
        ILocalCommandParser fallbackParser)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _fallbackParser = fallbackParser ?? throw new ArgumentNullException(nameof(fallbackParser));
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }
    
    public async Task<ParsedCommand> ParseAsync(string input, GameContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ParsedCommand
            {
                Verb = "help",
                Confidence = 0.5,
                RawInput = input
            };
        }
        
        try
        {
            // Build context information for better parsing
            var contextInfo = BuildContextInfo(context);
            
            var userPrompt = $@"
Context:
- Location: {context.CurrentLocation}
- Visible objects: {string.Join(", ", context.VisibleObjects)}
- Inventory: {string.Join(", ", context.InventoryItems)}
- Recent actions: {string.Join(", ", context.RecentCommands.TakeLast(3))}

Player input: ""{input}""

Parse this command into JSON.";
            
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3,
                max_tokens = 150,
                response_format = new { type = "json_object" }
            };
            
            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<LlmApiResponse>(jsonResponse);
                
                if (apiResponse?.Choices?.Length > 0)
                {
                    var messageContent = apiResponse.Choices[0].Message.Content;
                    var parsed = JsonSerializer.Deserialize<ParsedCommand>(messageContent);
                    
                    if (parsed != null)
                    {
                        parsed.RawInput = input;
                        parsed.Timestamp = DateTime.UtcNow;
                        return parsed;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fall back to local parser on any error
        }
        
        // Fallback to local parser
        return await _fallbackParser.ParseAsync(input, context);
    }
    
    private string BuildContextInfo(GameContext context)
    {
        return JsonSerializer.Serialize(new
        {
            location = context.CurrentLocation,
            visible = context.VisibleObjects,
            inventory = context.InventoryItems,
            flags = context.GameFlags
        });
    }
    
    // API response models
    private class LlmApiResponse
    {
        public Choice[]? Choices { get; set; }
    }
    
    private class Choice
    {
        public Message Message { get; set; } = new();
    }
    
    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}
