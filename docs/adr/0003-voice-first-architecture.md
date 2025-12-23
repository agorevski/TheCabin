# ADR-0003: Voice-First Architecture with Fallback

## Status

Accepted

## Date

2025-12-23

## Context

The Cabin is designed as a voice-controlled text adventure. Players should be able to interact primarily through speech, but we also need to support text input for accessibility and situations where voice isn't practical (noisy environments, privacy concerns).

## Decision

We implement a voice-first architecture with the following components:
1. **Speech-to-Text (STT)**: Primary input via platform APIs or Whisper.cpp
2. **Command Parser**: GPT-based natural language understanding
3. **Text-to-Speech (TTS)**: Narrative output via MAUI TextToSpeech API
4. **Text Fallback**: Optional keyboard input for accessibility

## Consequences

### Positive

- Immersive, hands-free gameplay experience
- Natural language input feels intuitive
- Accessibility through multiple input methods
- Offline support possible with on-device models

### Negative

- Voice recognition latency affects gameplay flow
- API costs for cloud-based LLM parsing
- Background noise can affect recognition accuracy
- Privacy considerations for voice data

### Neutral

- Requires careful UX design for voice feedback
- Need confidence thresholds for re-prompting users
- TTS voice selection affects game atmosphere

## Alternatives Considered

1. **Text-only input** - Traditional but less immersive and not hands-free.
2. **Fixed command vocabulary** - Simpler but limits natural expression.
3. **Cloud-only voice** - Lower latency initially but no offline support.

## References

- [Voice Pipeline Documentation](../05-voice-pipeline.md)
- [The Cabin Game Design Document](../../The_Cabin_Game_Design_Doc.md)
