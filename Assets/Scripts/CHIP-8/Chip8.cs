using System;
using System.Collections.Generic;

namespace Chip8
{
    public class Chip8
    {
        #region Opcodes
        /// <summary>
        /// Maps most main opcodes to a dictionary for quick lookup and execution.
        /// </summary>
        private readonly Dictionary<byte, Action<Opcode>> opcodes;
        /// <summary>
        /// Maps miscellaneous opcodes (0xFXXX) to a dictionary for quick lookup and execution.
        /// </summary>
        private readonly Dictionary<byte, Action<Opcode>> opcodesMisc;
        /// <summary>
        /// Maps arithmetic opcodes (0x8XXX) to a dictionary for quick lookup and execution.
        /// </summary>
        private readonly Dictionary<byte, Action<Opcode>> opcodesArithmetic;
        #endregion
        /// <summary>
        /// Contains the current state of the interpreter. You can use this for save states.
        /// </summary>
        public EmulationState state;
        /// <summary>
        /// Data for the fontset used by the interpreter.
        /// </summary>
        private static readonly byte[] LoresFontSet =
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, //0
            0x20, 0x60, 0x20, 0x20, 0x70, //1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, //2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, //3
            0x90, 0x90, 0xF0, 0x10, 0x10, //4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, //5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, //6
            0xF0, 0x10, 0x20, 0x40, 0x40, //7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, //8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, //9
            0xF0, 0x90, 0xF0, 0x90, 0x90, //A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, //B
            0xF0, 0x80, 0x80, 0x80, 0xF0, //C
            0xE0, 0x90, 0x90, 0x90, 0xE0, //D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, //E
            0xF0, 0x80, 0xF0, 0x80, 0x80 //F
        };
        /// <summary>
        /// The offset to store the lores fontset in interpreter memory.
        /// </summary>
        private ushort loresFontOffset = 0;
        /// <summary>
        /// Determines whether the interpreter is processing commands or not.
        /// </summary>
        public bool Powered;
        /// <summary>
        /// The interpreter mode. There are various differences between the different available interpreter modes, each with their own unique quirks.
        /// For more information, please visit https://chip-8.github.io/extensions/
        /// </summary>
        public Chip8InterpreterMode InterpreterMode;
        /// <summary>
        /// Flag to signal when a change to the display has been made.
        /// </summary>
        /// <remarks>
        /// In your own renderer, this can be used for a <b>massive</b> performance boost.
        /// </remarks>
        public bool Draw { get; private set; }
        /// <summary>
        /// RNG for the random number opcode
        /// </summary>
        /// <returns></returns>
        private readonly Random random = new();
        
        /// <summary>
        /// Buffer for storing random bytes - don't know why I need to store this in a 
        /// </summary>
        private byte[] buffer = new byte[1];
        /// <summary>
        /// Creates a new instance of a CHIP-8 interpreter. You must power the system before you can start running a ROM.
        /// </summary>
        /// <param name="interpreterMode">The interpreter mode to use for this specific interpreter.</param>
        public Chip8(Chip8InterpreterMode interpreterMode = Chip8InterpreterMode.Schip)
        {
            InterpreterMode = interpreterMode;
            opcodes = new Dictionary<byte, Action<Opcode>>
            {
                { 0x0, InterpreterSystemCommands },
                { 0x1, Jump },
                { 0x2, CallSubroutine },
                { 0x3, SkipIfRegisterEqualsImmediate },
                { 0x4, SkipIfRegisterNotEqualsImmediate },
                { 0x5, SkipIfRegisterEqualsRegister },
                { 0x6, LoadImmediate },
                { 0x7, AddImmediate },
                { 0x8, Arithmetic },
                { 0x9, SkipIfRegisterNotEqualsRegister },
                { 0xA, LoadIndexImmediate },
                { 0xB, JumpOffset },
                { 0xC, LoadRandom },
                { 0xD, DrawSprite },
                { 0xE, SkipInput },
                { 0xF, Misc },
            };

            opcodesArithmetic = new Dictionary<byte, Action<Opcode>>
            {
                { 0x0, MoveRegisters },
                { 0x1, OrRegisters },
                { 0x2, AndRegisters },
                { 0x3, XorRegisters },
                { 0x4, AddRegisters },
                { 0x5, SubtractRegisters },
                { 0x6, ShiftRegistersRight },
                { 0x7, SubtractRegistersAlt },
                { 0xE, ShiftRegistersLeft }
            };

            opcodesMisc = new Dictionary<byte, Action<Opcode>>
            {
                { 0x07, LoadDelay },
                { 0x0A, WaitForKeypress },
                { 0x15, SetDelay },
                { 0x18, SetSound },
                { 0x1E, AddRegisterToIndex },
                { 0x29, LoadIndexFont },
                { 0x30, LoadIndexFontSuper },
                { 0x33, StoreBCD },
                { 0x55, StoreRegisters },
                { 0x65, LoadRegisters },
                { 0x75, StoreRegistersRPL },
                { 0x85, ReadRegistersRPL }
            };

            state = new EmulationState(interpreterMode);
        }

        /// <summary>
        /// Loads a ROM into memory, then powers the interpreter.
        /// </summary>
        /// <param name="romData">The ROM data stored as a byte array.</param>
        public void PowerAndLoadRom(byte[] romData, Chip8InterpreterMode interpreterMode = Chip8InterpreterMode.Schip)
        {
            Powered = false;
            InterpreterMode = interpreterMode;
            Reset();
            // Load ROM into memory
            for (var i = 0; i < romData.Length; i++) state.Memory[0x200 + i] = romData[i];
            Powered = true;
        }

        /// <summary>
        /// Resets the interpreter to a known state.
        /// </summary>
        private void Reset()
        {
            // Reset pc, opcode reading, I, and sp
            state.PC = 0x200;
            state.I = state.SP = 0;

            // Clear memory, display, stack, and registers
            for (var i = 0; i < 0x1000; i++)
            {
                state.Memory[i] = 0;
                if (i < 0x10)
                {
                    state.Stack[i] = 0;
                    state.V[i] = 0;
                    state.Input[i] = false;
                }
            }

            // Clear display
            //for (var y = 0; y < 32; y++)
            //    for (var x = 0; x < 64; x++)
            //        state.Display[x, y] = 0;

            state.Display.Clear();
            WriteFontset();

            // Reset timers
            state.Delay = state.Sound = 0;
        }

        /// <summary>
        /// Seperate function to write the fontset to memory.
        /// </summary>
        private void WriteFontset(ushort offset = 0)
        {
            loresFontOffset = offset;
            for (var i = 0; i < 80; i++) state.Memory[i + offset] = LoresFontSet[i];
        }

        /// <summary>
        /// Emulates a cycle in the interpreter.
        /// </summary>
        public void Cycle()
        {
            Draw = false;
            Opcode op = Fetch();
            state.PC += 2; // automatically increments the PC
            opcodes[(byte)(op.opcode >> 12)](op);
        }

        /// <summary>
        /// Fetches an opcode from memory.
        /// </summary>
        /// <returns>The next opcode in memory.</returns>
        private Opcode Fetch()
        {
            return new Opcode((ushort)((state.Memory[state.PC] << 8) | state.Memory[state.PC + 1]));
        }

        /// <summary>
        /// Decrements both the sound and delay timers, if able.
        /// </summary>
        public void DecrementTimers()
        {
            if (state.Sound > 0) state.Sound--;
            if (state.Delay > 0) state.Delay--;
        }
        #region Standard Opcodes
        /// <summary>
        /// Various interpreter system commands. This includes screen clear (00E0) and return from subroutine (00EE).
        /// </summary>
        /// <param name="data">The opcode.</param>
        private void InterpreterSystemCommands(Opcode data)
        {
            if(data.Y == 0xC)
            {
                state.Display.ScrollVertical(data.N, false);
                Draw = true;
                return;
            }
            switch (data.NN)
            {
                case 0xE0:
                    state.Display.Clear();
                    Draw = true;
                    break;
                case 0xEE:
                    state.PC = state.Stack[state.SP--];
                    break;
                case 0xFB:
                    // Scroll 4 pixels right (2 in low res mode)
                    state.Display.ScrollHorizontal();
                    Draw = true;
                    break;
                case 0xFC:
                    // Scroll 4 pixels left (2 in low res mode)
                    state.Display.ScrollHorizontal(false);
                    Draw = true;
                    break;
                case 0xFD:
                    // Exit the interpreter
                    break;
                case 0xFE:
                    // Disable hires mode
                    state.Display.SetHires(false);
                    break;
                case 0xFF:
                    // Enable hires mode
                    state.Display.SetHires(true);
                    break;
                default:
                    // Just keep the program where it is at
                    state.PC -= 2;
                    break;
            }
        }

        /// <summary>
        /// Jumps to the specified address in memory.
        /// </summary>
        /// <param name="data">The opcode. Uses the last three nibbles to determine the jump address.</param>
        private void Jump(Opcode data)
        {
            state.PC = data.NNN;
        }

        /// <summary>
        /// Pushes the current PC to the stack, then jumps to the specified address in memory.
        /// </summary>
        /// <param name="data">The opcode. Uses the last three nibbles to determine the jump address.</param>
        private void CallSubroutine(Opcode data)
        {
            state.Stack[++state.SP] = state.PC;
            state.PC = data.NNN;
        }
        /// <summary>
        /// Skips the next instruction if the specified register equals the immediate value.
        /// </summary>
        /// <param name="data">The opcode. Uses the second nibble for determining the register, and the second byte for the immediate value.</param>
        private void SkipIfRegisterEqualsImmediate(Opcode data)
        {
            if (state.V[data.X] == data.NN)
                state.PC += 2;
        }
        /// <summary>
        /// Skips the next instruction if the specified register does not equal the immediate value.
        /// </summary>
        /// <param name="data">The opcode. Uses the second nibble for determining the register, and the second byte for the immediate value.</param>
        private void SkipIfRegisterNotEqualsImmediate(Opcode data)
        {
            if (state.V[data.X] != data.NN)
                state.PC += 2;
        }
        /// <summary>
        /// Skips the next instruction if the specified registers are equal.
        /// </summary>
        /// <param name="data">The opcode. Uses the second and third nibbles for determining which registers to compare.</param>
        private void SkipIfRegisterEqualsRegister(Opcode data)
        {
            if (state.V[data.X] == state.V[data.Y])
                state.PC += 2;
        }
        /// <summary>
        /// Loads an immediate value into a register.
        /// </summary>
        /// <param name="data">The opcode. Uses the second nibble for determining the register, and the second byte for the immediate value.</param>
        private void LoadImmediate(Opcode data)
        {
            state.V[data.X] = data.NN;
        }
        /// <summary>
        /// Adds an immediate value to a register's curent value.
        /// </summary>
        /// <param name="data">The opcode. Uses the second nibble for determining the register, and the second byte for the immediate value.</param>
        /// <remarks>The flag register is not modified by this operation.</remarks>
        private void AddImmediate(Opcode data)
        {
            state.V[data.X] += data.NN;
        }
        /// <summary>
        /// Skips the next instruction if the specified registers are not equal.
        /// </summary>
        /// <param name="data">The opcode. Uses the second and third nibbles for determining which registers to compare.</param>
        private void SkipIfRegisterNotEqualsRegister(Opcode data)
        {
            if (state.V[data.X] != state.V[data.Y])
                state.PC += 2;
        }
        /// <summary>
        /// Loads an immediate value into the index register.
        /// </summary>
        /// <param name="data">The opcode. Used for determining the address.</param>
        private void LoadIndexImmediate(Opcode data)
        {
            state.I = data.NNN;
        }
        /// <summary>
        /// Jumps to an address offset by the amount specified in a register.
        /// </summary>
        /// <param name="data">The opcode. Used to determine the address.</param>
        /// <remarks> In SCHIP mode, the register used is determined by the first nibble. In Cosmac VIP and Octo modes, register 0 is ued.</remarks>
        private void JumpOffset(Opcode data)
        {
            state.PC = (ushort)((InterpreterMode == Chip8InterpreterMode.Schip) ? data.NNN + state.V[(data.NNN >> 8) & 0xF] : data.NNN + state.V[0]);
        }
        /// <summary>
        /// Loads a random value into a register with mask NN.
        /// </summary>
        /// <param name="data">The opcode. Uses the second nibble to determine register and the second byte as the mask.</param>
        private void LoadRandom(Opcode data)
        {
            random.NextBytes(buffer);
            state.V[data.X] = (byte)(buffer[0] & data.NN);
        }
        /// <summary>
        /// Draws a sprite to the display using XOR. If any pixels are turned off, register F is set to 1. Otherwise, it is set to 0.
        /// </summary>
        /// <param name="data">The opcode. Used to determine the registers that determine the sprite position, as well as how many lines of a sprite to draw.</param>
        /// <remarks> The sprite is loaded from the index register. </remarks>
        private void DrawSprite(Opcode data)
        {
            // Oh boy.

            // New SCHIP compliant code.
            var posX = state.V[data.X];
            var posY = state.V[data.Y];
            bool setFlags;

            if (data.N > 0 || !state.Display.hiresMode)
            {
                var sprite = state.Memory[state.I..(state.I + data.N)];
                setFlags = state.Display.DrawSpriteLores(posX, posY, data.N, sprite);
            }
            else
            {
                var sprData = state.Memory[state.I..(state.I + 32)];
                ushort[] sprite = new ushort[16];

                for(var i = 0; i < 32; i += 2)
                {
                    ushort val = (ushort)(sprData[i] << 8 | sprData[i + 1]);
                    sprite[i / 2] = val;
                }

                setFlags = state.Display.DrawSprite(posX, posY, sprite);
            }

            state.V[0xF] = (byte)(setFlags ? 1 : 0);
            Draw = true;
        }
        /// <summary>
        /// Handles skipping code based on input. Handles if input is pressed (EX9E) or not pressed (EXA1).
        /// </summary>
        /// <param name="data">The opcode. Used for determining what register determines what key to check, and the instruction to execute.</param>
        /// <exception cref="IllegalOpcodeException">Thrown when an illegal opcode is found.</exception>
        private void SkipInput(Opcode data)
        {
            switch (data.NN)
            {
                case 0x9E:
                    if (state.Input[state.V[data.X]]) state.PC += 2;
                    break;
                case 0xA1:
                    if (!state.Input[state.V[data.X]]) state.PC += 2;
                    break;
                default:
                    throw new IllegalOpcodeException($"Illegal input opcode {data.opcode:X4}", data.opcode);
            }
        }
        /// <summary>
        /// Helper function for execution of arithmetic instructions.
        /// </summary>
        /// <param name="data">The opcode.</param>
        /// <exception cref="IllegalOpcodeException">Thrown when an illegal opcode is found.</exception>
        private void Arithmetic(Opcode data)
        {
            try
            {
                opcodesArithmetic[data.N](data);
            }
            catch (KeyNotFoundException)
            {
                // there's nothing to do here except throw another exception
                throw new IllegalOpcodeException($"Illegal arithmetic opcode {data.opcode:X4}", data.opcode);
            }
        }
        /// <summary>
        /// Helper function for execution of misceleanous instructions.
        /// </summary>
        /// <param name="data">The opcode.</param>
        /// <exception cref="IllegalOpcodeException">Thrown when an illegal opcode is found.</exception>
        private void Misc(Opcode data)
        {
            try
            {
                opcodesMisc[data.NN](data);
            }
            catch (KeyNotFoundException)
            {
                // there's nothing to do here except throw another exception
                throw new IllegalOpcodeException($"Illegal misc opcode {data.opcode:X4}", data.opcode);
            }
        }
        #endregion

        #region Arithmetic Opcodes
        /// <summary>
        /// Performs the operation r[X] = r[Y].
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        private void MoveRegisters(Opcode data)
        {
            state.V[data.X] = state.V[data.Y];
        }
        /// <summary>
        /// Performs the operation r[X] |= r[Y].
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        private void OrRegisters(Opcode data)
        {
            state.V[data.X] |= state.V[data.Y];
            if (InterpreterMode == Chip8InterpreterMode.CosmacVIP)
                state.V[0xF] = 0;
        }
        /// <summary>
        /// Performs the operation r[X] &= r[Y].
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        private void AndRegisters(Opcode data)
        {
            state.V[data.X] &= state.V[data.Y];
            if (InterpreterMode == Chip8InterpreterMode.CosmacVIP)
                state.V[0xF] = 0;
        }
        /// <summary>
        /// Performs the operation r[X] ^= r[Y].
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        private void XorRegisters(Opcode data)
        {
            state.V[data.X] ^= state.V[data.Y];
            if (InterpreterMode == Chip8InterpreterMode.CosmacVIP)
                state.V[0xF] = 0;
        }
        /// <summary>
        /// Performs the operation r[X] += r[Y]. Register F is set to the carry bit (1 for yes, 0 for no).
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        private void AddRegisters(Opcode data)
        {
            ushort temp = (ushort)(state.V[data.X] + state.V[data.Y]);
            state.V[0xF] = (byte)((temp > 255) ? 1 : 0);
            state.V[data.X] = (byte)((byte)temp & 0xFF);
        }
        /// <summary>
        /// Performs the operation r[X] -= r[Y]. Register F is set to the borrow bit (1 for no, 0 for yes).
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        private void SubtractRegisters(Opcode data)
        {
            state.V[0xF] = (byte)((state.V[data.X] < state.V[data.Y]) ? 0 : 1);
            state.V[data.X] = (byte)(state.V[data.X] - state.V[data.Y]);
        }
        /// <summary>
        /// Performs a right shift on the specified register and stores the result in register X. Register F stores the shifted bit.
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        /// <remarks> In SCHIP mode, the shift is performed in place on register X. Otherwise, the shift is performed on register Y and stored in register X. </remarks>
        private void ShiftRegistersRight(Opcode data)
        {
            if (InterpreterMode == Chip8InterpreterMode.Schip)
                data.Y = data.X;

            state.V[data.X] = (byte)(state.V[data.Y] >> 1);
            state.V[0xF] = (byte)(((state.V[data.Y] & 0x1) != 0) ? 1 : 0);
        }
        /// <summary>
        /// Performs the operation r[X] = r[Y] - r[X]. Register F is set to the borrow bit (1 for no, 0 for yes).
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        private void SubtractRegistersAlt(Opcode data)
        {
            state.V[0xF] = (byte)((state.V[data.X] > state.V[data.Y]) ? 0 : 1);
            state.V[data.X] = (byte)(state.V[data.Y] - state.V[data.X]);
        }
        /// <summary>
        /// Performs a left shift on the specified register and stores the result in register X. Register F stores the shifted bit.
        /// </summary>
        /// <param name="data">The opcode. Used for determining registers X and Y.</param>
        /// <remarks> In SCHIP mode, the shift is performed in place on register X. Otherwise, the shift is performed on register Y and stored in register X. </remarks>
        private void ShiftRegistersLeft(Opcode data)
        {
            if (InterpreterMode == Chip8InterpreterMode.Schip)
                data.Y = data.X;

            state.V[data.X] = (byte)(state.V[data.Y] << 1);
            state.V[0xF] = (byte)((((state.V[data.Y] >> 7) & 0x1) != 0) ? 1 : 0);
        }

        #endregion

        #region Misc Opcodes
        /// <summary>
        /// Loads the delay timer into the specified register.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the loading register.</param>
        private void LoadDelay(Opcode data)
        {
            state.V[data.X] = state.Delay;
        }
        /// <summary>
        /// Waits for any key to be pressed. On completion, stores the resulting key in the specified register.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the loading register.</param>
        private void WaitForKeypress(Opcode data)
        {
            state.PC -= 2;
            for (var i = 0; i < state.Input.Length; i++)
            {
                if (state.Input[i])
                {
                    state.V[data.X] = (byte)i;
                    state.PC += 2;
                    break;
                }
            }
        }
        /// <summary>
        /// Sets the delay timer to the value in the specified register.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        private void SetDelay(Opcode data)
        {
            state.Delay = state.V[data.X];
        }
        /// <summary>
        /// Sets the sound timer to the value in the specified register.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        private void SetSound(Opcode data)
        {
            state.Sound = state.V[data.X];
        }
        /// <summary>
        /// Adds the value of the specified register to the index register.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        private void AddRegisterToIndex(Opcode data)
        {
            state.I += state.V[data.X];
        }
        /// <summary>
        /// Sets the index register to the font character specified by the specified register.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        /// <remarks>This only handles lores fonts.
        private void LoadIndexFont(Opcode data)
        {
            state.I = (ushort)(5 * state.V[data.X]);
        }
        /// <summary>
        /// Sets the index register to the SCHIP font character specified by the specified register.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        /// <remarks>This only handles hires fonts.</remarks>
        private void LoadIndexFontSuper(Opcode data)
        {
            // Not implemented
        }
        /// <summary>
        /// Stores the binary-coded decimal representation of the value in the specified register to the current address of the index register.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        private void StoreBCD(Opcode data)
        {
            state.Memory[state.I] = (byte)(state.V[data.X] / 100 % 10);
            state.Memory[state.I + 1] = (byte)(state.V[data.X] / 10 % 10);
            state.Memory[state.I + 2] = (byte)(state.V[data.X] % 10);
        }
        /// <summary>
        /// Stores registers 0 to X (specified) at the current address of I.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        /// <remarks> In Cosmac VIP and Octo (XO-CHIP) modes, the index register is modified. SCHIP mode leaves the index register as is. </remarks>
        private void StoreRegisters(Opcode data)
        {
            for (var i = 0; i <= data.X; i++)
            {
                state.Memory[i + state.I] = state.V[i];
            }
            if (InterpreterMode != Chip8InterpreterMode.Schip)
            {
                state.I += (ushort)(data.X + 1);
            }
        }
        /// <summary>
        /// Loads registers 0 to X (specified) from the current address of I.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        /// <remarks> In Cosmac VIP and Octo (XO-CHIP) modes, the index register is modified. SCHIP mode leaves the index register as is. </remarks>
        private void LoadRegisters(Opcode data)
        {
            for (var i = 0; i <= data.X; i++)
            {
                state.V[i] = state.Memory[i + state.I];
            }
            if (InterpreterMode != Chip8InterpreterMode.Schip)
            {
                state.I += (ushort)(data.X + 1);
            }
        }

        /// <summary>
        /// Stores registers 0 to X in RPL user flags.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        /// <remarks>CHIP-48 only supports RPL going up to 7.</remarks>
        private void StoreRegistersRPL(Opcode data)
        {
            var x = Math.Max(data.X, (byte)0x7);

            for (var i = 0; i < x; i++)
            {
                state.RPL[i] = state.V[i];
            }
        }

        /// <summary>
        /// Reads registers 0 to X from RPL user flags.
        /// </summary>
        /// <param name="data">The opcode. Used to specify the register.</param>
        /// <remarks>CHIP-48 only supports RPL going up to 7.</remarks>
        private void ReadRegistersRPL(Opcode data)
        {
            var x = Math.Max(data.X, (byte)0x7);

            for (var i = 0; i < x; i++)
            {
                state.V[i] = state.RPL[i];
            }
        }

        #endregion
    }

    /// <summary>
    /// The interpretation mode that the interpretor will use.
    /// </summary>
    public enum Chip8InterpreterMode
    {
        /// <summary>
        /// The original COSMAC VIP interpretation style.
        /// </summary>
        CosmacVIP,
        /// <summary>
        /// Based on the CHIP-48 interpreter.
        /// </summary>
        Schip,
        /// <summary>
        /// Based on the Octo (XO-CHIP) interpreter.
        /// </summary>
        XoChip
    }

    public class IllegalOpcodeException : Exception
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public ushort Opcode { get; }

        public IllegalOpcodeException()
        {
        }

        public IllegalOpcodeException(string message) : base(message)
        {
        }

        public IllegalOpcodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public IllegalOpcodeException(string message, ushort opcode) : base(message)
        {
            Opcode = opcode;
        }
    }

    /// <summary>
    /// EmulationState contains a state of the emulator. This can be used in order to save and load system states.
    /// </summary>
    public struct EmulationState
    {
        public byte[] V;
        public byte[] Memory;
        public byte[] RPL;
        public ushort I, PC;
        public byte Delay, Sound, SP;
        public ushort[] Stack;
        public bool[] Input;
        public Chip8Display Display;

        public EmulationState(Chip8InterpreterMode settings = Chip8InterpreterMode.Schip)
        {
            V = new byte[16];
            RPL = new byte[8];
            Memory = new byte[0x1000];
            I = PC = 0;
            Delay = Sound = SP = 0;
            Input = new bool[16];
            Display = new Chip8Display(false);
            Stack = settings == Chip8InterpreterMode.CosmacVIP ? new ushort[12] : new ushort[16];
        }
    }
}