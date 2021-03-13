using System;
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
        [Header("Audio")] private AudioSource _source;
        [SerializeField] private float audioFrequency = 440f;
        [SerializeField] private float audioSampleRate = 44100;
        [SerializeField] private float audioWavelength = 0.16f;
        private int _timeIndex = 0;
        
        private ulong _stepCount;
        public string disassemblyContents;
        private byte[] _romData;

        #region Functionality

        private void Start()
        {
            // Setup AudioSource
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0;
            _source.Stop();
            // Load ROM into emulator
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

        void Update()
        {
            Step();
            
        }

        void FixedUpdate()
        {
            _emulator.DecrementTimers();
            if (_emulator.Sound > 0)
            {
                if (!_source.isPlaying)
                {
                    _timeIndex = 0;
                    _source.Play();
                }
            }
            else
            {
                _source.Stop();
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
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                data[i] = GenerateSine(_timeIndex, audioFrequency, audioSampleRate);
                if(channels == 2)
                    data[i+1] = GenerateSine(_timeIndex, audioFrequency, audioSampleRate);

                _timeIndex++;

                if (_timeIndex >= audioSampleRate * audioWavelength * _emulator.Sound)
                {
                    _timeIndex = 0;
                }
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

        public static float GenerateSine(int time, float frequency, float sampleRate = 44100f)
        {
            return Mathf.Sin(2 * Mathf.PI * time * frequency / sampleRate);
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
            if (context.performed)
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