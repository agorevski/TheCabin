# Phase 17D: Enhanced Puzzles - Implementation Plan

## Overview
Implement an advanced puzzle system that supports multi-step puzzles, hints, complex object interactions, and puzzle-specific achievements.

## Goals
1. ✅ Multi-step puzzle system with state tracking
2. ✅ Hint system with progressive reveals
3. ✅ Complex object combinations and interactions
4. ✅ Puzzle completion tracking and achievements
5. ✅ Enhanced PuzzleEngine with validation
6. ✅ Comprehensive test coverage

## Current State Analysis

### Existing Puzzle System
- Basic PuzzleEngine in `src/TheCabin.Core/Engine/PuzzleEngine.cs`
- Simple puzzle checking (e.g., lantern in dark room)
- Achievement integration exists
- Limited to single-step puzzles

### What Needs Enhancement
1. **Multi-step puzzles** - Puzzles requiring multiple actions in sequence
2. **Puzzle state persistence** - Track partial progress
3. **Hint system** - Progressive hints without spoiling solutions
4. **Object combinations** - Use multiple items together
5. **Puzzle validation** - Check if puzzle can be solved with current inventory
6. **Better feedback** - Clear messages for puzzle progress

## Architecture Design

### 1. Enhanced Data Models

#### Puzzle Definition Model
```csharp
public class Puzzle
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public PuzzleType Type { get; set; }
    public List<PuzzleStep> Steps { get; set; }
    public List<string> RequiredItems { get; set; }
    public string CompletionMessage { get; set; }
    public string CompletionAchievementId { get; set; }
    public List<Hint> Hints { get; set; }
}

public enum PuzzleType
{
    Sequential,      // Steps must be done in order
    Combinatorial,   // Steps can be done in any order
    Timed,          // Must be completed within time limit
    Environmental   // Based on room/world state
}

public class PuzzleStep
{
    public int StepNumber { get; set; }
    public string Description { get; set; }
    public List<string> RequiredFlags { get; set; }
    public List<string> RequiredItems { get; set; }
    public string CompletionFlag { get; set; }
    public string FeedbackMessage { get; set; }
}

public class Hint
{
    public int Level { get; set; }  // 1 = subtle, 2 = moderate, 3 = obvious
    public string Text { get; set; }
    public int Cost { get; set; }    // Optional: hints could cost something
}
```

#### Puzzle State Model
```csharp
public class PuzzleState
{
    public string PuzzleId { get; set; }
    public bool IsCompleted { get; set; }
    public List<int> CompletedSteps { get; set; }
    public int HintsUsed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Attempts { get; set; }
}
```

### 2. Enhanced PuzzleEngine Interface

```csharp
public interface IPuzzleEngine
{
    // Existing method
    Task<PuzzleResult> CheckPuzzleCompletionAsync(GameState gameState);
    
    // New methods for Phase 17D
    Task<List<Puzzle>> GetActivePuzzlesAsync(GameState gameState);
    Task<PuzzleState> GetPuzzleStateAsync(string puzzleId, GameState gameState);
    Task<bool> CanSolvePuzzleAsync(string puzzleId, GameState gameState);
    Task<PuzzleStepResult> AttemptPuzzleStepAsync(string puzzleId, int stepNumber, GameState gameState);
    Task<Hint> GetNextHintAsync(string puzzleId, GameState gameState);
    Task<List<string>> ValidateInventoryForPuzzleAsync(string puzzleId, GameState gameState);
}
```

### 3. New Command Handlers

#### HintCommandHandler
```csharp
public class HintCommandHandler : ICommandHandler
{
    public string Verb => "hint";
    
    // Provides hints for active puzzles
    // Progressive hint system (level 1 -> 2 -> 3)
}
```

#### CombineCommandHandler
```csharp
public class CombineCommandHandler : ICommandHandler
{
    public string Verb => "combine";
    
    // Combines two items: "combine key with lockpick"
    // Creates new items from combinations
}
```

## Implementation Steps

### Step 1: Enhanced Data Models ✅
- [ ] Create `Puzzle.cs` model
- [ ] Create `PuzzleStep.cs` model  
- [ ] Create `PuzzleState.cs` model
- [ ] Create `Hint.cs` model
- [ ] Add `PuzzleType` enum to Enums.cs
- [ ] Update `GameState` to include puzzle states

### Step 2: Update PuzzleEngine ✅
- [ ] Implement new IPuzzleEngine methods
- [ ] Add multi-step puzzle support
- [ ] Add puzzle state tracking
- [ ] Add hint management
- [ ] Add puzzle validation logic
- [ ] Keep backward compatibility

### Step 3: Create Sample Puzzles ✅
- [ ] Design 3-4 multi-step puzzles for classic_horror theme
- [ ] Create puzzle JSON definitions
- [ ] Add puzzle-specific achievements
- [ ] Example puzzles:
  - **Locked Chest Puzzle**: Find key, find lockpick, combine, open
  - **Mirror Reflection Puzzle**: Light candles in specific order
  - **Hidden Passage**: Move bookshelf, find switch, pull lever
  - **Music Box Puzzle**: Find broken music box, find missing gear, repair, wind up

### Step 4: New Command Handlers ✅
- [ ] Implement HintCommandHandler
- [ ] Implement CombineCommandHandler  
- [ ] Register handlers in dependency injection
- [ ] Add to CommandRouter

### Step 5: Update StoryPack Format ✅
- [ ] Add puzzles array to StoryPack model
- [ ] Update JSON schema
- [ ] Create migration for existing story packs
- [ ] Update StoryPackService to load puzzles

### Step 6: Testing ✅
- [ ] Unit tests for enhanced PuzzleEngine
- [ ] Unit tests for new command handlers
- [ ] Integration tests for multi-step puzzles
- [ ] Integration tests for hint system
- [ ] Integration tests for object combinations
- [ ] Test puzzle state persistence

### Step 7: Update Story Packs ✅
- [ ] Add puzzles to classic_horror.json
- [ ] Add puzzles to other story packs
- [ ] Ensure backward compatibility
- [ ] Test all story packs load correctly

## Example Puzzle: Locked Chest

### Puzzle Definition (JSON)
```json
{
  "id": "locked_chest_puzzle",
  "name": "The Locked Chest",
  "description": "An ancient chest with a complex lock mechanism",
  "type": "Sequential",
  "steps": [
    {
      "stepNumber": 1,
      "description": "Find the rusty key",
      "requiredFlags": [],
      "requiredItems": [],
      "completionFlag": "found_rusty_key",
      "feedbackMessage": "You've found a rusty key, but it won't fit the lock directly."
    },
    {
      "stepNumber": 2,
      "description": "Find a lockpick or file",
      "requiredFlags": ["found_rusty_key"],
      "requiredItems": [],
      "completionFlag": "found_lockpick",
      "feedbackMessage": "This lockpick might help modify the key."
    },
    {
      "stepNumber": 3,
      "description": "Combine the key with the lockpick",
      "requiredFlags": ["found_rusty_key", "found_lockpick"],
      "requiredItems": ["rusty_key", "lockpick"],
      "completionFlag": "modified_key",
      "feedbackMessage": "You've modified the key. It should fit the lock now."
    },
    {
      "stepNumber": 4,
      "description": "Use the modified key on the chest",
      "requiredFlags": ["modified_key"],
      "requiredItems": ["modified_key"],
      "completionFlag": "chest_opened",
      "feedbackMessage": "The lock clicks open! Inside you find ancient treasures."
    }
  ],
  "requiredItems": ["rusty_key", "lockpick"],
  "completionMessage": "Puzzle solved! You've opened the locked chest and discovered its secrets.",
  "completionAchievementId": "master_lockpicker",
  "hints": [
    {
      "level": 1,
      "text": "The key alone won't work. Maybe you need another tool?",
      "cost": 0
    },
    {
      "level": 2,
      "text": "Try combining the key with a lockpick or file.",
      "cost": 0
    },
    {
      "level": 3,
      "text": "Use the command: 'combine key with lockpick', then 'use modified key on chest'",
      "cost": 0
    }
  ]
}
```

### User Interaction Flow
```
Player: "examine chest"
Game: "An ancient wooden chest with a complex brass lock. It seems to require a special key."

Player: "hint"
Game: "Hint 1/3: The key alone won't work. Maybe you need another tool?"

Player: "take key"
Game: "You pick up the rusty key."

Player: "use key on chest"
Game: "The key doesn't fit properly. It needs modification."

Player: "hint"
Game: "Hint 2/3: Try combining the key with a lockpick or file."

Player: "combine key with lockpick"
Game: "You carefully modify the key using the lockpick. It should fit now."
Achievement Unlocked: "Improviser"

Player: "use modified key on chest"
Game: "The lock clicks open! Inside you find ancient treasures."
Achievement Unlocked: "Master Lockpicker"
Puzzle Complete: The Locked Chest
```

## File Structure

### New Files
```
src/TheCabin.Core/Models/
  ├── Puzzle.cs
  ├── PuzzleStep.cs
  ├── PuzzleState.cs
  └── Hint.cs

src/TheCabin.Core/Engine/CommandHandlers/
  ├── HintCommandHandler.cs
  └── CombineCommandHandler.cs

tests/TheCabin.Core.Tests/Engine/
  ├── EnhancedPuzzleEngineTests.cs
  └── CommandHandlers/
      ├── HintCommandHandlerTests.cs
      └── CombineCommandHandlerTests.cs

story_packs/
  └── puzzles_classic_horror.json
```

### Modified Files
```
src/TheCabin.Core/Models/
  ├── GameState.cs (add puzzle states)
  ├── StoryPack.cs (add puzzles list)
  └── Enums.cs (add PuzzleType)

src/TheCabin.Core/Interfaces/
  └── IPuzzleEngine.cs (add new methods)

src/TheCabin.Core/Engine/
  └── PuzzleEngine.cs (implement enhancements)

src/TheCabin.Core/Services/
  └── StoryPackService.cs (load puzzles)

story_packs/
  ├── classic_horror.json (add puzzles)
  ├── arctic_survival.json (add puzzles)
  └── ... (other story packs)
```

## Success Criteria
- [ ] Multi-step puzzles work correctly
- [ ] Puzzle state persists across save/load
- [ ] Hint system provides progressive help
- [ ] Object combination creates new items
- [ ] Puzzle validation prevents impossible states
- [ ] Achievements trigger on puzzle completion
- [ ] All tests pass (target: 150+ total tests)
- [ ] Backward compatibility maintained
- [ ] Documentation updated

## Testing Strategy

### Unit Tests
1. **PuzzleEngine Tests**
   - Multi-step puzzle progression
   - Puzzle state management
   - Hint system
   - Validation logic
   - Achievement integration

2. **Command Handler Tests**
   - HintCommandHandler (various scenarios)
   - CombineCommandHandler (valid/invalid combinations)

### Integration Tests
1. **End-to-End Puzzle Solving**
   - Complete locked chest puzzle
   - Complete mirror reflection puzzle
   - Test puzzle with missing items
   - Test hint progression

2. **Save/Load Tests**
   - Save mid-puzzle
   - Load and continue
   - Verify state persistence

## Timeline Estimate
- Step 1: Models (1-2 hours)
- Step 2: Enhanced PuzzleEngine (2-3 hours)
- Step 3: Sample Puzzles (1-2 hours)
- Step 4: Command Handlers (2-3 hours)
- Step 5: StoryPack Updates (1 hour)
- Step 6: Testing (2-3 hours)
- Step 7: Story Pack Content (1-2 hours)

**Total: 10-16 hours**

## Next Steps After 17D
**Phase 17E**: Testing & Polish
- Comprehensive integration testing
- Performance optimization
- Bug fixes
- Documentation
- Final QA before release

## Notes
- Keep puzzle complexity balanced (not too hard, not too easy)
- Ensure puzzles are voice-command friendly
- Provide clear feedback at each step
- Make hints genuinely helpful without spoiling
- Consider accessibility (puzzles shouldn't be frustrating)

---

**Status**: READY TO IMPLEMENT  
**Priority**: HIGH  
**Dependencies**: Phase 17A, 17B, 17C complete ✅
