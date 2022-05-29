using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using System.Diagnostics;

namespace Project1
{

    public static class Pico8
    {
        static Dictionary<byte, Color> Colors;

        static string path = @"/Users/ljm/Library/Application Support/pico-8/carts/portal.p8";
        public static string[] AllLines = { };
        public static byte[] allChunks = new byte[16 * 16 * 32];

        static Texture2D SpriteSheet;
        static int spriteDrawSize = 8 * 2;

        const int mapGridW = 4;
        static int offsetY = 0;


        public static int SpriteTabCount(string[] allLines)
        {
            int start = 0;
            int end = 0;

            for (int i = 0; i < allLines.Length; i++)
            {
                string line = allLines[i];
                if (line.Equals("__gfx__"))
                {
                    start = i + 1;
                }
                if (line.Equals("__label__") || line.Equals("__gff__"))
                {
                    end = i - 1;
                    break;
                }
            }

            int tabCount = (int)MathF.Ceiling((float)(end - start) / 32);
            return tabCount;
        }

        public static int SpriteTabCount(string path)
        {
            string[] allLines = File.ReadAllLines(path);
            return SpriteTabCount(allLines);
        }

        public static int UpdateSpriteSheet(string[] lines, GraphicsDevice graphics, Texture2D sheet)
        {
            int dataW = 8 * 16;
            int dataSize = 8 * 8 * 16 * 4 * 4;
            Color[] colorData = new Color[dataSize];

            // Find sprite index;
            int startIndex = 0;
            int endIndex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Equals("__gfx__"))
                {
                    startIndex = i + 1;
                }
                if (line.Equals("__label__") || line.Equals("__gff__"))
                {
                    endIndex = i - 1;
                    break;
                }
            }

            // Set texture data
            for (int i = 0; i < endIndex - startIndex - 1; i++)
            {
                string line = lines[startIndex + i];

                for (int j = 0; j < line.Length; j++)
                {
                    string numStr = line.Substring(j, 1);
                    byte num = Convert.ToByte(numStr, 16);
                    colorData[i * dataW + j] = Colors[num];
                }
            }

            sheet.SetData<Color>(colorData, 0, dataSize);

            // Return tab count
            return (endIndex - startIndex + 1) / (8 * 4);
        }

        public static string[] GetMapLines(string[] allLines)
        {
            int start = 0;
            int end = 0;
            int i = 0;

            for (; i < allLines.Length; i++)
            {
                string line = allLines[i];
                if (line.Equals("__map__"))
                {
                    start = i + 1;
                    continue;
                }
                if (start != 0 && (line.Length != allLines[start].Length))
                {
                    end = i - 1;
                    break;
                }
            }

            end = i - 1;
            string[] mapLines = allLines[start..(end + 1)];
            return mapLines;
        }

        public static byte[,] GetMapDataInMap(string[] allLines)
        {
            string[] mapLines = GetMapLines(allLines);
            byte[,] data = new byte[mapLines.Length, mapLines[0].Length / 2];

            for (int i = 0; i < mapLines.Length; i++)
            {
                string line = mapLines[i];
                for (int j = 0; j < line.Length / 2; j++)
                {
                    string pair = line.Substring(j * 2, 2);
                    byte b = Convert.ToByte(pair, 16);
                    data[i, j] = b;
                }
            }

            return data;
        }

        public static byte[,] GetMapDataInMap(string path)
        {
            return GetMapDataInMap(File.ReadAllLines(path));
        }

        public static byte[] GetLinearMapDataInMap(string[] allLines)
        {
            string[] mapLines = GetMapLines(allLines);

            byte[] linearData = new byte[16 * 8 * 16 * 2];

            for (int i = 0; i < mapLines.Length; i++)
            {
                string line = mapLines[i];
                for (int j = 0; j < line.Length / 2; j++)
                {
                    string pair = line.Substring(j * 2, 2);
                    byte b = Convert.ToByte(pair, 16);
                    linearData[i * 16 * 8 + j] = b;
                }
            }

            return linearData;
        }

        public static byte[] GetMapData(string[] allLines)
        {
            byte[] data = GetLinearMapDataInMap(allLines);

            int tabCount = SpriteTabCount(path);

            // Get data in gfx if has 4 tabs
            if (tabCount == 4)
            {
                int start = 0;
                int end = 0;

                for (int i = 0; i < allLines.Length; i++)
                {
                    string line = allLines[i];
                    if (line.Equals("__gfx__"))
                    {
                        start = i + 1 + 8 * 4 * 2;
                    }
                    if (line.Equals("__gff__") || line.Equals("__label__"))
                    {
                        end = i - 1;
                        break;
                    }
                }

                string[] mapLines = allLines[start..(end + 1)];

                string concated = "";
                foreach (string line in mapLines)
                {
                    concated += line;
                }

                // Turn concated string into continuous byte array
                byte[] linearData = new byte[16 * 16 * 8 * 2]; // Leave space for empty data at the end


                for (int i = 0; i < concated.Length / 2; i++)
                {
                    string pair = concated.Substring(i * 2, 2);
                    string[] charPair = { pair.Substring(0, 1), pair.Substring(1, 1) };
                    byte[] sepNums = { Convert.ToByte(charPair[0], 16), Convert.ToByte(charPair[1], 16) };
                    ;
                    byte num = (byte)(sepNums[0] + sepNums[1] * 16);
                    linearData[i] = num;
                }

                // Merge gfx map data with map data
                int dataEnd = data.Length;
                Array.Resize<byte>(ref data, data.Length + linearData.Length);
                Array.Copy(linearData, 0, data, dataEnd, linearData.Length);
            }

            return data;
        }

        public static byte[,] GetSplitMapData(byte[] linearData)
        {
            byte[,] splitData = new byte[16 * 4, 16 * 8];
            for (int r = 0; r < splitData.GetLength(0); r++)
            {
                for (int c = 0; c < splitData.GetLength(1); c++)
                {
                    splitData[r, c] = linearData[r * 16 * 8 + c];
                }
            }
            return splitData;
        }

        public static byte[,] GetSplitMapData(string[] allLines)
        {
            return GetSplitMapData(GetMapData(allLines));
        }

        public static byte[,] GetChunkData(string[] allLines, int row, int col)
        {
            byte[,] data = new byte[16, 16];
            byte[,] splitData = GetSplitMapData(allLines);

            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 16; c++)
                {
                    data[r, c] = splitData[row * 16 + r, col * 16 + c];
                }
            }

            return data;
        }

        public static byte[,] GetChunkData(string[] allLines, int index)
        {
            int col = index % 8;
            int row = (index - col) / 8;

            return GetChunkData(allLines, row, col);
        }

        public static byte[] GetChunkData(int row, int col) //row0row1row2
        {
            byte[] chunkData = new byte[16 * 16];

            byte[,] splitData = GetSplitMapData(AllLines);

            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 16; c++)
                {
                    chunkData[r * 16 + c] = splitData[row * 16 + r, col * 16 + c];
                }
            }

            return chunkData;
        }

        // SingleD array chunk functions
        public static byte[] GetChunkData(int index)
        {
            int col = index % 8;
            int row = (index - col) / 8;
            return GetChunkData(row, col);
        }

       
        public static byte[] GetAllChunkData()
        {
            byte[] allData = new byte[16 * 16 * 32];
            for (int i = 0; i < 32; i++)
            {
                Array.Copy(GetChunkData(i), 0, allData, i * 16 * 16, 16 * 16);
            }
            return allData;
        }

        public static byte GetSpriteInChunk(byte[] chunk, int x, int y)
        {
            return chunk[y * 16 + x];
        }

        public static Point GetChunkPosByMouse(string[] allLines, int ScreenW)
        {
            var ms = Mouse.GetState();
            int size = ScreenW / mapGridW;

            Point mousePos = ms.Position + new Point(0, -offsetY);

            Point gridPos = new Point((int)Math.Floor((float)mousePos.X / size),
                (int)Math.Floor((float)mousePos.Y / size));

            return gridPos;
        }


        public struct RepositionInfo
        {
            public Point gPos;
            public bool isForward;
        }

        public static RepositionInfo GetRepositionInfoByMouse(int ScreenW)
        {
            var ms = Mouse.GetState();
            int size = ScreenW / mapGridW;
            RepositionInfo rpInfo = new RepositionInfo();

            Point mousePos = ms.Position + new Point(0, -offsetY);

            Point gridPos = new Point((int)Math.Floor((float)mousePos.X / size),
                (int)Math.Floor((float)mousePos.Y / size));

            if (mousePos.X < (gridPos.X + 0.5) * size )
            {
                rpInfo.gPos = gridPos;
                rpInfo.isForward = true;
            }

            return rpInfo;
        }

        public static void Reposition(int pickedIndex, int toIndex)
        {
            Debug.Assert(pickedIndex == toIndex);

            var chunks = GetAllChunkData();
            byte[] newChunks = new byte[chunks.Length];

            if (pickedIndex > toIndex)
            {
                Array.Copy(chunks, newChunks, chunks.Length);
                Array.Copy(chunks, pickedIndex, newChunks, pickedIndex + 1, pickedIndex - toIndex);
                newChunks[toIndex] = chunks[pickedIndex];
            }
        }

        public static int GetChunkIndexByMouse(string[] allLines, int ScreenW)
        {
            Point gridPos = GetChunkPosByMouse(allLines, ScreenW);
            return gridPos.Y * ScreenW + gridPos.X;
        }

        public static void DrawSprite(SpriteBatch batch, Texture2D sheet, int index, int x, int y)
        {
            int size = spriteDrawSize;
            int sx = index % 16;
            int sy = (index - sx) / 16;

            var src = new Rectangle(sx * 8, sy * 8, 8, 8);
            var dst = new Rectangle(x * size, y * size, size, size);
            batch.Draw(sheet, dst, src, Color.White);
        }

        public static void DrawSprite(SpriteBatch batch, Texture2D sheet, int index, Rectangle dst)
        {
            int sx = index % 16;
            int sy = (index - sx) / 16;
            var src = new Rectangle(sx * 8, sy * 8, 8, 8);
            batch.Draw(sheet, dst, src, Color.White);
        }

        public static Rectangle CenterRectOnPos(Rectangle rect, Point pos)
        {
            return new Rectangle(pos.X - rect.Width/2, pos.Y - rect.Height/2, rect.Width, rect.Height);
        }

        public static void DrawMapChunk(SpriteBatch batch, byte[,] chunk, Texture2D sheet)
        {
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    byte spr = chunk[y, x];
                    DrawSprite(batch, sheet, spr, x, y);
                }
            }
        }

        public static void DrawChunkOnMouse(GraphicsDevice device, SpriteBatch batch, string[] allLines, int index, float scale)
        {
            // Draw sprites on chunk
            var chunk = GetChunkData(allLines, index);
            int sprW = (int)(Game1.ScreenW * scale / (mapGridW * 16));

            var origin = CenterRectOnPos(new Rectangle(0, 0, sprW * 16, sprW * 16), Mouse.GetState().Position);

            for (int r = 0; r < chunk.GetLength(0); r++)
            {
                for (int c = 0; c < chunk.GetLength(1); c++)
                {
                    var dst = new Rectangle(origin.X + c * sprW, origin.Y + r * sprW, sprW, sprW);
                    DrawSprite(batch, SpriteSheet, chunk[r, c], dst);
                }
            }
            // Draw outline

            Primitives2D.DrawRectangle(batch, origin, Color.White, 4);
        }

        public static void DrawMapChunksInScreen(SpriteBatch batch, string[] allLines, Texture2D sheet)
        {
            int size = Game1.ScreenW / (mapGridW * 16);
            for (int i = 0; i < 8 * 4; i++)
            {
                var chunk = GetChunkData(allLines, i);

                var gPos = Helper.IndexToPos(i, mapGridW);

                int x = gPos.X;
                int y = gPos.Y;

                for (int r = 0; r < 16; r++)
                {
                    for (int c = 0; c < 16; c++)
                    {
                        byte spr = chunk[r, c];
                        var dst = new Rectangle((x * 16 + c) * size, (y * 16 + r) * size + (int)offsetY, size, size);
                        DrawSprite(batch, sheet, spr, dst);
                    }
                }
            }

            // Draw outline
            bool isOutLineOn = true;
            if (isOutLineOn)
            {
                Helper.DrawGridLine(batch, 0, offsetY, 4, 20, Game1.ScreenW, Color.Gray);
            }
        }

        public static void DrawAllChunks(SpriteBatch batch)
        {
            int size = Game1.ScreenW / (mapGridW * 16);
            byte[] allChunks = GetAllChunkData();
            for (int i = 0; i < 32; i++)
            {
                int index = i * 16 * 16;
                var chunk = allChunks[index..(index + 16 * 16)];
                var gPos = Helper.IndexToPos(i, mapGridW);

                int x = gPos.X;
                int y = gPos.Y;

                for (int r = 0; r < 16; r++)
                {
                    for (int c = 0; c < 16; c++)
                    {
                        byte spr = GetSpriteInChunk(chunk, c, r);
                        var dst = new Rectangle((x * 16 + c) * size, (y * 16 + r) * size + (int)offsetY, size, size);
                        DrawSprite(batch, SpriteSheet, spr, dst);
                    }
                }
            }

            // Draw outline
            bool isOutLineOn = true;
            if (isOutLineOn)
            {
                Helper.DrawGridLine(batch, 0, offsetY, 4, 20, Game1.ScreenW, Color.Gray);
            }
        }

        public static void Draw(GraphicsDevice graphics,SpriteBatch batch)
        {
            DrawAllChunks(batch);
            int chunkW = Game1.ScreenW / mapGridW;
            var chunkPos = GetChunkPosByMouse(AllLines, Game1.ScreenW);
            // Draw selected chunk outline
            {
                var rect = new Rectangle(chunkPos.X * chunkW, chunkPos.Y * chunkW + offsetY, chunkW, chunkW);
                if (Click.IsPressing)
                {
                    chunkPos = Click.LastGridPos;
                    Primitives2D.FillRectangle(batch, rect, Color.Black);
                }

                Primitives2D.DrawRectangle(batch, rect, Color.White, 4);
            }

            if (Click.IsPressing)
            {
                DrawChunkOnMouse(graphics, batch, AllLines, Helper.PosToIndex(chunkPos, mapGridW), 1.0f);
            }
        }

        public static void Init(GraphicsDevice graphics)
        {
            SpriteSheet = new Texture2D(graphics, 8 * 16, 8 * 4 * 4);
            // Set color map
            Colors = new Dictionary<byte, Color>();
            Colors.Add(0, new Color(0, 0, 0));
            Colors.Add(1, new Color(29, 43, 83));
            Colors.Add(2, new Color(126, 37, 83));
            Colors.Add(3, new Color(0, 135, 81));
            Colors.Add(4, new Color(171, 82, 54));
            Colors.Add(5, new Color(95, 87, 79));
            Colors.Add(6, new Color(194, 195, 199));
            Colors.Add(7, new Color(255, 241, 232));
            Colors.Add(8, new Color(255, 0, 77));
            Colors.Add(9, new Color(255, 163, 0));
            Colors.Add(10, new Color(255, 236, 39));
            Colors.Add(11, new Color(0, 228, 54));
            Colors.Add(12, new Color(41, 173, 255));
            Colors.Add(13, new Color(131, 118, 156));
            Colors.Add(14, new Color(255, 119, 168));
            Colors.Add(15, new Color(255, 204, 170));


            // Read data from text file
            {
                string[] lines = System.IO.File.ReadAllLines(path);
                Array.Resize<string>(ref AllLines, lines.Length);
                Array.Copy(lines, AllLines, lines.Length);

                GetLinearMapDataInMap(AllLines);
                GetSplitMapData(GetMapData(AllLines));
                UpdateSpriteSheet(lines, graphics, SpriteSheet);
            }
        }

        public static void Update()
        {
            float sens = 0.1f;
            MouseWheel.Update();
            Click.Update();
            Console.WriteLine("mouse wheel increment: " + MouseWheel.increment);
            Console.WriteLine("chunk pos: " + GetChunkPosByMouse(AllLines, Game1.ScreenW));
            Console.WriteLine("offset y: " + offsetY);
            offsetY += (int)(MouseWheel.increment * sens);
            if (offsetY > 0)
            {
                offsetY = 0;
            }
        }
    }

    public static class MouseWheel
    {
        static int lastWheel = 0;
        public static int increment = 0;

        public static void Update()
        {
            var ms = Mouse.GetState();
            increment = ms.ScrollWheelValue - lastWheel;
            lastWheel = ms.ScrollWheelValue;
        }
    }

    public static class Click
    {
        static int pickedChunk = 0;
        public static Point LastGridPos;
        public static bool IsPressing = false;
        public static void Update()
        {
            var ms = Mouse.GetState();
            if (!IsPressing && ms.LeftButton == ButtonState.Pressed)
            {
                IsPressing = true;
                LastGridPos = Pico8.GetChunkPosByMouse(Pico8.AllLines, Game1.ScreenW);
                pickedChunk = Pico8.GetChunkIndexByMouse(Pico8.AllLines, Game1.ScreenW);
            }
            if (IsPressing && ms.LeftButton != ButtonState.Pressed)
            {
                IsPressing = false;
            }
            
        }
    }

    public class Level
    {
        public static int W = 16;
        public static int H = 16;

        public Texture2D _tex;
        public Level(GraphicsDevice graphics)
        {
            _tex = new Texture2D(graphics, W, H);
            // Set pixels on texture.
            Color[] testData = new Color[W * H];
            for (int i = 0; i < testData.Length; i++)
            {
                testData[i] = Color.Black;
            }
            this._tex.SetData<Color>(testData, 0, W * H);
        }
        
        public void Draw(SpriteBatch batch)
        {
            // Draw texture on screen.
            // batch.Draw(this._tex, new Rectangle(0, 0, W * 10, H * 10), Color.White);
            // Pico8.DrawSprite(batch, Pico8.SpriteSheet, 36, 0,0);
        }
    }

    public static class Helper
    {
        public static string MergeLines(string[] lines, int start, int end)
        {
            string merged = "";
            for (int i = start; i <= end; i++)
            {
                string line = lines[i];
                merged += line;
            }
            return merged;
        }

        public static int PosToIndex(Point pos, int w)
        {
            return pos.Y * w + pos.X;
        }

        public static Point IndexToPos(int index, int w)
        {
            int x = index % w;
            int y = (index - x) / w;
            return new Point(x, y);
        }

        public static void DrawGridLine(SpriteBatch batch, int x, int y, int w, int h, int sw, Color color)
        {
            
            int cellS = sw / w;

            // Verticle lines
            for (int gx = 0; gx < w; gx++)
            {
                Primitives2D.DrawLine(batch, new Vector2(gx * cellS + x, y), new Vector2(gx * cellS, y + h * cellS), color);
            }

            // Horizontal lines
            for (int gy = 0; gy < h; gy++)
            {
                Primitives2D.DrawLine(batch, new Vector2(0, gy * cellS + y), new Vector2(x + sw, gy * cellS + y), color);
            }
        }

    }
}
