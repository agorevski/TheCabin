# 🎮 The Cabin (Voice Edition)
### Game Design Document (v3)

---

## 1. Overview

**The Cabin (Voice Edition)** is a modern reimagining of the classic text adventure genre — an immersive voice-controlled interactive fiction game for Android and Windows.

Players explore mysterious environments by *speaking commands* instead of typing them. Speech recognition, natural language parsing, and dynamic text-to-speech narration create a hands-free, story-driven experience.

---

## 2. Core Gameplay Loop

1. **Listen:** The app activates the microphone and records the player’s command.
2. **Transcribe:** Speech-to-text (STT) ML model converts voice → text.
3. **Parse:** A GPT-based parser interprets player intent and generates structured JSON commands.
4. **Simulate:** The game engine updates the world state based on those commands.
5. **Respond:** Narration text (and optional TTS) describes the outcome.
6. **Repeat:** The player continues exploring until the mystery resolves.

---

## 3. UX / UI Design

### 3.1 Screen Layout
- **Top Bar:** Location name and player stats (health, light, time).
- **Main Text Panel:** Scrollable story feed with dynamic narrative text.
- **Voice Control Section:**
  - Central microphone button with waveform animation.
  - Transcript preview (“You said: look under the bed”).
- **Inventory Drawer:** Slide-up panel showing objects and tools.
- **Settings Menu:** TTS toggle, difficulty, theme selection.

### 3.2 Voice Experience
- Tap or say “Listen” to activate the mic.
- Speech recognition uses an on-device model (Whisper.cpp or Android SpeechRecognizer).
- Short-term context memory keeps the last 10 player actions for narrative continuity.

---

## 4. System Architecture

### 4.1 Data Flow
```
Player Voice
   ↓
Speech-to-Text (STT)
   ↓
Parser (LLM / GPT)
   ↓
Game Engine (State Machine)
   ↓
Response Text
   ↓
Text-to-Speech (TTS) → Audio Output
```

### 4.2 Components

| Layer | Technology | Description |
|-------|-------------|-------------|
| **Frontend** | .NET MAUI + C# | UI, ViewModels, Views, and game loop management. |
| **STT Layer** | Platform APIs / Whisper.cpp | Converts player voice into text. |
| **Parser Layer** | GPT-based API (OpenAI / Azure OpenAI) | Translates text commands into structured JSON actions. |
| **Engine** | C# State Machine (TheCabin.Core) | Manages rooms, inventory, puzzles, and outcomes. |
| **TTS Layer** | MAUI TextToSpeech / Azure TTS | Narrates game responses. |
| **Storage** | SQLite (sqlite-net-pcl) | Saves world definitions, progress, and content packs. |
| **Optional Backend** | Azure / AWS | Theme content delivery, analytics, updates. |

---

## 5. Data Models

### 5.1 Room Model
```json
{
  "id": "cabin_main",
  "description": "You stand in a dimly lit wooden cabin...",
  "objects": ["lantern", "door"],
  "exits": {"north": "forest_edge"}
}
```

### 5.2 Object Model
```json
{
  "id": "lantern",
  "state": "unlit",
  "actions": {
    "take": "You pick up the lantern.",
    "light": "It flickers to life."
  }
}
```

### 5.3 Player State
```json
{
  "inventory": ["note"],
  "health": 100,
  "location": "cabin_main",
  "light_level": "low"
}
```

---

## 6. Implementation Details

### 6.1 Speech Recognition
- Default: Android’s built-in **SpeechRecognizer** API.
- Offline mode: Integrate **Whisper.cpp** or **Vosk** for on-device inference.
- Output confidence threshold: 0.75 (re-prompt player if lower).

### 6.2 Command Parsing
- Use GPT-4o-mini or local distilled LLM to translate text into structured JSON commands.
- Example Input: “Pick up the lantern on the table.”  
- Example Output:
  ```json
  {"verb": "take", "object": "lantern", "context": "table"}
  ```

### 6.3 Game Engine
- Built in C# as a finite state machine using TheCabin.Core.
- Each command triggers transitions between rooms, updates object states, and appends to the story log.
- Uses async/await patterns for async I/O (voice recording, GPT calls, TTS playback).

### 6.4 TTS Narration
- Uses MAUI TextToSpeech API by default.
- Supports dynamic voice selection and speech speed adjustment.
- Optional: integrate Azure TTS or ElevenLabs API for realistic narration.

### 6.5 Content Management
- Each theme pack (JSON) is stored in `/assets/themes/`.
- New packs can be downloaded or generated dynamically from GPT.

---

## 7. Thematic Packs

Prebuilt content packs (5 included):
- **Classic Horror:** Haunted log cabin, decaying interiors, candlelight.
- **Arctic Survival:** Polar outpost, dwindling power, freezing tension.
- **Fantasy Magic:** Wizard’s workshop, glowing runes, spellcraft puzzles.
- **Sci-Fi Isolation:** Derelict space module, system malfunctions, alien dread.
- **Cozy Mystery:** Snowbound lodge, warm atmosphere, hidden secrets.

---

## 8. Meta-Prompts (for Generating New Content)

### Theme Generator
```
Generate 10 new themes for 'The Cabin' game.
Each should include: setting, mood, gameplay style, and short pitch.
```

### Content Pack Creator
```
Using theme [THEME], generate a JSON content bundle:
- theme, description, 6-10 rooms, interconnected exits
- each room has id, description, objects, exits
Ensure thematic consistency.
```

### Object Expansion
```
Generate JSON behaviors for objects in theme [THEME]:
- actions (take, use, open, combine)
- state transitions
```

### Dynamic Events
```
For theme [THEME], produce 5 random narrative events:
- trigger_condition
- event_text
- consequences
```

---

## 9. Technical Challenges & Solutions

| Challenge | Solution |
|------------|-----------|
| Latency | Cache TTS + use streaming Whisper for low delay. |
| STT accuracy | Confidence fallback → re-prompt user. |
| LLM cost | Cache parsed commands; use smaller local models for frequent actions. |
| Offline support | Store all JSON content locally and use embedded STT/TTS. |

---

## 10. Future Expansions
- Multiplayer voice sessions (“two explorers trapped together”).
- Daily procedural cabin stories.
- AR overlay (physical-world cabin mapping).
- Integration with smart speakers for ambient play.

---
