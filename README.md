# UCHIP - Unity-based CHIP-8 emulator

UCHIP is an implementation of the CHIP-8 interpreter in the Unity game engine. The code is structured so that this can be used as a general reference C# CHIP-8 implementation as well.

# Implementation

UCHIP is implemented in two classes.

## Chip8.cs

Chip8.cs is the main class, and houses all of the emulation code for the CHIP-8. ROM loading, input handling, drawing, and sound are not directly handled here.

## Chip8MonoBehaviour.cs

Chip8MonoBehaviour.cs houses all of the Unity-specific implementations of the CHIP-8 code, and allows for a few features including custom colors and support for Unity's new input system.

### Additional goals
- Implement CHIP-48 / XO-CHIP support
- Refactor `Chip8MonoBehaviour.cs` to streamline emulation process
- Ensure that `Chip8.cs` is Unity-agnostic so that it can be used in non-Unity projects
- Potentially split up `Chip8.cs` for better readability.

# Credits & Sources

1. https://github.com/mattmikolay/chip-8 for a lot of documentation and references
2. http://devernay.free.fr/hacks/chip8/C8TECH10.HTM for reference
