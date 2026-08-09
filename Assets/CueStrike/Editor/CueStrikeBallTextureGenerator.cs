#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

namespace CueStrike.Editor
{
    /// <summary>
    /// Procedurally generates pool ball textures (0-15) and saves them as PNGs.
    /// Menu: CueStrike → Generate → Create Ball Textures (0-15)
    ///
    /// Color mapping (standard 8-ball):
    ///   0 = White (cue) with small red spot
    ///   1 = Yellow, 2 = Blue, 3 = Red, 4 = Purple, 5 = Orange,
    ///   6 = Green, 7 = Maroon, 8 = Black, 9-15 = Striped (white + color band)
    ///
    /// After generation, materials can use 'Universal Render Pipeline/Lit'.
    /// </summary>
    public static class CueStrikeBallTextureGenerator
    {
        public const string OutputFolder = "Assets/CueStrike/Textures/Balls";
        private const int TextureSize = 128;
        private const float BallDiameter = 108f; // circle diameter in pixels inside canvas

        // Standard 8-ball solid colors
        private static readonly Color[] SolidColors =
        {
            new Color(1.00f, 0.78f, 0.15f), // 1  Yellow
            new Color(0.10f, 0.35f, 0.85f), // 2  Blue
            new Color(0.90f, 0.15f, 0.15f), // 3  Red
            new Color(0.45f, 0.20f, 0.65f), // 4  Purple
            new Color(1.00f, 0.45f, 0.05f), // 5  Orange
            new Color(0.05f, 0.55f, 0.25f), // 6  Green
            new Color(0.55f, 0.05f, 0.10f), // 7  Maroon
            new Color(0.12f, 0.12f, 0.12f)  // 8  Black
        };

        [MenuItem("CueStrike/Generate/Create Ball Textures (0-15)")]
        public static void GenerateAllBallTextures()
        {
            Directory.CreateDirectory(OutputFolder);
            int created = 0;

            // Cue ball (0)
            if (CreateBallTexture("ball_0_cue", ball =>
            {
                FillBall(ball, Color.white);
                // Small red spot (official cue ball marking)
                DrawCircle(ball, new Vector2(0.5f, 0.64f), 0.06f, new Color(0.85f, 0.10f, 0.10f));
            }))
            {
                created++;
            }

            // Balls 1-15
            for (int n = 1; n <= 15; n++)
            {
                bool striped = n >= 9;
                int colorIndex = (n - 1) % 8;
                Color baseColor = SolidColors[colorIndex];
                string name = striped ? $"ball_{n}_striped" : $"ball_{n}_solid";

                if (CreateBallTexture(name, ball =>
                {
                    if (striped)
                    {
                        // White base + horizontal color band
                        FillBall(ball, Color.white);
                        PaintHorizontalBand(ball, baseColor);
                    }
                    else
                    {
                        FillBall(ball, baseColor);
                    }

                    // White number disc
                    DrawCircle(ball, new Vector2(0.5f, 0.5f), 0.24f, Color.white);

                    // Number text (dot-matrix 5x7)
                    DrawNumber(ball, n, new Vector2(0.5f, 0.5f), 0.105f, Color.black);
                }))
                {
                    created++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[CueStrike] Generated {created} ball textures in '{OutputFolder}'.");
            EditorUtility.DisplayDialog("Ball Textures",
                $"Created {created} ball textures in:\n{OutputFolder}\n\n" +
                "Now assign them to the ball materials using URP/Lit.",
                "OK");
        }

        /// <summary>Creates a single ball texture and saves it as a PNG.</summary>
        private static bool CreateBallTexture(string name, Action<Texture2D> painter)
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);

            // Transparent background
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < TextureSize; y++)
                for (int x = 0; x < TextureSize; x++)
                    tex.SetPixel(x, y, clear);

            painter(tex);
            tex.Apply();

            string path = Path.Combine(OutputFolder, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[CueStrike] Ball texture: {name}.png");
            return true;
        }

        /// <summary>Returns true if pixel (x,y) lies inside the ball circle.</summary>
        private static bool IsInsideBall(int x, int y)
        {
            float cx = TextureSize * 0.5f;
            float cy = TextureSize * 0.5f;
            float r = BallDiameter * 0.5f;
            float dx = x - cx;
            float dy = y - cy;
            return (dx * dx + dy * dy) < (r * r);
        }

        /// <summary>Fills the ball with the given color, adding subtle edge shading.</summary>
        private static void FillBall(Texture2D tex, Color c)
        {
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    if (!IsInsideBall(x, y)) continue;

                    float nx = (x - TextureSize * 0.5f) / (BallDiameter * 0.5f);
                    float ny = (y - TextureSize * 0.5f) / (BallDiameter * 0.5f);
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float shade = Mathf.Lerp(1.0f, 0.70f, Mathf.Clamp01(dist));
                    tex.SetPixel(x, y, new Color(c.r * shade, c.g * shade, c.b * shade, 1f));
                }
            }
        }

        /// <summary>Paints a horizontal color band across the middle (for striped balls).</summary>
        private static void PaintHorizontalBand(Texture2D tex, Color color)
        {
            float bandHalf = TextureSize * 0.18f;
            int cy = TextureSize / 2;
            for (int y = Mathf.RoundToInt(cy - bandHalf); y <= Mathf.RoundToInt(cy + bandHalf); y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    if (IsInsideBall(x, y))
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        /// <summary>Draws a filled circle with the given center (0..1) and radius (0..1).</summary>
        private static void DrawCircle(Texture2D tex, Vector2 centerN, float radiusN, Color color)
        {
            Vector2 center = new Vector2(centerN.x * TextureSize, centerN.y * TextureSize);
            float radius = radiusN * TextureSize;

            int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int x1 = Mathf.Min(TextureSize - 1, Mathf.CeilToInt(center.x + radius));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int y1 = Mathf.Min(TextureSize - 1, Mathf.CeilToInt(center.y + radius));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!IsInsideBall(x, y)) continue;

                    float dx = x - center.x;
                    float dy = y - center.y;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        /// <summary>Draws a multi-digit number using a 5x7 dot-matrix font.</summary>
        private static void DrawNumber(Texture2D tex, int number, Vector2 centerN, float cellSizeN, Color color)
        {
            string digits = number.ToString();
            float spacing = cellSizeN * 0.85f;
            float totalWidth = spacing * digits.Length;
            float startXN = centerN.x - totalWidth * 0.5f + cellSizeN * 0.5f;

            for (int i = 0; i < digits.Length; i++)
            {
                DrawSingleDigit(tex, centerN.x - totalWidth * 0.5f + i * spacing, centerN.y, cellSizeN, digits[i], color);
            }
        }

        /// <summary>Draws a single dot-matrix digit centered at (cxN, cyN).</summary>
        private static void DrawSingleDigit(Texture2D tex, float cxN, float cyN, float cellSizeN, char digit, Color color)
        {
            string[] pattern = GetDigitPattern(digit);
            int rows = 7;
            int cols = 5;
            float w = cellSizeN * TextureSize;
            float h = cellSizeN * TextureSize;
            float startX = cxN * TextureSize - cols * w * 0.5f;
            float startY = cyN * TextureSize - rows * h * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (pattern[r][c] == '1')
                    {
                        PaintRect(tex, startX + c * w, startY + r * h, w, h, color);
                    }
                }
            }
        }

        /// <summary>Returns the 5x7 dot matrix (rows of '0'/'1') for a single digit.</summary>
        private static string[] GetDigitPattern(char c)
        {
            // Each pattern is 7 rows of length 5.
            switch (c)
            {
                case '0': return new[]
                {
                    "01110", "10001", "10011", "10101", "11001", "10001", "01110"
                };
                case '1': return new[]
                {
                    "00100", "01100", "00100", "00100", "00100", "00100", "01110"
                };
                case '2': return new[]
                {
                    "01110", "10001", "00001", "00010", "00100", "01000", "11111"
                };
                case '3': return new[]
                {
                    "01110", "10001", "00001", "00110", "00001", "10001", "01110"
                };
                case '4': return new[]
                {
                    "00010", "00110", "01010", "10010", "11111", "00010", "00010"
                };
                case '5': return new[]
                {
                    "11111", "10000", "11110", "00001", "00001", "10001", "01110"
                };
                case '6': return new[]
                {
                    "00110", "01000", "10000", "11110", "10001", "10001", "01110"
                };
                case '7': return new[]
                {
                    "11111", "00001", "00010", "00100", "01000", "01000", "01000"
                };
                case '8': return new[]
                {
                    "01110", "10001", "10001", "01110", "10001", "10001", "01110"
                };
                case '9': return new[]
                {
                    "01110", "10001", "10001", "01111", "00001", "00010", "01100"
                };
                default: return new[]
                {
                    "00000", "00000", "00000", "00000", "00000", "00000", "00000"
                };
            }
        }

        /// <summary>Paints a filled rectangle in pixel coordinates.</summary>
        private static void PaintRect(Texture2D tex, float px0, float py0, float pw, float ph, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(px0));
            int x1 = Mathf.Min(TextureSize - 1, Mathf.CeilToInt(px0 + pw));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(py0));
            int y1 = Mathf.Min(TextureSize - 1, Mathf.CeilToInt(py0 + ph));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!IsInsideBall(x, y)) continue;
                    tex.SetPixel(x, y, color);
                }
            }
        }
    }
}
#endif