using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Project1
{
    public class Spr
    {
        static Dictionary<byte, Color> Colors;
        public static Color BackGroundColor = new Color(99, 99, 102);
        static Texture2D spriteSheet; // Fixed size by 4 tabs
        public static int TabCount = 0;

        public static void DrawSprite(SpriteBatch batch, int spr, Rectangle dst)
        {
            int sx = spr % 16;
            int sy = (spr - sx) / 16;
            var src = new Rectangle(sx * 8, sy * 8, 8, 8);
            batch.Draw(spriteSheet, dst, src, Color.White);
        }

        public static void DrawSprite(SpriteBatch batch, int spr, int gx, int gy, int sprW)
        {
            var dst = new Rectangle(gx * sprW, gy * sprW, sprW, sprW);
            DrawSprite(batch, spr, dst);
        }

        public static void Init(string[] allLines, GraphicsDevice gd)
        {

            spriteSheet = new Texture2D(gd, 8 * 16, 8 * 4 * 4);
            // Set some constants
            {
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
                Colors[0] = BackGroundColor;
            }

            // Update sprite sheet
            {
                int dataW = 8 * 16;
                int dataSize = 8 * 8 * 16 * 4 * 4;
                Color[] colorData = new Color[dataSize];

                // Find sprite index;
                int startIndex = 0;
                int endIndex = 0;

                for (int i = 0; i < allLines.Length; i++)
                {
                    string line = allLines[i];
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
                    string line = allLines[startIndex + i];

                    for (int j = 0; j < line.Length; j++)
                    {
                        string numStr = line.Substring(j, 1);
                        byte num = Convert.ToByte(numStr, 16);
                        colorData[i * dataW + j] = Colors[num];
                    }
                }

                spriteSheet.SetData<Color>(colorData, 0, dataSize);

                // Get tab count
                TabCount = (endIndex - startIndex + 1) / (8 * 4);
                // Console.WriteLine("tab count: " + TabCount);
            }
        }
    }


    public class LevelManager
    {
        static GraphicsDevice graphicsDevice;
        const int SprTabPixH = 8 * 4;

        const int MapSprW = 16 * 8;

        const float scrollSens = 0.1f;
        public static int DrawOffsetY = 0;

        public const int EditorWInLevel = 4;
        const int SprDrawW = Game1.ScreenW/(EditorWInLevel * 16);

        static string path = @"/Users/ljm/Library/Application Support/pico-8/carts/portal.p8";
        static byte[] mapData = new byte[16 * 16 * 32]; // Leave empty space if data in file is smaller
        static byte[] allChunks = new byte[16 * 16 * 32];
        static string[] allLines = { };
        
        // Section
        struct Section
        {
            public int HeaderIndex;
            public int BeginIndex;
            public int EndIndex;
            public int Count;
            public bool IsExist;
        }

        static int GetEndOfFile(string[] lines)
        {
            return lines.Length - 1;
        }


        static Section GetSection(string[] allLines, string name) // [index, count]
        {
            Section section;
            section.IsExist = false;
            string header = "__" + name + "__";

            int beginIndex = 0;
            int endIndex = 0;
            int endOfFile = GetEndOfFile(allLines);

            int i = 0;
            for( ; i < allLines.Length; i++)
            {
                var line = allLines[i];
                if (line.Equals(header))
                {
                    beginIndex = i + 1;
                    section.IsExist = true;
                    continue;
                }
                if (beginIndex != 0 && line.Length != allLines[beginIndex].Length)
                {
                    endIndex = i - 1;
                    break;
                }
            }
            if (endIndex == 0)
            {
                endIndex = i - 1;
            }

            // Asign result
            section.HeaderIndex = beginIndex - 1;
            section.BeginIndex = beginIndex;
            section.EndIndex = endIndex;
            section.Count = endIndex - beginIndex + 1;

            // When next line after header is another header or out of file
            // it means this section is empty
            {
                int nextIndex = beginIndex + 1;
                if (nextIndex >= endOfFile || allLines[nextIndex].Contains('_'))
                {
                    section.Count = 0;
                    section.BeginIndex = section.EndIndex = 0;
                }
            }

            return section;
        }

        static Section GetGfxMapSection(string[] allLines)
        {
            Section section;
            section.IsExist = false;

            int start = 0;
            int end = 0;

            {
                int i = 0;
                for (; i < allLines.Length; i++)
                {
                    string line = allLines[i];
                    if (line.Equals("__gfx__"))
                    {
                        start = i + 1 + SprTabPixH * 2;
                        section.IsExist = true;
                        continue;
                    }
                    if (start != 0 && line.Length != allLines[start].Length)
                    {
                        end = i - 1;
                        break;
                    }
                }
                if (end == 0)
                {
                    end = i - 1;
                }
            }

            section.HeaderIndex = start - 1;
            section.BeginIndex = start;
            section.EndIndex = end;
            section.Count = end - start + 1;

            return section;
        }

        // Get raw lined data

        static byte[] GetDataInMap()
        {
            byte[] dataInMap = new byte[16 * 16 * 8 * 2];

            var section = GetSection(allLines, "map");
            
            var mapLines = allLines[section.BeginIndex..(section.EndIndex + 1)];

            // Convert lines to byte array
            for (int i = 0; i < mapLines.Length; i++)
            {
                string line = mapLines[i];
                for (int j = 0; j < line.Length / 2; j++)
                {
                    string pair = line.Substring(j * 2, 2);
                    byte spr = Convert.ToByte(pair, 16);
                    dataInMap[i * 16 * 8 + j] = spr;
                }
            }
            return dataInMap;
        }

        static byte[] GetDataInGfx()
        {
            byte[] data = { };
            int tabCount = Spr.TabCount;
            if (tabCount <= 2)
            {
                return data;
            }

            string[] gfxLines = { };

            int start = 0;
            int end = 0;

            {
                int i = 0;
                for (; i < allLines.Length; i++)
                {
                    string line = allLines[i];
                    if (line.Equals("__gfx__"))
                    {
                        start = i + 1 + SprTabPixH * 2;
                        continue;
                    }
                    if (start != 0 && line.Length != allLines[start].Length)
                    {
                        end = i - 1;
                        break;
                    }
                }
                if (end == 0)
                {
                    end = i - 1;
                }
            }

            gfxLines = allLines[start..(end + 1)];

            Array.Resize<byte>(ref data, gfxLines.Length * gfxLines[0].Length / 2);

            for (int i = 0; i < gfxLines.Length; i++)
            {
                string line = gfxLines[i];
                for (int j = 0; j < line.Length / 2; j++)
                {
                    string pair = line.Substring(j * 2, 2);
                    string pairChar0 = pair.Substring(0, 1);
                    string pairChar1 = pair.Substring(1, 1);
                    byte num0 = Convert.ToByte(pairChar0, 16);
                    byte num1 = Convert.ToByte(pairChar1, 16);
                    byte num = (byte)(num0 + num1 * 16);
                    data[i * line.Length / 2 + j] = num;
                }
            }

            return data;
        }

        static byte[] GetMapData()
        {
            var dataInMap = GetDataInMap();
            var dataInGfx = GetDataInGfx();
            byte[] wholeData = new byte[16 * 16 * 32]; // Leave empth space at the end to make sure it's fixed size
            Array.Copy(dataInMap, wholeData, dataInMap.Length);
            Array.Copy(dataInGfx, 0, wholeData, dataInMap.Length, dataInGfx.Length);
            return wholeData;
        }

        public static byte[] GetChunkedMapData(byte[] mapData)
        {
            byte[] data = new byte[mapData.Length];

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int beginIndex = r * MapSprW * 16 + c * 16;
                    byte[] chunk = new byte[16 * 16];
                    for (int i = 0; i < 16; i++)
                    {
                        Array.Copy(mapData, beginIndex + i * MapSprW, chunk, i * 16, 16);
                    }
                    Array.Copy(chunk, 0, data, (r * 8 + c) * 16 * 16, chunk.Length);
                }
            }

            return data;
        }

        public static byte[] GetChunk(byte[] chunkedData, int index)
        {
            byte[] chunk = new byte[16 * 16];
            Array.Copy(chunkedData, index * 16 * 16, chunk, 0, chunk.Length);
            return chunk;
        }

        public static void LoadFile()
        {
            // Read file data to variables
            // Check file completeness
            bool isComplete = true;
            allLines = File.ReadAllLines(path);

            while (!isComplete)
            {
                isComplete = true;
                allLines = File.ReadAllLines(path);

                var gfxSection = GetSection(allLines, "gfx");
                var mapSection = GetSection(allLines, "map");

                if (!gfxSection.IsExist)
                {
                    isComplete = false;
                    Console.WriteLine("File lack gfx section.");
                }
                if (!mapSection.IsExist)
                {
                    isComplete = false;
                    Console.WriteLine("File lack map section.");
                }
            }

            Spr.Init(allLines, graphicsDevice);
            mapData = GetMapData();
            allChunks = GetChunkedMapData(mapData);

            FileChangeChecker.OnLoadingFile(path);
            // Console.WriteLine("File load success.");
        }

        // Render
        static void PrintMapData(string name, byte[] data)
        {
            Console.WriteLine(name + ": ");
            int sprH = data.Length / MapSprW;

            for(int r = 0; r < data.Length / MapSprW; r++)
            {
                for(int c = 0; c < MapSprW; c++)
                {
                    Console.Write(data[r * MapSprW + c] + ", ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static void DrawMapDataStrechedOut(SpriteBatch batch, byte[] data)
        {
            for(int r = 0; r < data.Length / MapSprW; r++)
            {
                for (int c = 0; c < MapSprW; c++)
                {
                    var spr = data[r * MapSprW + c];
                    Spr.DrawSprite(batch, spr, c, r, Game1.ScreenW/(16 * 8));
                }
            }
        }

        static void DrawChunk(SpriteBatch batch, byte[] chunk, int sx, int sy)
        {
            for(int y = 0; y < 16; y++)
            {
                for(int x = 0; x < 16; x++)
                {
                    var spr = chunk[y * 16 + x];
                    Spr.DrawSprite(batch, spr,
                        new Rectangle(sx + x * SprDrawW, sy + y * SprDrawW + DrawOffsetY, SprDrawW, SprDrawW));
                }
            }
        }
        
        static void DrawAllChunks(SpriteBatch batch, byte[] chunkedData)
        {
            for(int i = 0; i < 32; i++)
            {
                var chunk = GetChunk(chunkedData, i);
                var x = i % EditorWInLevel;
                var y = (i - x) / EditorWInLevel;
                x *= SprDrawW * 16;
                y *= SprDrawW * 16;
                DrawChunk(batch, chunk, x, y);
            }
            
        }

        public static void Reorder(int pickI, int toI)
        {
            if (pickI == toI || pickI < 0 || pickI >= 32 || toI < 0 || toI >= 32)
            {
                return;
            }

            byte[] newChunks = new byte[allChunks.Length];
            Array.Copy(allChunks, newChunks, allChunks.Length);


            int chunkLen = 16 * 16;
            int moveLen = Math.Abs(pickI - toI) * chunkLen;


            if (pickI > toI)
            {
                Array.Copy(allChunks, toI * chunkLen, newChunks, (toI + 1) * chunkLen, moveLen);
            }
            if (pickI < toI)
            {
                Array.Copy(allChunks, (pickI + 1) * chunkLen, newChunks, pickI * chunkLen, moveLen);
            }


            Array.Copy(allChunks, pickI * chunkLen, newChunks, toI * chunkLen, chunkLen);

            Array.Copy(newChunks, allChunks, allChunks.Length);

            // Auto save after reordering
            SaveToFile();
        }

        public static void ClearChunk(int index)
        {
            Array.Clear(allChunks, index * 16 * 16, 16 * 16);
        }

        public static void DeleteChunk(int i)
        {
            if (i >= 31)
            {
                return;
            }
            const int chunkSize = 16 * 16;
            // Move all chunks behind one chunk forward
            Array.Copy(allChunks, (i + 1) * chunkSize, allChunks, i * chunkSize, chunkSize * (32 - i - 1));
            // Compensate zeroes at the end
            ClearChunk(31);
            SaveToFile();
        }

        // Functions for saving to file
        static byte[] GetRowInChunk(byte[] chunk, int r)
        {
            byte[] row = new byte[16];
            Array.Copy(chunk, r * 16, row, 0, 16);
            return row;
        }

        static byte[] GetStraightChunks()
        {
            byte[] straight = { };
            for (int i = 0; i < 4; i++)
            {
                for (int r = 0; r < 16; r++)
                {
                    for (int j = 0; j < 8; j++) // Chunks in a row
                    {
                        var chunk = GetChunk(allChunks, i * 8 + j);
                        var row = GetRowInChunk(chunk, r);
                        Helper.ConcatArray(ref straight, row);
                    }
                }
            }

            return straight;
        }

        static void InsertSection(ref List<string> dst, Section section, string[] data)
        {
            var endOfFile = GetEndOfFile(dst.ToArray());
            if (section.HeaderIndex == endOfFile)
            {
                dst.AddRange(data);
            }
            else
            {
                dst.InsertRange(section.HeaderIndex + 1, data);
            }
        }

        public static void SaveToFile() // Save and load file at the same time
        {
            List<string> wholeLinesBuffer = new List<string>(allLines);

            var straightData = GetStraightChunks();

            // Remove sections
            {
                var gfxMapSection = GetGfxMapSection(wholeLinesBuffer.ToArray());
                wholeLinesBuffer.RemoveRange(gfxMapSection.BeginIndex, gfxMapSection.Count);
                var mapSection = GetSection(wholeLinesBuffer.ToArray(), "map");
                wholeLinesBuffer.RemoveRange(mapSection.BeginIndex, mapSection.Count);

            }
            // Replace map section in __gfx__
            {
                var gfxStraightData = straightData[(straightData.Length/2)..straightData.Length];
                string gfxBuffer = "";
                // Convert every numbers to hex string pair and append them to gfx lines buffer
                for(int i = 0; i < gfxStraightData.Length; i++)
                {
                    byte num = gfxStraightData[i];
                    string binStrRead = Convert.ToString(num, 2);
                    char[] binStrBuffer = "00000000".ToCharArray();
                    Array.Copy(binStrRead.ToCharArray(), 0, binStrBuffer, 8 - binStrRead.Length, binStrRead.Length);
                    var binStr = new string(binStrBuffer);
                    string bStr0 = binStr.Substring(0, binStr.Length/2);
                    string bStr1 = binStr.Substring(binStr.Length/2, binStr.Length / 2);
                    byte num0 = Convert.ToByte(bStr0, 2);
                    byte num1 = Convert.ToByte(bStr1, 2);
                    string numHex0 = Convert.ToString(num0, 16);
                    string numHex1 = Convert.ToString(num1, 16);
                    string pair = (numHex1 + numHex0);
                    gfxBuffer += pair;
                }
                // Split straight gfx lines buffer into rows of correct length
                gfxBuffer = gfxBuffer.ToLower();
                var gfxLines = gfxBuffer.SplitIntoLines(8 * 16);

                // Remove section in buffer
                var gfxMapSection = GetGfxMapSection(allLines);

                // Insert data into whole lines buffer
                wholeLinesBuffer.InsertRange(gfxMapSection.BeginIndex, gfxLines);
            }

            // Replace map section in __map__
            {
                // Get new map data
                var mapStraightData = straightData[0..(1 + straightData.Length/2)];
                var straightString = BitConverter.ToString(mapStraightData).Filter('-');
                straightString = straightString.ToLower();
                var mapLines = straightString.SplitIntoLines(16 * 8 * 2);
                // Insert new section data
                InsertSection(ref wholeLinesBuffer, GetSection(wholeLinesBuffer.ToArray(), "map"), mapLines);
            }
            // Replace all lines in file
            File.WriteAllLines(path, wholeLinesBuffer.ToArray());

            // Reload file
            LoadFile();
            // Console.WriteLine("saved");
        }

        public static string CheckPath(string path)
        {
            if (!path.Contains(".p8"))
            {
                path += ".p8";
            }
            path = path.Replace("\\", String.Empty);
            return path;
        }

        public static void Init(GraphicsDevice gd)
        {
            // Ask for path
            if (!File.Exists(path))
            {
                Console.WriteLine("Enter file path:");
                path = Console.ReadLine();
                path = CheckPath(path);
            }
            
            while (!File.Exists(path))
            {
                Console.WriteLine("File does not exist, enter again:");
                path = Console.ReadLine();
                path = CheckPath(path);
            }

            graphicsDevice = gd;
            LoadFile();
        }

        public static void Update()
        {
            // Scroll
            {
                MouseWheel.Update();
                DrawOffsetY += (int)(MouseWheel.increment * scrollSens);

                // Clamp
                if (DrawOffsetY > 0)
                {
                    DrawOffsetY = 0;
                }
                if (DrawOffsetY < -SprDrawW * 16 * 32 / (EditorWInLevel * 2))
                {
                    DrawOffsetY = -SprDrawW * 16 * 32 / (EditorWInLevel * 2);
                }
            }
            // Mouse input
            Click.Update();

            // Save
            var ks = Keyboard.GetState();
            if (ks.IsKeyDown(Keys.S))
            {
                SaveToFile();
            }

            // Reload file when file is changed externally
            if (FileChangeChecker.FileChanged())
            {
                Thread.Sleep(10);
                LoadFile();
            }
        }

        public static void Draw(SpriteBatch batch)
        {
            DrawAllChunks(batch, allChunks);
            //DrawMapDataStrechedOut(batch, GetStraightChunks());
            Helper.DrawGridLine(batch, 0, DrawOffsetY, EditorWInLevel, 30, Game1.ScreenW, Color.White);
        }
            
    }

    public static class FileChangeChecker
    {
        static string path;
        static byte[] tempBytes;

        public static void OnLoadingFile(string _path)
        {
            path = _path;
            tempBytes = File.ReadAllBytes(path);
        }

        public static bool FileChanged()
        {
            var originLines = File.ReadAllBytes(path);
            return !originLines.SequenceEqual(tempBytes);
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
        public static bool IsPressing = false;

        static int screenW = Game1.ScreenW;
        static int editorW = LevelManager.EditorWInLevel;

        static KeyboardState lastKeyState;

        public static bool IsJustPressed(Keys key)
        {
            var ks = Keyboard.GetState();
            return lastKeyState.IsKeyUp(key) && ks.IsKeyDown(key);
        }

        public static Point GetChunkGPosByMouse()
        {
            var ms = Mouse.GetState();
            int size = screenW / editorW;

            Point mousePos = ms.Position + new Point(0, -LevelManager.DrawOffsetY);

            Point gridPos = new Point((int)Math.Floor((float)mousePos.X / size),
                (int)Math.Floor((float)mousePos.Y / size));

            return gridPos;
        }

        static int GetChunkIndexByMouse()
        {
            Point gPos = GetChunkGPosByMouse();
            return gPos.Y * editorW + gPos.X;
        }

        public static void Update()
        {
            var ms = Mouse.GetState();
            var ks = Keyboard.GetState();

            var toChunk = GetChunkIndexByMouse();

            // On pressing
            if (!IsPressing && ms.LeftButton == ButtonState.Pressed)
            {
                IsPressing = true;
                pickedChunk = GetChunkIndexByMouse();
                // Console.WriteLine("picked chunk: " + pickedChunk);
            }
            // On holding
            if (IsPressing && pickedChunk != toChunk)
            {
                LevelManager.Reorder(pickedChunk, toChunk);
                pickedChunk = toChunk;
            }
            // On releasing
            if (IsPressing && ms.LeftButton != ButtonState.Pressed)
            {
                IsPressing = false;
                // Console.WriteLine("to chunk: " + toChunk);
            }

            // Pressing key
            if (IsJustPressed(Keys.Back))
            {
                // LevelManager.ClearChunk(toChunk);
                LevelManager.DeleteChunk(toChunk);
            }
            if (IsJustPressed(Keys.R))
            {
                LevelManager.LoadFile();
            }
            // Last frame thing
            lastKeyState = Keyboard.GetState();
        }
    }

    public static class Helper
    {
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

        public static void ConcatArray(ref byte[] arr, byte[] cat)
        {
            int originalSize = arr.Length;
            Array.Resize<byte>(ref arr, arr.Length + cat.Length);
            Array.Copy(cat, 0, arr, originalSize, cat.Length);
        }

        public static string Filter(this string str, char c)
        {
            return str.Replace(c.ToString(), string.Empty);
        }

        public static string[] SplitIntoLines(this string str, int lineLen)
        {
            List<string> lines = new List<string>();

            for(int i = 0; i < str.Length / lineLen; i++)
            {
                var line = str.Substring(i * lineLen, lineLen);
                lines.Add(line);
            }
            
            return lines.ToArray();
        }

    }
}

