# UCHIP - Unity-based CHIP-8 emulator

UCHIP is an implementation of the CHIP-8 interpreter originally made for the Cosmac VIP in 1977. This allows users to place CHIP-8 programs and games inside thier Unity programs.

## Current support

Currently, UCHIP supports CHIP-8 mainly. SCHIP v1.1 (CHIP-48) support is experimental. You can change between three interpreter modes:

`NOTE: This does not change what instructions the interpreter supports. It merely changes the behavior of some instructions to match that specific machine.`

- Cosmac VIP
- SCHIP v1.1 (HP-48)
- Octo

## Project goals

On occasion, updates are made to UCHIP to make it slightly better. It's not a high-maintenance program, so I tend to just leave it be. Future things I'd love to implement are:

- XO-CHIP (Octo program) support
- Better frontend
- Save states
- Debugging capabilities
- Mobile support
- Per-game customizable controls
- Performance improvements

## Credits & Sources

1. https://github.com/mattmikolay/chip-8 for a lot of documentation and references
2. http://devernay.free.fr/hacks/chip8/C8TECH10.HTM for reference
3. https://chip-8.github.io/ for documentation and resources on CHIP-8, SCHIP, and Octo
4. https://github.com/JohnEarnest/Octo for help with writing and understanding CHIP programs

