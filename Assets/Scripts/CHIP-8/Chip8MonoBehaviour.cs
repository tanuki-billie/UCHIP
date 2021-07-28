using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
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
        [SerializeField] private MeshRenderer display;
        private Texture2D displayTexture;
        [Header("Audio")] private AudioSource _source;
        [SerializeField] private float audioFrequency = 440f;
        [SerializeField] private float audioSampleRate = 44100;
        [SerializeField] private float audioWavelength = 0.16f;
        private int _timeIndex = 0;
        // The default rom that is loaded is defined here. It's basically telling the player that there is an error with their game.
        private byte[] _romData = { 0x12, 0x1E, 0x24, 0x24, 0x00, 0x3C, 0x42, 0x92, 
                                    0xD5, 0xB5, 0xB5, 0x92, 0x62, 0x85, 0xB5, 0x97, 
                                    0x65, 0x97, 0xF4, 0xF7, 0x94, 0x97, 0xFE, 0xFF, 
                                    0xE7, 0xE7, 0xFF, 0x81, 0xBD, 0xBD, 0xA2, 0x02, 
                                    0x60, 0x0C, 0x61, 0x0D, 0xD0, 0x15, 0xA2, 0x07, 
                                    0x60, 0x18, 0xD0, 0x15, 0xA2, 0x0C, 0x60, 0x24, 
                                    0xD0, 0x15, 0xA2, 0x11, 0x60, 0x2D, 0xD0, 0x15, 
                                    0xA2, 0x16, 0x60, 0x1C, 0x61, 0x16, 0x66, 0x2D, 
                                    0xF6, 0x15, 0xF2, 0x07, 0x32, 0x00, 0x12, 0x4C, 
                                    0xD0, 0x18, 0x12, 0x40, 0x12, 0x42};

        #region Functionality

        private void Start()
        {
            displayTexture = new Texture2D(64, 32);
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
                Debug.LogWarning($"Could not find the rom at {romPath}. Loading default rom...");
                // Destroy(this);
                // return;
            }
            else
            {
                _romData = rom.bytes;
            }

            
            // DisassembleROM();

            _emulator.PowerAndLoadRom(_romData, interpreterMode);
        }

        void Update()
        {
            Step();
            
        }

        void FixedUpdate()
        {
            _emulator.DecrementTimers();
            if (_emulator.state.Sound > 0)
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
                _emulator.Cycle();
                if (_emulator.Draw)
                    RenderChipFrame(ref displayTexture, _emulator.state.Display, backgroundColor, foregroundColor);
                display.material.mainTexture = displayTexture;
                // RenderFrame(emulator.Display);
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                data[i] = GenerateSquare(_timeIndex, audioFrequency, audioSampleRate);
                if(channels == 2)
                    data[i+1] = GenerateSquare(_timeIndex, audioFrequency, audioSampleRate);

                _timeIndex++;

                if (_timeIndex >= audioSampleRate * audioWavelength * _emulator.state.Sound)
                {
                    _timeIndex = 0;
                }
            }
        }

        public static void RenderChipFrame(ref Texture2D result, byte[,] data, Color bg, Color fg)
        {
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
        }

        public static float GenerateSine(int time, float frequency, float sampleRate = 44100f)
        {
            return Mathf.Sin(2 * Mathf.PI * time * frequency / sampleRate);
        }

        public static float GenerateSquare(int time, float frequency, float sampleRate = 44100f)
        {
            return Mathf.Sign(GenerateSine(time, frequency, sampleRate));
        }

        #endregion

        #region Input

        public void Input0(InputAction.CallbackContext context)
        {

            if (context.started)
            {
                _emulator.state.Input[0] = true;
            }
            else if (context.canceled)
                _emulator.state.Input[0] = false;
        }

        public void Input1(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[1] = true;
            else if (context.canceled)
                _emulator.state.Input[1] = false;
        }

        public void Input2(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[2] = true;
            else if (context.canceled)
                _emulator.state.Input[2] = false;
        }

        public void Input3(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[3] = true;
            else if (context.canceled)
                _emulator.state.Input[3] = false;
        }

        public void Input4(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[4] = true;
            else if (context.canceled)
                _emulator.state.Input[4] = false;
        }

        public void Input5(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[5] = true;
            else if (context.canceled)
                _emulator.state.Input[5] = false;
        }

        public void Input6(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[6] = true;
            else if (context.canceled)
                _emulator.state.Input[6] = false;
        }

        public void Input7(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[7] = true;
            else if (context.canceled)
                _emulator.state.Input[7] = false;
        }

        public void Input8(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[8] = true;
            else if (context.canceled)
                _emulator.state.Input[8] = false;
        }

        public void Input9(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[9] = true;
            else if (context.canceled)
                _emulator.state.Input[9] = false;
        }

        public void InputA(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[0xA] = true;
            else if (context.canceled)
                _emulator.state.Input[0xA] = false;
        }

        public void InputB(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[0xB] = true;
            else if (context.canceled)
                _emulator.state.Input[0xB] = false;
        }

        public void InputC(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[0xC] = true;
            else if (context.canceled)
                _emulator.state.Input[0xC] = false;
        }

        public void InputD(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[0xD] = true;
            else if (context.canceled)
                _emulator.state.Input[0xD] = false;
        }

        public void InputE(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[0xE] = true;
            else if (context.canceled)
                _emulator.state.Input[0xE] = false;
        }

        public void InputF(InputAction.CallbackContext context)
        {
            if (context.started)
                _emulator.state.Input[0xF] = true;
            else if (context.canceled)
                _emulator.state.Input[0xF] = false;
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