using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Chip8
{
    public class Chip8MonoBehaviour : MonoBehaviour
    {
        private Chip8 _emulator;
        [Header("Emulator")] [SerializeField] private string romPath;
        [SerializeField] private Chip8InterpreterMode interpreterMode;
        [Header("Display")] [SerializeField] private Color backgroundColor = Color.black;
        [SerializeField] private Color foregroundColor = Color.white;
        [SerializeField] private RawImage display;
        private ulong _stepCount;
        public string disassemblyContents;
        private byte[] _romData;

        #region Functionality

        private void Start()
        {
            var rom = Resources.Load<TextAsset>(romPath);
            _emulator = new Chip8();
            if (rom == null)
            {
                Debug.LogError("Error: Could not find rom. Whoops!");
                Destroy(this);
                return;
            }

            _romData = rom.bytes;
            // DisassembleROM();

            _emulator.PowerAndLoadRom(_romData, interpreterMode);
        }

        /*private void DisassembleROM()
        {
            int romMemoryLocation = 0x200;
            for (int i = 0; i < romData.Length; i += 2)
            {
                disassemblyContents += (romMemoryLocation + i).ToString("X4") + ": ";
                if(i + 1 < romData.Length)
                    disassemblyContents += romData[i].ToString("X2") + " " + romData[i + 1].ToString("X2");
                else
                {
                    disassemblyContents += romData[i].ToString("X2");
                }
    
                disassemblyContents += "\n";
    
            }
            disassembly.text = disassemblyContents;
        }*/

        private void UpdateRegistryDisplay()
        {
            if (!debugItems) return;
            string result = string.Empty;
            result += string.Format("<color=red>Step {0}</color>\n", _stepCount);
            for (int i = 0; i < 0x10; i++)
            {
                result += string.Format("V{0:X1}: {1:X2}\n", i, _emulator.V[i]);
            }

            result += string.Format("\nI: {0:X4}\n", _emulator.I);
            result += string.Format("PC: {0:X4}\n", _emulator.PC);
            result += string.Format("SP: {0:X1}\n", _emulator.StackPointer);

            for (int i = 0; i < _emulator.Stack.Length; i++)
            {
                if (_emulator.Stack[i] != 0)
                {
                    result += string.Format("<color=yellow>S{0:X1}: {1:X4}</color>\n", i, _emulator.Stack[i]);
                }
            }

            registerDisplay.text = result;
        }

        private void DisplayInput()
        {
            if (!debugItems) return;
            string result = string.Empty;
            const string formatStringOff = "[{0}] ";
            const string formatStringOn = "<color=red>[{0}]</color> ";

            // First row
            result += string.Format((_emulator.Input[1] ? formatStringOn : formatStringOff), 1);
            result += string.Format((_emulator.Input[2] ? formatStringOn : formatStringOff), 2);
            result += string.Format((_emulator.Input[3] ? formatStringOn : formatStringOff), 3);
            result += string.Format((_emulator.Input[0xC] ? formatStringOn : formatStringOff), 'C');
            result += '\n';
            result += string.Format((_emulator.Input[4] ? formatStringOn : formatStringOff), 4);
            result += string.Format((_emulator.Input[5] ? formatStringOn : formatStringOff), 5);
            result += string.Format((_emulator.Input[6] ? formatStringOn : formatStringOff), 6);
            result += string.Format((_emulator.Input[0xD] ? formatStringOn : formatStringOff), 'D');
            result += '\n';
            result += string.Format((_emulator.Input[7] ? formatStringOn : formatStringOff), 7);
            result += string.Format((_emulator.Input[8] ? formatStringOn : formatStringOff), 8);
            result += string.Format((_emulator.Input[9] ? formatStringOn : formatStringOff), 9);
            result += string.Format((_emulator.Input[0xE] ? formatStringOn : formatStringOff), 'E');
            result += '\n';
            result += string.Format((_emulator.Input[0xA] ? formatStringOn : formatStringOff), 'A');
            result += string.Format((_emulator.Input[0] ? formatStringOn : formatStringOff), 0);
            result += string.Format((_emulator.Input[0xB] ? formatStringOn : formatStringOff), 'B');
            result += string.Format((_emulator.Input[0xF] ? formatStringOn : formatStringOff), 'F');

            disassembly.text = result;
        }

        void FixedUpdate()
        {
            if (runMode == Chip8RunMode.FixedUpdate)
            {
                Step();
            }
        }

        void Update()
        {
            DisplayInput();
            if (runMode == Chip8RunMode.Update)
            {
                Step();
            }
        }

        void Step()
        {
            if (_emulator.Powered)
            {
                _emulator.Update();
                _stepCount++;
                if (_emulator.Draw)
                    display.texture = RenderChipFrame(_emulator.Display, backgroundColor, foregroundColor);
                // RenderFrame(emulator.Display);
                UpdateRegistryDisplay();
            }
        }

        public static Texture2D RenderChipFrame(byte[,] data, Color bg, Color fg)
        {
            Texture2D result = new Texture2D(64, 32);
            result.filterMode = FilterMode.Point;
            for (int y = 0; y < result.height; y++)
            {
                for (int x = 0; x < result.width; x++)
                {
                    Color color = data[x, y] > 0 ? fg : bg;
                    result.SetPixel(x, result.height - 1 - y, color);
                }
            }

            result.Apply(false);
            return result;
        }

        #endregion

        #region Input

        public void Input0(InputAction.CallbackContext context)
        {

            if (context.started)
            {
                _emulator.Input[0] = true;
            }
            else if (context.canceled)
                _emulator.Input[0] = false;
        }

        public void Input1(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[1] = true;
            else if (context.canceled)
                _emulator.Input[1] = false;
        }

        public void Input2(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[2] = true;
            else if (context.canceled)
                _emulator.Input[2] = false;
        }

        public void Input3(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[3] = true;
            else if (context.canceled)
                _emulator.Input[3] = false;
        }

        public void Input4(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[4] = true;
            else if (context.canceled)
                _emulator.Input[4] = false;
        }

        public void Input5(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[5] = true;
            else if (context.canceled)
                _emulator.Input[5] = false;
        }

        public void Input6(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[6] = true;
            else if (context.canceled)
                _emulator.Input[6] = false;
        }

        public void Input7(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[7] = true;
            else if (context.canceled)
                _emulator.Input[7] = false;
        }

        public void Input8(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[8] = true;
            else if (context.canceled)
                _emulator.Input[8] = false;
        }

        public void Input9(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[9] = true;
            else if (context.canceled)
                _emulator.Input[9] = false;
        }

        public void InputA(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[0xA] = true;
            else if (context.canceled)
                _emulator.Input[0xA] = false;
        }

        public void InputB(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[0xB] = true;
            else if (context.canceled)
                _emulator.Input[0xB] = false;
        }

        public void InputC(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[0xC] = true;
            else if (context.canceled)
                _emulator.Input[0xC] = false;
        }

        public void InputD(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[0xD] = true;
            else if (context.canceled)
                _emulator.Input[0xD] = false;
        }

        public void InputE(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[0xE] = true;
            else if (context.canceled)
                _emulator.Input[0xE] = false;
        }

        public void InputF(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.Input[0xF] = true;
            else if (context.canceled)
                _emulator.Input[0xF] = false;
        }

        public void InputStep(InputAction.CallbackContext context)
        {
            if (context.performed && runMode == Chip8RunMode.Step)
                Step();
        }

        public void InputReset(InputAction.CallbackContext context)
        {
            if (context.canceled)
                _emulator.PowerAndLoadRom(_romData);
        }

        #endregion
    }

    public enum Chip8RunMode
    {
        FixedUpdate,
        Update,
        Step
    }
}