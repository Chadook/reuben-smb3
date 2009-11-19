﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daiz.NES.Reuben.ProjectManagement
{
    public class ROMManager
    {
        Dictionary<Guid, byte> levelIndexTable;

        private string Filename;
        public byte[] Rom;

        public ROMManager()
        {
            levelIndexTable = new Dictionary<Guid, byte>();
        }

        public bool CompileRom(string fileName)
        {
            if (!LoadRom(fileName)) return false;
            if(!IsPatchedRom()) return false;
            if (IsCleanRom())
            {
                SignRom(ProjectController.ProjectManager.CurrentProject.Guid);
            }

            if (!VerifyRomGuid(ProjectController.ProjectManager.CurrentProject.Guid)) return false;
            if (!CompileLevels()) return false;

            return true;
        }

        private bool CompileLevels()
        {
            int address = 0x44010;
            byte levelIndex = 0;
            foreach(LevelInfo li in ProjectController.LevelManager.Levels)
            {
                levelIndexTable.Add(li.LevelGuid, levelIndex++);
            }

            Level l = new Level();
            foreach (LevelInfo li in ProjectController.LevelManager.Levels)
            {
                l.Load(li);
                address = WriteLevel(l, address);
            }

            return true;
        }

        private bool CompileWorlds()
        {
            int address = 0x40010;

            World w = new World();
            foreach (WorldInfo wi in ProjectController.WorldManager.Worlds)
            {
                w.Load(wi);
                address = WriteWorld(w, address);
                if (address >= 0x4400F)
                    return false;
            }

            return true;
        }

        public bool LoadRom(string filename)
        {
            if(!File.Exists(filename)) return false;
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            Rom = new byte[fs.Length];
            fs.Read(Rom, 0, (int) fs.Length);
            fs.Close();
            Filename = filename;
            return true;
        }

        public bool IsCleanRom()
        {
            byte[] guid = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                guid[i] = Rom[0xFE000 + i];
            }

            return new Guid(guid) == Guid.Empty;
        }

        public bool IsPatchedRom()
        {
            return true;
        }

        public bool VerifyRomGuid(Guid projectGuid)
        {
            byte[] guid = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                guid[i] = Rom[0xFE000 + i];
            }

            Guid compareGuid = new Guid(guid);
            return compareGuid == projectGuid;
        }

        public void SignRom(Guid projectGuid)
        {
            byte[] guidArray = projectGuid.ToByteArray();
            for (int i = 0; i < 16; i++)
            {
                Rom[0xFE000 + i] = guidArray[i];
            };
        }

        public int WriteLevel(Level l, int levelAddress)
        {
            int yStart = 0;
            switch (l.LevelLayout)
            {
                case LevelLayout.Horizontal:
                    yStart = l.YStart - 1;
                    break;

                case LevelLayout.Vertical:
                    yStart = l.YStart - 1;
                    break;
            }

            Rom[levelAddress++] = (byte) l.ClearValue;
            Rom[levelAddress++] = (byte) l.GraphicsBank;
            Rom[levelAddress++] = (byte) l.Palette;
            Rom[levelAddress++] = (byte)((l.StartAction << 4) | l.Type);
            Rom[levelAddress++] = (byte)(((l.XStart & 0x0F) << 4) | ((l.XStart & 0xF0) >> 4));
            Rom[levelAddress++] = (byte)(((yStart & 0x0F) << 4) | ((yStart & 0xF0) >> 4));

            if (l.Music < 15)
            {
                Rom[levelAddress++] = (byte) l.Music;
            }
            else
            {
                Rom[levelAddress++] = (byte)((l.Music - 15) << 4);
            }

            Rom[levelAddress++] = (byte)(((l.Time / 100) << 4) | ((l.Time - ((l.Time / 100) * 100)) / 10));

            switch (l.LevelLayout)
            {
                case LevelLayout.Horizontal:
                    Rom[levelAddress++] = (byte)((l.ScrollType << 4) | (l.Length - 1));
                    break;

                case LevelLayout.Vertical:
                    Rom[levelAddress++] = (byte)((0x80) | (l.Length - 1));
                    break;
            }

            Rom[levelAddress++] = (byte)l.Unused1;
            Rom[levelAddress++] = (byte)l.Unused2;
            Rom[levelAddress++] = (byte)l.Unused3;

            foreach (var p in l.Pointers)
            {
                Rom[levelAddress++] = levelIndexTable[p.LevelGuid];
                Rom[levelAddress++] = (byte)p.XEnter;
                Rom[levelAddress++] = (byte)p.YEnter;
                Rom[levelAddress++] = (byte)p.XExit;
                Rom[levelAddress++] = (byte)p.YExit;
                Rom[levelAddress++] = (byte)p.ExitType;
            }

            Rom[levelAddress++] = (byte)0xFF;
            byte[] levelData = l.GetCompressedData();
            for (int i = 0; i < levelData.Length; i++)
            {
                Rom[levelAddress++] = levelData[i];
            }

            Rom[levelAddress] = (byte) 0xFF;

            switch (l.LevelLayout)
            {
                case LevelLayout.Horizontal:
                    foreach (var s in from sprites in l.SpriteData orderby sprites.X select sprites)
                    {
                        Rom[levelAddress++] = (byte)s.InGameID;
                        Rom[levelAddress++] = (byte)s.X;
                        Rom[levelAddress++] = (byte)s.Y;
                    }
                    break;

                case LevelLayout.Vertical:
                    foreach (var s in from sprites in l.SpriteData orderby sprites.Y select sprites)
                    {
                        Rom[levelAddress++] = (byte)s.InGameID;
                        Rom[levelAddress++] = (byte)s.X;
                        Rom[levelAddress++] = (byte)s.Y;
                    }
                    break;
            }
            Rom[levelAddress++] = 0xFF;
            return levelAddress;
        }


        public int WriteWorld(World w, int levelAddress)
        {
            Rom[levelAddress++] = (byte)w.GraphicsBank;
            Rom[levelAddress++] = (byte)w.Palette;
            Rom[levelAddress++] = (byte)((w.XStart << 4) | (w.YStart));

            if (w.Music < 15)
            {
                Rom[levelAddress++] = (byte)w.Music;
            }
            else
            {
                Rom[levelAddress++] = (byte)((w.Music - 15) << 4);
            }

            Rom[levelAddress++] = (byte)((w.XStart & 0xF0) >> 4);
            Rom[levelAddress++] = (byte)w.Unused1;

            foreach (var p in w.Pointers)
            {
                Rom[levelAddress++] = levelIndexTable[p.LevelGuid];
                Rom[levelAddress++] = (byte)p.X;
                Rom[levelAddress++] = (byte)p.Y;
            }

            Rom[levelAddress++] = (byte)0xFF;
            byte[] levelData = w.GetCompressedData();
            for (int i = 0; i < levelData.Length; i++)
            {
                Rom[levelAddress++] = levelData[i];
            }

            Rom[levelAddress] = (byte)0xFF;
            foreach (var s in from sprites in w.SpriteData orderby sprites.X select sprites)
            {
                Rom[levelAddress++] = (byte)s.InGameID;
                Rom[levelAddress++] = (byte)s.X;
                Rom[levelAddress++] = (byte)s.Y;
            }

            return levelAddress;
        }

        public bool WriteBlockDefinitions(List<BlockDefinition> definitions)
        {
            int defCount = 0;
            foreach (var d in definitions)
            {
                int address = 0x3E010 + defCount * 0x400;

                for (int i = 0; i < 256; i++)
                {
                    Rom[address] = d[i][0, 0];
                    Rom[address + 0x100] = d[i][0, 1];
                    Rom[address + 0x200] = d[i][1, 0];
                    Rom[address + 0x300] = d[i][1, 1];
                    address++;
                }

                defCount++;
            }
            return true;
        }

        public bool WritePalette(List<PaletteInfo> paletteInfo)
        {
            int address = 0x3C010;
            foreach (var p in paletteInfo)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Rom[address++] = (byte)p[i, j];
                    }
                }
            }

            return true;
        }

        public bool Save()
        {
            FileStream fs = new FileStream(Filename, FileMode.Open, FileAccess.Write);
            fs.Write(Rom, 0, Rom.Length);
            return true;
        }
    }
}
