using System;
using UnityEngine;
using System.Collections;

namespace Chip8
{
    public struct Chip8Display
    {
        // Constants for screen width and height so we can avoid magic numbers.
        public const int WIDTH = 128;
        public const int HEIGHT = 64;

        public bool hiresMode;
        public byte[,] pixels;
        public bool doWraparound;

        public void SetPixel(int x, int y, bool lores = false)
        {
            if(lores)
            {
                Debug.Log($"Setting pixel at {x}, {y} (lores)");
                pixels[x * 2, y * 2] ^= 1;
                var val = pixels[x * 2, y * 2];
                pixels[x * 2 + 1, y * 2] = pixels[x * 2 + 1, y * 2 + 1] = pixels[x * 2, y * 2 + 1] = val;
                return;
            }

            Debug.Log($"Setting pixel at {x}, {y}");
            pixels[x, y] ^= 1;
        }

        public void Clear()
        {
            for (var i = 0; i < HEIGHT; i++)
                for (var j = 0; j < WIDTH; j++)
                    pixels[j, i] = 0;
        }

        public bool DrawSpriteLores(int x, int y, int n, byte[] sprite)
        {
            Debug.Log($"Drawing sprite at ({x}, {y}) with {n} lines");
            bool unset = false;
            for(var sy = 0; sy < n; sy++)
            {
                var pixel = sprite[sy];
                Debug.Log($"Byte data: {pixel}");
                for (var sx = 0; sx < 8; sx++)
                {
                    if((pixel & (0x80 >> sx)) != 0)
                    {
                        var rawX = (x + sx);
                        var rawY = (y + sy);
                        if(doWraparound)
                        {
                            rawX %= 64;
                            rawY %= 32;
                        }
                        else
                        {
                            if (rawX >= 64 || rawY >= 32)
                                continue;
                        }
                        var xPos = rawX * 2;
                        var yPos = rawY * 2;
                        if(pixels[xPos, yPos] == 1)
                        {
                            unset = true;
                        }
                        SetPixel(rawX, rawY, true);
                    }
                }
            }

            Debug.Log("Hi!");
            return unset;
        }

        public Chip8Display(bool hires = false, bool wraparound = false)
        {
            pixels = new byte[WIDTH, HEIGHT];
            hiresMode = hires;
            doWraparound = wraparound;
        }
    }
}