using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rampastring.Tools;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Globalization;
using System.Drawing;

namespace DrawScripts
{
    class Script{
        public string id;
        public string Name;
        public List<int> SWPlist = new List<int>();
        public List<int> X = new List<int>();
        public List<int> Y = new List<int>();
        public List<string> action = new List<string>();
        public bool hasInvalidWP = false;
    }
    public class Waypoint
    {
        public int X;
        public int Y;
        public int Index;
        public bool OnMap = true;

        public void Initialize(KeyValuePair<string, string> iniLine)
        {
            string value = iniLine.Value;
            int length = value.Length;
            string x = value.Substring(value.Length - 3, 3);
            string y = value.Substring(0, value.Length - 3);
            Index = int.Parse(iniLine.Key);
            X = int.Parse(x);
            Y = int.Parse(y);
            //RelativeX = int.Parse(x) - WorkingMap.StartingX;
            //RelativeY = int.Parse(y) - WorkingMap.StartingY;
        }

        public Waypoint Clone()
        {
            return (Waypoint)this.MemberwiseClone();
        }
        public KeyValuePair<string, string> CreateINILine()
        {
            string value = Y.ToString() + string.Format("{0:000}", X);
            var iniLine = new KeyValuePair<string, string>(Index.ToString(), value);
            return iniLine;
        }
    }
    class MapFile
    {
        public int Width;
        public int Height;
        public List<IsoTile> IsoTileList;
        public List<Overlay> OverlayList;
        public List<Waypoint> WPlist = new List<Waypoint>();
        public List<Script> Scriptlist = new List<Script>();

        /*public IniSection Unit = new IniSection("Units");
        public IniSection Infantry = new IniSection("Infantry");
        public IniSection Structure = new IniSection("Structures");
        public IniSection Terrain = new IniSection("Terrain");
        public IniSection Aircraft = new IniSection("Aircraft");
        public IniSection Smudge = new IniSection("Smudge");
        public IniSection Waypoint = new IniSection("Waypoints");*/
        public int[] LocalSize = new int[2];
        bool IsOnMapAT(int x, int y)
        {
            bool isOnMap = false;
            if (y + x > Width
                && x + y < 2 * Height + Width + 1
                && y - x < Width
                && x - y < Width)
            {
                isOnMap = true;
            }
            return isOnMap;
        }
        public void ReadWP()
        {
            foreach (var kvp in Program.MAP_FILE.GetSection("Waypoints").Keys)
            {
                var wp = new Waypoint();
                wp.Initialize(kvp);
                WPlist.Add(wp);
            }
            foreach (var tile in IsoTileList)
            { 
                foreach (var wp in WPlist)
                {
                    if (tile.Rx == wp.X && tile.Ry == wp.Y)
                    {
                        tile.hasWP = true;
                        tile.WPindex = wp.Index;
                    }
                }
            }
            foreach (var wp in WPlist)
            {
                if (!IsOnMapAT(wp.X, wp.Y))
                {
                    wp.OnMap = false;
                }
            }
        }
        public void CreateBitMapbyMap(string filename)
        {
            var srcBitmap = new Bitmap(Width * 2, Height);
            var waypoints = new Bitmap(Width * 2, Height);
            foreach (var tile in IsoTileList)
            {
                int x = tile.Dx;
                int y = (tile.Dy - tile.Dx % 2) / 2;
                if (x < srcBitmap.Width && y < srcBitmap.Height)
                {
                    srcBitmap.SetPixel(x, y, tile.RadarLeft);
                    waypoints.SetPixel(x, y, tile.RadarLeft);
                    if (tile.hasWP)
                        waypoints.SetPixel(x, y, Color.Red);
                }
                    
            }

            Bitmap resizedImage = new Bitmap(srcBitmap.Width * 5, srcBitmap.Height * 5);

            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                // 设置插值模式为双线性插值算法
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                // 调用 DrawImage 方法将原始图片按比例放大到目标位图上
                graphics.DrawImage(srcBitmap, 0, 0, srcBitmap.Width * 5, srcBitmap.Height * 5);
            }

            Bitmap resizedImageWP = new Bitmap(waypoints.Width * 5, waypoints.Height * 5);

            using (Graphics graphics = Graphics.FromImage(resizedImageWP))
            {
                // 设置插值模式为双线性插值算法
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                // 调用 DrawImage 方法将原始图片按比例放大到目标位图上
                graphics.DrawImage(waypoints, 0, 0, waypoints.Width * 5, waypoints.Height * 5);
            }

            foreach (var tile in IsoTileList)
            {
                int x = tile.Dx;
                int y = (tile.Dy - tile.Dx % 2) / 2;
                if (x < srcBitmap.Width && y < srcBitmap.Height)
                {
                    if (tile.hasWP)
                    {
                        using (Graphics graphics = Graphics.FromImage(resizedImageWP))
                        {
                            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                            // 设置文本样式（字体、位置等）
                            Font font = new Font("Times New Roman", 7, FontStyle.Regular);
                            Brush brush = new SolidBrush(Color.Yellow);
                            PointF point = new PointF(x * 5 + 5, y * 5 - 5);

                            // 绘制文本
                            string text = tile.WPindex.ToString();
                            graphics.DrawString(text, font, brush, point);
                        }
                    }
                }
            }

            foreach (var script in Scriptlist)
            {
                foreach (var wpi in script.SWPlist)
                {
                    foreach (var tile in IsoTileList)
                    {
                        if (tile.hasWP)
                        {
                            if (tile.WPindex == wpi)
                            {
                                script.X.Add(tile.Dx * 5 - 2);
                                script.Y.Add(((tile.Dy - tile.Dx % 2) / 2) * 5 - 2);
                            }
                        }
                    }
                }
                if (script.hasInvalidWP)
                {
                    Bitmap si = new Bitmap(resizedImage.Width, resizedImage.Height + 30);

                    using (Graphics graphics = Graphics.FromImage(si))
                    {
                        graphics.Clear(Color.White);
                        graphics.DrawImage(resizedImage, 0, 0, resizedImage.Width, resizedImage.Height);
                    }
                    using (Graphics graphics = Graphics.FromImage(si))
                    {
                        Font font2 = new Font("Times New Roman", 18, FontStyle.Bold);
                        Brush brush2 = new SolidBrush(Color.Red);
                        PointF point2 = new PointF(5, resizedImage.Height + 2);

                        // 绘制文本
                        string text2 = script.id + " <" + script.Name + "> has invalid waypoint(s):";

                        foreach (int wpi in script.SWPlist)
                        {
                            foreach (var wp in WPlist)
                            {
                                if (wp.Index == wpi && !wp.OnMap)
                                {
                                    text2 += " " + wp.Index.ToString();
                                }
                            }
                        }
                        foreach (int wpi in script.SWPlist)
                        {
                            bool meet = false;
                            foreach (var wp in WPlist)
                            {
                                if (wp.Index == wpi)
                                {
                                    meet = true;
                                }
                            }
                            if (!meet)
                                text2 += " " + wpi.ToString();
                        }
                        graphics.DrawString(text2, font2, brush2, point2);
                    }

                    si.Save(Program.MAP_NAME + "_" + script.id + ".bmp");
                }
                
                else if (script.X.Count > 1)
                {
                    Bitmap si = new Bitmap(resizedImage.Width, resizedImage.Height + 30);

                    using (Graphics graphics = Graphics.FromImage(si))
                    {
                        graphics.Clear(Color.White);
                        graphics.DrawImage(resizedImage, 0, 0, resizedImage.Width, resizedImage.Height);
                    }
                    using (Graphics graphics = Graphics.FromImage(si))
                    {
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                        System.Drawing.Drawing2D.AdjustableArrowCap lineCap = new System.Drawing.Drawing2D.AdjustableArrowCap(6, 6, true);
                        Pen redArrowPen = new Pen(Color.Red, 4);
                        redArrowPen.CustomEndCap = lineCap;
                        for (int i = 0; i < script.X.Count - 1; i++)
                        {
                            graphics.DrawLine(redArrowPen, script.X[i], script.Y[i], script.X[i + 1], script.Y[i + 1]);
                        }
                        for (int i = 0; i < script.X.Count; i++)
                        {
                            // 设置文本样式（字体、位置等）
                            Font font = new Font("Times New Roman", 15, FontStyle.Regular);
                            Brush brush = new SolidBrush(Color.Yellow);
                            PointF point = new PointF(script.X[i] + 7, script.Y[i] - 3);

                            // 绘制文本
                            string text = script.action[i];
                            graphics.DrawString(text, font, brush, point);
                        }
                        Font font2 = new Font("Times New Roman", 18, FontStyle.Bold);
                        Brush brush2 = new SolidBrush(Color.Black);
                        PointF point2 = new PointF(5, resizedImage.Height + 2);

                        // 绘制文本
                        string text2 = script.id + " <" + script.Name + ">";
                        graphics.DrawString(text2, font2, brush2, point2);
                    }

                    si.Save(Program.MAP_NAME + "_" + script.id + ".bmp");
                }

            }

            //srcBitmap.Save(filename + ".bmp");
            resizedImageWP.Save(filename + ".bmp");
        }

        public void CreateRadarColor()
        {
            var tileList = new List<IsoTile>();
            int range = Width + Height;

            var minimapIni = new IniFile("minimap.ini");
            var section = minimapIni.GetSection(Program.MAP_FILE.GetStringValue("Map", "Theater", "NEWURBAN"));

            foreach (var tile in IsoTileList)
            {
                var defaultv = section.GetStringValue("0", "255,255,255,255,255,255");
                var value = section.GetStringValue(tile.TileNum.ToString(), defaultv);
                var colorcombos = value.Split('/');

                int index = tile.SubTile;
                if (index >= colorcombos.Count())
                    index = 0;
                var colorcombo = colorcombos[index];
                var rgbs = colorcombo.Split(',');
                tile.RadarLeft = Color.FromArgb(SafeColorInt(int.Parse(rgbs[0])), SafeColorInt(int.Parse(rgbs[1])), SafeColorInt(int.Parse(rgbs[2])));
                tile.RadarRight = Color.FromArgb(SafeColorInt(int.Parse(rgbs[3])), SafeColorInt(int.Parse(rgbs[4])), SafeColorInt(int.Parse(rgbs[5])));


            }

        }

        public void ReadScript()
        {
            List<string> scripts = new List<string>();
            if (Program.MAP_FILE.SectionExists("ScriptTypes"))
            {
                foreach (var kvp in Program.MAP_FILE.GetSection("ScriptTypes").Keys)
                {
                    scripts.Add(kvp.Value);
                }
                foreach (var script in scripts)
                {
                    var section = Program.MAP_FILE.GetSection(script);
                    bool add = false;
                    int index = 0;
                    while (index < 50)
                    {
                        var action = section.GetStringValue(index.ToString(), "0,0");
                        int act1 = int.Parse(action.Split(',')[0]);
                        int act2 = int.Parse(action.Split(',')[1]);
                        switch (act1)
                        {
                            case 1:
                                add = true;
                                break;

                            case 3:
                                add = true;
                                break;

                            case 15:
                                add = true;
                                break;

                            case 16:
                                add = true;
                                break;

                            case 59:
                                add = true;
                                break;

                            default:
                                break;
                        }
                        index++;
                    }
                    if (add)
                    {
                        index = 0;
                        var s = new Script();
                        while (index < 50)
                        {
                            s.id = script;
                            var action = section.GetStringValue(index.ToString(), "0,0");
                            int act1 = int.Parse(action.Split(',')[0]);
                            int act2 = int.Parse(action.Split(',')[1]);

                            switch (act1)
                            {
                                case 1:
                                    s.SWPlist.Add(act2);
                                    s.action.Add("1, Attack waypoint");
                                    break;

                                case 3:
                                    s.SWPlist.Add(act2);
                                    s.action.Add("3, Move to waypoint");
                                    break;

                                case 15:
                                    s.SWPlist.Add(act2);
                                    s.action.Add("15, Spy on structure at waypoint");
                                    break;

                                case 16:
                                    s.SWPlist.Add(act2);
                                    s.action.Add("16, Patrol to waypoint");
                                    break;

                                case 59:
                                    s.SWPlist.Add(act2);
                                    s.action.Add("59, Attack structure at waypoint");
                                    break;

                                default:
                                    break;
                            }
                            index++;
                        }
                        foreach (int wpi in s.SWPlist)
                        {
                            foreach (var wp in WPlist)
                            {
                                if (wp.Index == wpi && !wp.OnMap)
                                {
                                    s.hasInvalidWP = true;
                                }
                            }
                        }
                        foreach (int wpi in s.SWPlist)
                        {
                            bool meet = false;
                            foreach (var wp in WPlist)
                            {
                                if (wp.Index == wpi)
                                {
                                    meet = true;
                                }
                            }
                            if (!meet)
                                s.hasInvalidWP = true;
                        }

                        s.Name = section.GetStringValue("Name", "no name");
                        Scriptlist.Add(s);
                    }
                }
            }
        }
        public static int SafeColorInt(int x)
        {
            if (x > 255)
                x = 255;
            if (x < 0)
                x = 0;
            return x;
        }
        public void ReadIsoMapPack5(string filePath)
        {
            var MapFile = new IniFile(filePath);
            var MapPackSections = MapFile.GetSection("IsoMapPack5");
            var MapSize = MapFile.GetStringValue("Map", "Size", "0,0,0,0");
            string IsoMapPack5String = "";

            int sectionIndex = 1;
            while (MapPackSections.KeyExists(sectionIndex.ToString()))
            {
                IsoMapPack5String += MapPackSections.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            string[] sArray = MapSize.Split(',');
            Width = Int32.Parse(sArray[2]);
            Height = Int32.Parse(sArray[3]);
            int cells = (Width * 2 - 1) * Height;
            IsoTile[,] Tiles = new IsoTile[Width * 2 - 1, Height];//这里值得注意
            byte[] lzoData = Convert.FromBase64String(IsoMapPack5String);

            //Log.Information(cells);
            int lzoPackSize = cells * 11 + 4;
            var isoMapPack = new byte[lzoPackSize];
            uint totalDecompressSize = Format5.DecodeInto(lzoData, isoMapPack);//TODO 源，目标 输入应该是解码后长度，isoMapPack被赋值解码值了
                                                                               //uint	0 to 4,294,967,295	Unsigned 32-bit integer	System.UInt32
            var mf = new MemoryFile(isoMapPack);

            //Log.Information(BitConverter.ToString(lzoData));
            int count = 0;
            //List<List<IsoTile>> TilesList = new List<List<IsoTile>>(Width * 2 - 1);
            IsoTileList = new List<IsoTile>();
            //Log.Information(TilesList.Capacity);
            for (int i = 0; i < cells; i++)
            {
                ushort rx = mf.ReadUInt16();//ushort	0 to 65,535	Unsigned 16-bit integer	System.UInt16
                ushort ry = mf.ReadUInt16();
                short tilenum = mf.ReadInt16();//short	-32,768 to 32,767	Signed 16-bit integer	System.Int16
                short zero1 = mf.ReadInt16();//Reads a 2-byte signed integer from the current stream and advances the current position of the stream by two bytes.
                byte subtile = mf.ReadByte();//Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
                byte z = mf.ReadByte();
                byte zero2 = mf.ReadByte();

                count++;
                int dx = rx - ry + Width - 1;

                int dy = rx + ry - Width - 1;
                //Log.Information("{1}", rx, ry, tilenum, subtile, z, dx, dy,count);
                //上面是一个线性变换 旋转45度、拉长、平移
                if (dx >= 0 && dx < 2 * Width &&
                    dy >= 0 && dy < 2 * Height)
                {
                    var tile = new IsoTile((ushort)dx, (ushort)dy, rx, ry, z, tilenum, subtile);//IsoTile定义是NumberedMapObject

                    Tiles[(ushort)dx, (ushort)dy / 2] = tile;//给瓷砖赋值
                    IsoTileList.Add(tile);
                }
            }
            //用来检查有没有空着的
            for (ushort y = 0; y < Height; y++)
            {
                for (ushort x = 0; x < Width * 2 - 1; x++)
                {
                    var isoTile = Tiles[x, y];//从这儿来看，isoTile指的是一块瓷砖，Tile是一个二维数组，存着所有瓷砖
                                              //isoTile的定义在TileLayer.cs里
                    if (isoTile == null)
                    {
                        // fix null tiles to blank
                        ushort dx = (ushort)(x);
                        ushort dy = (ushort)(y * 2 + x % 2);
                        ushort rx = (ushort)((dx + dy) / 2 + 1);
                        ushort ry = (ushort)(dy - rx + Width + 1);
                        Tiles[x, y] = new IsoTile(dx, dy, rx, ry, 0, 0, 0);
                    }
                }

            }
        }

        public void SaveIsoMapPack5(string path)
        {
            long di = 0;
            int cells = (Width * 2 - 1) * Height;
            int lzoPackSize = cells * 11 + 4;
            var isoMapPack2 = new byte[lzoPackSize];
            foreach (var tile in IsoTileList)
            {
                var bs = tile.ToMapPack5Entry().ToArray();//ToMapPack5Entry的定义在MapObjects.cs
                                                          //ToArray将ArrayList转换为Array：
                Array.Copy(bs, 0, isoMapPack2, di, 11);//把bs复制给isoMapPack,从di索引开始复制11个字节
                di += 11;//一次循环复制11个字节
            }

            var compressed = Format5.Encode(isoMapPack2, 5);

            string compressed64 = Convert.ToBase64String(compressed);
            int j = 1;
            int idx = 0;

            var saveFile = new IniFile(path);

            if (saveFile.SectionExists("IsoMapPack5"))
                saveFile.RemoveSection("IsoMapPack5");

            saveFile.AddSection("IsoMapPack5");
            var saveMapPackSection = saveFile.GetSection("IsoMapPack5");

            while (idx < compressed64.Length)
            {
                int adv = Math.Min(74, compressed64.Length - idx);//74 is the length of each line
                saveMapPackSection.SetStringValue(j.ToString(), compressed64.Substring(idx, adv));
                j++;
                idx += adv;//idx=adv+1
            }
            saveFile.WriteIniFile();
        }
        public void SaveWorkingMapPack(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var mapPack = new IniFile(path);
            mapPack.AddSection("mapPack");
            var mapPackSection = mapPack.GetSection("mapPack");
            int mapPackIndex = 1;
            mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");
            mapPack.SetStringValue("Map", "Size", Width.ToString() + "," + Height.ToString());

            for (int i = 0; i < IsoTileList.Count; i++)
            {
                var isoTile = IsoTileList[i];
                mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                       isoTile.Dx.ToString() + "," +
                       isoTile.Dy.ToString() + "," +
                       isoTile.Rx.ToString() + "," +
                       isoTile.Ry.ToString() + "," +
                       isoTile.Z.ToString() + "," +
                       isoTile.TileNum.ToString() + "," +
                       isoTile.SubTile.ToString());
            }
            mapPack.WriteIniFile();
        }

        public void SaveWorkingMapPack2(string path, List<IsoTile> IsoTileList2)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var mapPack = new IniFile(path);
            mapPack.AddSection("mapPack");
            var mapPackSection = mapPack.GetSection("mapPack");
            int mapPackIndex = 1;
            mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");
            mapPack.SetStringValue("Map", "Size", Width.ToString() + "," + Height.ToString());

            for (int i = 0; i < IsoTileList2.Count; i++)
            {

                var isoTile = IsoTileList2[i];
                if (isoTile.Used)
                {
                    mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                       isoTile.Dx.ToString() + "," +
                       isoTile.Dy.ToString() + "," +
                       isoTile.Rx.ToString() + "," +
                       isoTile.Ry.ToString() + "," +
                       isoTile.Z.ToString() + "," +
                       isoTile.TileNum.ToString() + "," +
                       isoTile.SubTile.ToString());
                }
            }
            mapPack.WriteIniFile();
        }


        /*public void SaveFullMap(string path)
        {
            var fullMap = new IniFile(Program.TemplateMap);
            var settings = new IniFile(Program.WorkingFolder + "settings.ini");
            var bottomSpace = settings.GetIntValue("settings", "BottomSpace", 4);
            fullMap.SetStringValue("Map", "Size", "0,0," + Width.ToString() + "," + Height.ToString());
            LocalSize[0] = Width - 4;
            LocalSize[1] = Height - 11 + 4 - bottomSpace;
            fullMap.SetStringValue("Map", "LocalSize", "2,5," + LocalSize[0].ToString() + "," + LocalSize[1].ToString());
            fullMap.SetStringValue("Map", "Theater", Enum.GetName(typeof(Theater), MapTheater));
            if (Unit != null)
                fullMap.AddSection(Unit);
            if (Infantry != null)
                fullMap.AddSection(Infantry);
            if (Structure != null)
                fullMap.AddSection(Structure);
            if (Terrain != null)
                fullMap.AddSection(Terrain);
            if (Aircraft != null)
                fullMap.AddSection(Aircraft);
            if (Smudge != null)
                fullMap.AddSection(Smudge);
            if (Waypoint != null)
                fullMap.AddSection(Waypoint);
            fullMap.WriteIniFile(path);
            SaveIsoMapPack5(path);
            SaveOverlay(path);
        }*/
        public void LoadWorkingMapPack(string path)
        {
            IsoTileList = new List<IsoTile>();
            var mapPack = new IniFile(path);
            var mapPackSection = mapPack.GetSection("mapPack");
            string[] size = mapPack.GetStringValue("Map", "Size", "0,0").Split(',');
            Width = int.Parse(size[0]);
            Height = int.Parse(size[1]);

            int i = 1;
            while (mapPackSection.KeyExists(i.ToString()))
            {
                if (mapPackSection.KeyExists(i.ToString()))
                {
                    string[] isoTileInfo = mapPackSection.GetStringValue(i.ToString(), "").Split(',');
                    var isoTile = new IsoTile(ushort.Parse(isoTileInfo[0]),
                        ushort.Parse(isoTileInfo[1]),
                        ushort.Parse(isoTileInfo[2]),
                        ushort.Parse(isoTileInfo[3]),
                        (byte)int.Parse(isoTileInfo[4]),
                        short.Parse(isoTileInfo[5]),
                        (byte)int.Parse(isoTileInfo[6]));
                    IsoTileList.Add(isoTile);
                    i++;
                }
            }
        }

        public List<Overlay> ReadOverlay(string path)
        {
            OverlayList = new List<Overlay>();
            var mapFile = new IniFile(path);
            if (!mapFile.SectionExists("OverlayPack") || !mapFile.SectionExists("OverlayDataPack"))
                return null;
            IniSection overlaySection = mapFile.GetSection("OverlayPack");
            if (overlaySection == null)
                return null;

            string OverlayPackString = "";
            int sectionIndex = 1;
            while (overlaySection.KeyExists(sectionIndex.ToString()))
            {
                OverlayPackString += overlaySection.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            byte[] format80Data = Convert.FromBase64String(OverlayPackString);
            var overlayPack = new byte[1 << 18];
            Format5.DecodeInto(format80Data, overlayPack, 80);

            IniSection overlayDataSection = mapFile.GetSection("OverlayDataPack");
            if (overlayDataSection == null)
                return null;

            string OverlayDataPackString = "";
            sectionIndex = 1;
            while (overlayDataSection.KeyExists(sectionIndex.ToString()))
            {
                OverlayDataPackString += overlayDataSection.GetStringValue(sectionIndex.ToString(), "");
                sectionIndex++;
            }

            format80Data = Convert.FromBase64String(OverlayDataPackString);
            var overlayDataPack = new byte[1 << 18];
            Format5.DecodeInto(format80Data, overlayDataPack, 80);

            foreach (var tile in IsoTileList)
            {
                if (tile == null) continue;
                int idx = tile.Rx + 512 * tile.Ry;
                byte overlay_id = overlayPack[idx];

                /*                if (overlay_id != 0xff)
                                {*/
                byte overlay_value = overlayDataPack[idx];
                var ovl = new Overlay(overlay_id, overlay_value);
                ovl.Tile = tile.Clone();
                OverlayList.Add(ovl);
                /*                }*/
            }

            return OverlayList;
        }

        public void SaveOverlay(string path)
        {

            var overlayPack = new byte[1 << 18];
            for (int i = 0; i < overlayPack.Length; i++)
            {
                overlayPack[i] = 0xff;
            }
            var overlayDataPack = new byte[1 << 18];
            foreach (var overlay in OverlayList)
            {
                int index = overlay.Tile.Rx + 512 * overlay.Tile.Ry;
                overlayPack[index] = overlay.OverlayID;
                overlayDataPack[index] = overlay.OverlayValue;

            }

            var compressedPack = Format5.Encode(overlayPack, 80);
            var compressedDataPack = Format5.Encode(overlayDataPack, 80);

            string compressedPack64 = Convert.ToBase64String(compressedPack);
            string compressedDataPack64 = Convert.ToBase64String(compressedDataPack);
            int j = 1;
            int idx = 0;

            int j2 = 1;
            int idx2 = 0;

            var saveFile = new IniFile(path);
            if (saveFile.SectionExists("OverlayPack"))
                saveFile.RemoveSection("OverlayPack");

            saveFile.AddSection("OverlayPack");
            if (saveFile.SectionExists("OverlayDataPack"))
                saveFile.RemoveSection("OverlayDataPack");

            saveFile.AddSection("OverlayDataPack");

            var OverlayPackSection = saveFile.GetSection("OverlayPack");
            var OverlayDataPackSection = saveFile.GetSection("OverlayDataPack");

            while (idx < compressedPack64.Length)
            {
                int adv = Math.Min(70, compressedPack64.Length - idx);//70 is the length of each line
                OverlayPackSection.SetStringValue(j.ToString(), compressedPack64.Substring(idx, adv));
                j++;
                idx += adv;//idx=adv+1
            }
            while (idx2 < compressedDataPack64.Length)
            {
                int adv = Math.Min(70, compressedDataPack64.Length - idx2);//70 is the length of each line
                OverlayDataPackSection.SetStringValue(j2.ToString(), compressedDataPack64.Substring(idx2, adv));
                j2++;
                idx2 += adv;//idx=adv+1
            }
            saveFile.WriteIniFile();
        }

        public void SaveWorkingOverlay(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var overlayPack = new IniFile(path);
            overlayPack.AddSection("overlayPack");
            var mapPackSection = overlayPack.GetSection("overlayPack");
            int mapPackIndex = 1;
            //mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");

            for (int i = 0; i < OverlayList.Count; i++)
            {
                var overlay = OverlayList[i];
                mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                        overlay.OverlayID.ToString() + "," +
                        overlay.OverlayValue.ToString());

            }
            overlayPack.WriteIniFile();
        }

        public void SaveWorkingOverlay2(string path, List<Overlay> OverlayList2)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            var overlayPack = new IniFile(path);
            overlayPack.AddSection("overlayPack");
            var mapPackSection = overlayPack.GetSection("overlayPack");
            int mapPackIndex = 1;
            //mapPackSection.SetStringValue("0", "Dx,Dy,Rx,Ry,Z,TileNum,SubTile");

            for (int i = 0; i < OverlayList2.Count; i++)
            {
                var overlay = OverlayList2[i];

                if (overlay.Tile.Used)
                {
                    mapPackSection.SetStringValue(mapPackIndex++.ToString(),
                        overlay.OverlayID.ToString() + "," +
                        overlay.OverlayValue.ToString() + ";" +
                        overlay.Tile.RelativeRx + "," +
                        overlay.Tile.RelativeRy);
                }


            }
            overlayPack.WriteIniFile();
        }

        public IsoTile GetIsoTileByXY(int x, int y)
        {
            if (x >= Width + Height || x < 0 || y >= Width + Height || y < 0)
            {
                return null;
            }
            else
            {
                foreach (var isoTile in IsoTileList)
                {
                    if (isoTile.Rx == x && isoTile.Ry == y)
                        return isoTile;
                }
                return null;
            }
        }

    }
}
