using System;
using System.Collections.Generic;

namespace Chip8
{
    public class Chip8
    {
        #region Opcodes
        private readonly Dictionary<byte, Action<Opcode>> opcodes;
        private readonly Dictionary<byte, Action<Opcode>> opcodesMisc;
        private readonly Dictionary<byte, Action<Opcode>> opcodesArithmetic;
        #endregion
        // Holds the current state of the emulator. Useful for save states!
        public EmulationState state;
        // The default fontset used by the Chip-8.
        private static readonly byte[] FontSet =
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
        

        // Determines whether the machine is powered or not.
        public bool Powered;

        // The interpreter mode. There are differences between the SCHIP mode and the COSMAC mode. All information regarding
        // these two different modes can be found here: https://github.com/mattmikolay/chip-8/wiki/CHIP%E2%80%908-Instruction-Set
        public Chip8InterpreterMode InterpreterMode;

        // Determines whether the screen should be redrawn or not. Usually only 00E0 and DXYN opcodes should set this to true.
        public bool Draw { get; private set; }

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
                { 0x65, LoadRegisters }
            };

            state = new EmulationState(interpreterMode);
        }

        /// <summary>
        ///     Loads a ROM into memory, then powers the virtual machine.
        /// </summary>
        /// <param name="romData">The ROM data stored as a byte array.</param>
        public void PowerAndLoadRom(byte[] romData, Chip8InterpreterMode interpreterMode = Chip8InterpreterMode.Schip)
        {
            Powered = false;
            InterpreterMode = interpreterMode;
            Power();
            // Load ROM into memory
            for (var i = 0; i < romData.Length; i++) state.Memory[0x200 + i] = romData[i];
            Powered = true;
        }

        /// <summary>
        ///     Sets up the virtual machine to ensure that it is in the correct operational state before running any program.
        /// </summary>
        private void Power()
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
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 64; x++)
                    state.Display[x, y] = 0;
            WriteFontset();

            // Reset timers
            state.Delay = state.Sound = 0;
        }

        private void WriteFontset()
        {
            for (var i = 0; i < 80; i++) state.Memory[i] = FontSet[i];
        }

        public void Cycle()
        {
            Draw = false;
            Opcode op = Fetch();
            state.PC += 2; // automatically increments the PC
            opcodes[(byte)(op.opcode >> 12)](op);
        }

        private Opcode Fetch()
        {
            return new Opcode((ushort)((state.Memory[state.PC] << 8) | state.Memory[state.PC + 1]));
        }

        public void DecrementTimers()
        {
            if(state.Sound > 0) state.Sound--;
            if(state.Delay > 0) state.Delay--;
        }
        #region Standard Opcodes
        private void InterpreterSystemCommands(Opcode data)
        {
            switch(data.NN)
            {
                case 0xE0:
                    for (var x = 0; x < 64; x++)
                        for (var y = 0; y < 32; y++)
                            state.Display[x, y] = 0;
                    Draw = true;
                    break;
                case 0xEE:
                    state.PC = state.Stack[state.SP--];
                    break;
                default:
                    // Just keep the program where it is at
                    state.PC -= 2;
                    break;
            }
        }

        private void Jump(Opcode data)
        {
            state.PC = data.NNN;
        }

        private void CallSubroutine(Opcode data)
        {
            state.Stack[++state.SP] = state.PC;
            state.PC = data.NNN;
        }

        private void SkipIfRegisterEqualsImmediate(Opcode data)
        {
            if(state.V[data.X] == data.NN)
                state.PC += 2;
        }
        private void SkipIfRegisterNotEqualsImmediate(Opcode data)
        {
            if(state.V[data.X] != data.NN)
                state.PC += 2;
        }
        private void SkipIfRegisterEqualsRegister(Opcode data)
        {
            if(state.V[data.X] == state.V[data.Y])
                state.PC += 2;
        }
        private void LoadImmediate(Opcode data)
        {
            state.V[data.X] = data.NN;
        }
        private void AddImmediate(Opcode data)
        {
            state.V[data.X] += data.NN;
        }
        private void SkipIfRegisterNotEqualsRegister(Opcode data)
        {
            if(state.V[data.X] != state.V[data.Y])
                state.PC += 2;
        }
        private void LoadIndexImmediate(Opcode data)
        {
            state.I = data.NNN;
        }
        private void JumpOffset(Opcode data)
        {
            state.PC = (ushort)((InterpreterMode == Chip8InterpreterMode.Schip) ? data.NNN + state.V[(data.NNN >> 8) & 0xF] : data.NNN + state.V[0]);
        }
        private void LoadRandom(Opcode data)
        {
            state.V[data.X] = (byte)(UnityEngine.Random.Range(0, 255) & data.NN);
        }
        private void DrawSprite(Opcode data)
        {
            // Oh boy.
            state.V[0xF] = 0;

            var posX = state.V[data.X];
            var posY = state.V[data.Y];

            for (var y = 0; y < data.N; y++)
            {
                byte pixel = state.Memory[state.I + y];
                for (var x = 0; x < 8; x++)
                {
                    if((pixel & (0x80 >> x)) != 0)
                    {
                        if (state.Display[(posX + x) % 64, (posY + y) % 32] == 1)
                            state.V[0xF] = 1;
                        state.Display[(posX + x) % 64, (posY + y) % 32] ^= 1;
                    }
                }
            }
            Draw = true;
        }
        private void SkipInput(Opcode data)
        {
            switch(data.NN)
            {
                case 0x9E:
                    if(state.Input[state.V[data.X]]) state.PC += 2;
                    break;
                case 0xA1:
                    if(!state.Input[state.V[data.X]]) state.PC += 2;
                    break;
                default:
                    throw new IllegalOpcodeException($"Illegal input opcode {data.opcode:X4}", data.opcode);
            }
        }
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

        private void MoveRegisters(Opcode data)
        {
            state.V[data.X] = state.V[data.Y];
        }
        private void OrRegisters(Opcode data)
        {
            state.V[data.X] |= state.V[data.Y];
            if(InterpreterMode == Chip8InterpreterMode.CosmacVIP)
                state.V[0xF] = 0;
        }
        private void AndRegisters(Opcode data)
        {
            state.V[data.X] &= state.V[data.Y];
            if(InterpreterMode == Chip8InterpreterMode.CosmacVIP)
                state.V[0xF] = 0;
        }
        private void XorRegisters(Opcode data)
        {
            state.V[data.X] ^= state.V[data.Y];
            if(InterpreterMode == Chip8InterpreterMode.CosmacVIP)
                state.V[0xF] = 0;
        }
        private void AddRegisters(Opcode data)
        {
            ushort temp = (ushort)(state.V[data.X] + state.V[data.Y]);
            state.V[0xF] = (byte)((temp > 255) ? 1 : 0);
            state.V[data.X] = (byte)((byte)temp & 0xFF);
        }
        private void SubtractRegisters(Opcode data)
        {
            state.V[0xF] = (byte)((state.V[data.X] < state.V[data.Y]) ? 0 : 1);
            state.V[data.X] = (byte)(state.V[data.X] - state.V[data.Y]);
        }
        private void ShiftRegistersRight(Opcode data)
        {
            if(InterpreterMode == Chip8InterpreterMode.Schip)
                data.Y = data.X;
            
            state.V[data.X] = (byte)(state.V[data.Y] >> 1);
            state.V[0xF] = (byte)(((state.V[data.Y] & 0x1) != 0) ? 1 : 0);
        }
        private void SubtractRegistersAlt(Opcode data)
        {
            state.V[0xF] = (byte)((state.V[data.X] > state.V[data.Y]) ? 0 : 1);
            state.V[data.X] = (byte)(state.V[data.Y] - state.V[data.X]);
        }
        private void ShiftRegistersLeft(Opcode data)
        {
            if(InterpreterMode == Chip8InterpreterMode.Schip)
                data.Y = data.X;
            
            state.V[data.X] = (byte)(state.V[data.Y] << 1);
            state.V[0xF] = (byte)((((state.V[data.Y] >> 7) & 0x1) != 0) ? 1 : 0);
        }
            
        #endregion

        #region Misc Opcodes
        private void LoadDelay(Opcode data)
        {
            state.V[data.X] = state.Delay;
        }

        private void WaitForKeypress(Opcode data)
        {
            state.PC -= 2;
            for (var i = 0; i < state.Input.Length; i++)
            {
                if(state.Input[i])
                {
                    state.V[data.X] = (byte)i;
                    state.PC += 2;
                    break;
                }
            }
        }

        private void SetDelay(Opcode data)
        {
            state.Delay = state.V[data.X];
        }

        private void SetSound(Opcode data)
        {
            state.Sound = state.V[data.X];
        }

        private void AddRegisterToIndex(Opcode data)
        {
            state.I += state.V[data.X];
        }

        private void LoadIndexFont(Opcode data)
        {
            state.I = (ushort)(5 * state.V[data.X]);
        }

        private void LoadIndexFontSuper(Opcode data)
        {
            // Not implemented
        }

        private void StoreBCD(Opcode data)
        {
            state.Memory[state.I] = (byte)(state.V[data.X] / 100 % 10);
            state.Memory[state.I + 1] = (byte)(state.V[data.X] / 10 % 10);
            state.Memory[state.I + 2] = (byte)(state.V[data.X]% 10);
        }

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
        /// Based on the CHIP-48 interpreter. Note that this does not make this compatible with SCHIP programs.
        /// </summary>
        Schip,
        /// <summary>
        /// Based on the Octo interpreter.
        /// </summary>
        Octo
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
        public ushort I, PC;
        public byte Delay, Sound, SP;
        public ushort[] Stack;
        public bool[] Input;
        public byte[,] Display;

        public EmulationState(Chip8InterpreterMode settings = Chip8InterpreterMode.Schip)
        {
            V = new byte[16];
            Memory = new byte[0x1000];
            I = PC = 0;
            Delay = Sound = SP = 0;
            Input = new bool[16];
            Display = new byte[64, 32];
            Stack = settings == Chip8InterpreterMode.CosmacVIP ? new ushort[12] : new ushort[16];
        }
    }

    /// <summary>
    /// A struct containing opcode data. Sends everything that an opcode should ever need to know.
    /// </summary>
    public struct Opcode
    {
        public ushort opcode;
        public ushort NNN;
        public byte NN, X, Y, N;

        public override string ToString() => $"{opcode:X4} (X: {X:X}, Y: {Y:X}, N: {N:X}, NN: {NN:X2}, NNN: {NNN:X3})";

        public Opcode(ushort opcode)
        {
            this.opcode = opcode;
            NNN = (ushort)(opcode & 0xFFF);
            NN = (byte)(opcode & 0xFF);
            X = (byte)((opcode >> 8) & 0xF);
            Y = (byte)((opcode >> 4) & 0xF);
            N = (byte)(opcode & 0xF);
        }
    }
}