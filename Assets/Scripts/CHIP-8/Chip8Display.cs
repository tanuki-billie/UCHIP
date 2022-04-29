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
            UnityEngine.Debug.Log($"setting pixel {x}, {y}");
            if(lores)
            {
                pixels[x * 2, y * 2] ^= 1;
                var val = pixels[x * 2, y * 2];
                pixels[x * 2 + 1, y * 2] = pixels[x * 2 + 1, y * 2 + 1] = pixels[x * 2, y * 2 + 1] = val;
                return;
            }
            pixels[x, y] ^= 1;
        }

        public void Clear()
        {
            for (var i = 0; i < HEIGHT; i++)
                for (var j = 0; j < WIDTH; j++)
                    pixels[j, i] = 0;
        }

        public byte DrawSpriteLores(int x, int y, int n, byte[] sprite)
        {
            byte set = 0;
            for(var sy = 0; sy < n; sy++)
            {
                var pixel = sprite[sy];
                for (var sx = 0; sx < 8; sx++)
                {
                    if((pixel & (0x80 >> sx)) != 0)
                    {
                        var rawX = (x + sx) % 64;
                        var rawY = (y + sy);
                        if(doWraparound)
                        {
                            rawY %= 32;
                        }
                        else
                        {
                            if (rawY >= 32)
                                continue;
                        }
                        var xPos = rawX * 2;
                        var yPos = rawY * 2;
                        if(pixels[xPos, yPos] == 1)
                        {
                            set = 1;
                        }
                        SetPixel(rawX, rawY, true);
                    }
                }
            }
            return set;
        }

        public byte DrawLoresSpriteHires(int x, int y, int n, byte[] sprite)
        {
            UnityEngine.Debug.Log("hi");
            byte rowsSet = 0;
            for (var sy = 0; sy < n; sy++)
            {
                var pixel = sprite[sy];
                bool rowCollides = false;
                for (var sx = 0; sx < 8; sx++)
                {
                    if ((pixel & (0x80 >> sx)) != 0)
                    {
                        var rawX = (x + sx) % WIDTH;
                        var rawY = (y + sy);
                        UnityEngine.Debug.Log($"pixel coords: {rawX}, {rawY}");
                        if (doWraparound)
                        {
                            rawY %= HEIGHT;
                        }
                        else
                        {
                            if (rawY >= HEIGHT)
                            {
                                rowCollides = true;
                                break;
                            }
                        }
                        if (pixels[rawX, rawY] == 1)
                            rowCollides = true;
                        SetPixel(rawX, rawY);
                    }
                }

                if (rowCollides) rowsSet++;
            }
            return rowsSet;
        }

        public void SetHires(bool set = false)
        {
            hiresMode = set;
        }

        public void ScrollHorizontal(bool scrollRight = true)
        {
            if(scrollRight)
            {
                for (var y = HEIGHT - 1; y >= 0; y++)
                    for (var x = WIDTH - 1; x >= 4; x++)
                        pixels[x, y] = pixels[x - 4, y];

                for (var y = 0; y < HEIGHT; y++)
                    for (var x = 0; x < 4; x++)
                        pixels[x, y] = 0;
            }
            else
            {
                for (var y = 0; y < HEIGHT - 4; y++)
                    for (var x = 0; x < WIDTH - 4; x++)
                        pixels[x, y] = pixels[x + 4, y];

                for (var y = 0; y < HEIGHT; y++)
                    for (var x = 1; x <= 4; x++)
                        pixels[WIDTH - x, y] = 0;
            }
        }

        public void ScrollVertical(int count, bool scrollUp = false)
        {
            // If we are in lores mode, we can go ahead and make sure our count is even
            if(!hiresMode)
            {
                count >>= 1;
                count <<= 1;
            }

            if(scrollUp)
            {
                // This isn't going to be used for schip, so no implementation rn
            }
            else
            {
                for (var x = 0; x < WIDTH; x++)
                    for (var y = count; y < HEIGHT; y++)
                        pixels[x, y] = pixels[x, y - count];

                for (var x = 0; x < WIDTH; x++)
                    for (var y = 0; y < count; y++)
                        pixels[x, y] = 0;
                        
            }
        }

        public byte DrawSpriteHires(int x, int y, ushort[] sprite)
        {
            byte rowsSet = 0;

            for(var sy = 0; sy < 0xF; sy++)
            {
                var pixel = sprite[sy];
                bool rowCollides = false;
                for (var sx = 0; sx < 0xF; sx++)
                {
                    if((pixel & (0x8000 >> sx)) != 0)
                    {
                        var rawX = (x + sx) % WIDTH;
                        var rawY = (y + sy);
                        
                        if(doWraparound)
                        {
                            rawY %= HEIGHT;
                        }
                        else
                        {
                            if (rawY >= HEIGHT)
                            {
                                rowCollides = true;
                                break;
                            }
                        }

                        if (pixels[rawX, rawY] == 1)
                            rowCollides = true;
                        SetPixel(rawX, rawY);
                    }
                }

                if (rowCollides) rowsSet++;
            }

            return rowsSet;
        }

        public Chip8Display(bool hires = false, bool wraparound = false)
        {
            pixels = new byte[WIDTH, HEIGHT];
            hiresMode = hires;
            doWraparound = wraparound;
        }
    }
}