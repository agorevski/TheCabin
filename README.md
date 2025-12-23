# 🎮 The Cabin

A voice-controlled interactive fiction game built with .NET MAUI for Android and Windows. Explore mysterious environments by speaking commands in this immersive narrative-driven adventure that combines classic text adventure gameplay with modern AI-powered natural language processing.

[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-9.0-512BD4?logo=.net)](https://dotnet.microsoft.com/apps/maui)
[![Android](https://img.shields.io/badge/Android-6.0%2B-3DDC84?logo=android)](https://www.android.com/)
[![Windows](https://img.shields.io/badge/Windows-10%2B-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## ✨ Features

- 🎙️ **Voice-First Gameplay** - Speak commands naturally instead of typing
- 🤖 **AI-Powered Parsing** - LLM-based natural language understanding for flexible command interpretation
- 📖 **Multiple Story Packs** - 5+ immersive thematic adventures (horror, sci-fi, fantasy, mystery, survival)
- 🔊 **Text-to-Speech Narration** - Optional voice narration for complete hands-free experience
- 💾 **Save/Load System** - Multiple save slots and auto-save functionality
- 🏆 **Achievements & Puzzles** - Engaging challenge system with tracked progress
- 📴 **Offline Support** - Core gameplay works without internet connection

## 🎯 Game Themes

- **Classic Horror** - Haunted log cabin with decaying interiors and supernatural mysteries
- **Arctic Survival** - Polar outpost with dwindling power and freezing tension
- **Fantasy Magic** - Wizard's workshop with glowing runes and spellcraft puzzles
- **Sci-Fi Isolation** - Derelict space module with system malfunctions and alien dread
- **Cozy Mystery** - Snowbound lodge with warm atmosphere and hidden secrets

## 🏗️ Architecture

The project follows a clean architecture pattern with clear separation of concerns:

```text
┌─────────────────────────────────────────────────┐
│  Presentation (MAUI)  - UI, ViewModels, Views   │
├─────────────────────────────────────────────────┤
│  Application          - Services, Orchestration │
├─────────────────────────────────────────────────┤
│  Domain (Core)        - Game Engine, Models     │
├─────────────────────────────────────────────────┤
│  Infrastructure       - Data Access, External   │
└─────────────────────────────────────────────────┘
```

### Key Components

- **Voice Pipeline**: Speech-to-text → LLM parsing → Command execution
- **Game Engine**: State machine managing rooms, inventory, and puzzles
- **Story Pack System**: JSON-based content with extensible theme support
- **Persistence Layer**: SQLite for game saves and command caching

## 📁 Project Structure

```text
TheCabin/
├── src/
│   ├── TheCabin.Maui/           # MAUI app (Android & Windows)
│   ├── TheCabin.Core/           # Game engine and domain logic
│   ├── TheCabin.Infrastructure/ # Data access and external services
│   └── TheCabin.Console/        # Console test harness
├── tests/
│   ├── TheCabin.Core.Tests/
│   └── TheCabin.Infrastructure.Tests/
├── story_packs/                 # JSON theme definitions
├── docs/                        # Comprehensive documentation
│   ├── 01-project-overview.md
│   ├── 02-system-architecture.md
│   ├── 03-technical-stack.md
│   └── ... (detailed design docs)
└── README.md
```

## 🚀 Quick Start

### Prerequisites

#### For Both Platforms

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or higher
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.8+) with MAUI workload

#### For Android Testing

- Android SDK (API Level 23-34)
- Android device or emulator

#### For Windows Testing

- Windows 10 (version 1809 / build 17763) or higher
- Windows 10 SDK (installed with Visual Studio MAUI workload)

### Build and Run

```bash
# Clone the repository
git clone https://github.com/agorevski/TheCabin.git
cd TheCabin

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

#### Run on Windows

**From Visual Studio (Recommended):**

1. Set `TheCabin.Maui` as startup project
2. Select **Windows Machine** from the debug target dropdown
3. Press F5 or click Start

**From Command Line (for debugging):**

```bash
# Build the Windows app
cd src/TheCabin.Maui
dotnet build -f net9.0-windows10.0.19041.0

# Run with console output to see errors
cd bin\Debug\net9.0-windows10.0.19041.0\win10-x64
cmd /c "TheCabin.Maui.exe & pause"
```

**Troubleshooting:** If the app crashes immediately:

1. **Use Visual Studio** - It's the recommended way to run and debug Windows MAUI apps
2. Check for missing resources - Story pack JSON files must be in the output directory
3. View crash logs in Windows Event Viewer (Application logs)
4. Run from Visual Studio debugger to see exception details (F5)

**Note:** Windows MAUI apps require the Windows App SDK runtime. Visual Studio handles all dependencies and provides debugging capabilities automatically.

#### Run on Android

```bash
# Build and run on Android (requires connected device/emulator)
cd src/TheCabin.Maui
dotnet build -t:Run -f net9.0-android

# Look at device logs while running
cd C:\android-sdk\platform-tools
adb logcat -c && adb logcat | findstr "com.thecabin.voiceadventure"
```

### Alternative: Use PowerShell Script

```powershell
# Build and run tests
.\build-and-test.ps1
```

## 🛠️ Technology Stack

| Component | Technology |
|-----------|-----------|
| **Framework** | .NET MAUI 9.0 |
| **Language** | C# 12 |
| **Platforms** | Android 6.0+, Windows 10+ |
| **UI Pattern** | MVVM (CommunityToolkit.Mvvm) |
| **Speech Recognition** | Platform-specific APIs (Android) |
| **Text-to-Speech** | MAUI TextToSpeech / Azure TTS |
| **LLM Integration** | OpenAI GPT-4o-mini / Azure OpenAI |
| **Database** | SQLite (sqlite-net-pcl) |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection |

## 📚 Documentation

Comprehensive documentation is available in the [`docs/`](docs/) directory:

- [Project Overview](docs/01-project-overview.md) - Goals, scope, and success metrics
- [System Architecture](docs/02-system-architecture.md) - Technical architecture and patterns
- [Technical Stack](docs/03-technical-stack.md) - Detailed technology choices
- [Data Models](docs/04-data-models.md) - Core data structures
- [Voice Pipeline](docs/05-voice-pipeline.md) - Speech recognition and TTS
- [Game Engine](docs/06-game-engine.md) - State machine and logic
- [UI/UX Design](docs/07-ui-ux-design.md) - Interface patterns
- [MAUI Implementation](docs/08-maui-implementation.md) - Platform-specific details
- [Content Management](docs/09-content-management.md) - Story pack system
- [Deployment Strategy](docs/10-deployment-strategy.md) - Release process
- [Future Roadmap](docs/11-future-roadmap.md) - Planned features

### Phase Summaries

Development progress is tracked in phase summary documents (`PHASE_*_SUMMARY.md`), detailing completed milestones and implementation details.

## 🎮 How to Play

### On Android

1. **Launch the app** and grant microphone permissions when prompted
2. **Select a story pack** from the available themes
3. **Tap the microphone button** to activate voice input
4. **Speak commands** naturally (e.g., "Look around", "Pick up the lantern", "Go north")
5. **Listen or read** the narration describing what happens
6. **Solve puzzles** and explore to uncover the story

### On Windows

1. **Launch the app** from Visual Studio or the built executable
2. **Select a story pack** from the available themes
3. **Type commands** in the command entry field (voice input coming in future updates)
4. **Press Enter** or click the submit button to execute commands
5. **Read** the narration describing what happens (text-to-speech available)
6. **Solve puzzles** and explore to uncover the story

### Common Commands

- "Look around" - Examine your current location
- "Pick up [item]" - Add an item to your inventory
- "Go [direction]" - Move to another room (north, south, east, west)
- "Use [item] on [target]" - Interact with objects
- "Check inventory" - View items you're carrying
- "Help" - Get available commands

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/TheCabin.Core.Tests/

# Run with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"
```

## 🗺️ Development Status

**Current Phase**: Phase 17E (Achievements & Advanced Features)

### Completed Features ✅

- ✅ Core game engine with state machine
- ✅ Command routing and parsing
- ✅ Inventory management system
- ✅ Puzzle engine with multi-step support
- ✅ Story pack loading and management
- ✅ Game save/load functionality
- ✅ Achievement system
- ✅ MAUI UI implementation
- ✅ Text-to-speech integration
- ✅ Multiple thematic story packs

### In Progress 🚧

- Voice recognition integration
- LLM command parser implementation
- UI polish and optimization

### Planned 📋

- Enhanced voice controls
- Additional story packs
- Cloud save synchronization
- iOS version (via MAUI)
- Accessibility features

## 🤝 Contributing

Contributions are welcome! Please see our [Contributing Guide](CONTRIBUTING.md) for detailed information on:

- Development environment setup
- Coding standards and conventions
- Pull request process
- Issue reporting guidelines

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👏 Acknowledgments

- Inspired by classic text adventures like Zork and Colossal Cave Adventure
- Built with [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
- AI integration powered by [OpenAI](https://openai.com/)
- Community contributions and feedback

## 📞 Contact

- **Project Repository**: [github.com/agorevski/TheCabin](https://github.com/agorevski/TheCabin)
- **Issues**: [GitHub Issues](https://github.com/agorevski/TheCabin/issues)
- **Documentation**: [docs/](docs/)

---

## Made with ❤️ using .NET MAUI**
