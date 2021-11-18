namespace Chip8
{
    /// <summary>
    /// A struct containing opcode data. Sends everything that an opcode should ever need to know.
    /// </summary>
    public struct Opcode
    {
        /// <summary>
        /// Full representation of the opcode as a ushort.
        /// </summary>
        public ushort opcode;
        /// <summary>
        /// Encoded NNN data for instructions that are encoded in XNNN fashion.
        /// </summary>
        public ushort NNN;
        /// <summary>
        /// Encodes NN/X/Y/N data for instructions in 0XYN or 01NN format.
        /// </summary>
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