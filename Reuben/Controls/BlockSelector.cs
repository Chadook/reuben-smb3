﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

using Daiz.Library;
using Daiz.NES.Reuben.ProjectManagement;

namespace Daiz.NES.Reuben
{
    public unsafe class BlockSelector : Control
    {
        public event EventHandler<TEventArgs<MouseButtons>> SelectionChanged;

        public BlockSelector()
        {
            BackBuffer = new Bitmap(256, 256);
            QuickColorLookup = new Color[4, 4];
            SpecialColors = new Color[8, 4];
            CurrentDefiniton = null;
            this.Width = this.Height = 256;
            this.MouseDown += new MouseEventHandler(PatternTableViewer_MouseDown);
            FullRender();
        }

        private PatternTable _SpecialTable;
        public PatternTable SpecialTable
        {
            get { return _SpecialTable; }
            set
            {
                _SpecialTable = value;
                FullRender();
            }
        }

        private BlockLayout _BlockLayout;
        public BlockLayout BlockLayout
        {
            get { return _BlockLayout; }
            set
            {
                _BlockLayout = value;
                FullRender();
            }
        }

        private bool _ShowSpecialBlocks;
        public bool ShowSpecialBlocks
        {
            get { return _ShowSpecialBlocks; }
            set
            {
                _ShowSpecialBlocks = value;
                FullRender();
            }
        }

        private BlockDefinition _SpecialDefinitions;
        public BlockDefinition SpecialDefnitions
        {
            private get { return _SpecialDefinitions; }
            set
            {
                _SpecialDefinitions = value;
                FullRender();
            }
        }

        private PatternTable _CurrentTable;
        public PatternTable CurrentTable
        {
            set
            {
                if (_CurrentTable != null)
                {
                    _CurrentTable.GraphicsChanged -= _CurrentTable_GraphicsChanged;
                }

                if (_CurrentTable != value)
                {
                    _CurrentTable = value;

                    if (_CurrentTable != null)
                    {
                        _CurrentTable.GraphicsChanged += new EventHandler<TEventArgs<int>>(_CurrentTable_GraphicsChanged);
                    }

                    FullRender();
                }
            }
        }

        void _CurrentTable_GraphicsChanged(object sender, TEventArgs<int> e)
        {
            FullRender();
        }

        private int DefinitionIndex;
        private BlockDefinition _CurrentDefiniton;
        public BlockDefinition CurrentDefiniton
        {
            get { return _CurrentDefiniton; }
            set
            {
                if (_CurrentDefiniton == value) return;
                _CurrentDefiniton = value;
                DefinitionIndex = ProjectController.BlockManager.AllDefinitions.IndexOf(value);
                FullRender();
            }
        }

        private PaletteInfo _CurrentPalette;
        public PaletteInfo CurrentPalette
        {
            set
            {
                _CurrentPalette = value;
                UpdateColors();
                FullRender();
            }
        }

        Bitmap BackBuffer;

        private Color[,] QuickColorLookup;
        private Color[,] SpecialColors;

        public PaletteInfo SpecialPalette
        {
            set
            {
                for (int j = 0; j < 8; j++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        SpecialColors[j, i] = ProjectController.ColorManager.Colors[value[j, i]];
                    }
                }
            }
        }

        private void UpdateColors()
        {
            if (_CurrentPalette != null)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        QuickColorLookup[j, i] = ProjectController.ColorManager.Colors[_CurrentPalette[j, i]];
                    }
                }
            }
        }

        public bool HaltRendering { get; set; }

        private void FullRender()
        {
            if (HaltRendering || _CurrentTable == null || _CurrentPalette == null || _CurrentDefiniton == null || BlockLayout == null)
            {
                Graphics.FromImage(BackBuffer).Clear(Color.Black);
                return;
            }

            BitmapData data = BackBuffer.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    int tileValue = _BlockLayout.Layout[i * 16 + j];
                    int PaletteIndex = tileValue / 0x40;
                    if (tileValue < 0)
                    {
                        RenderBlank(j * 16, i * 16, data);
                        RenderBlank(j * 16, i * 16 + 8, data);
                        RenderBlank(j * 16 + 8, i * 16, data);
                        RenderBlank(j * 16 + 8, i * 16 + 8, data);
                        continue;
                    }

                    Block b = CurrentDefiniton[tileValue];
                    RenderTile(_CurrentTable[b[0,0]], j * 16, i * 16, PaletteIndex, data);
                    RenderTile(_CurrentTable[b[0, 1]], j * 16, i * 16 + 8, PaletteIndex, data);
                    RenderTile(_CurrentTable[b[1, 0]], j * 16 + 8, i * 16, PaletteIndex, data);
                    RenderTile(_CurrentTable[b[1, 1]], j * 16 + 8, i * 16 + 8, PaletteIndex, data);

                    if (_ShowBlockProperties)
                    {
                        switch (ProjectController.SpecialManager.GetProperty(DefinitionIndex, tileValue))
                        {
                            case BlockProperty.Solid:
                                RenderSpecialTileAlpha(_SpecialTable[0xD0], j * 16, i * 16, 6, data, .50);
                                RenderSpecialTileAlpha(_SpecialTable[0xD0], j * 16, i * 16 + 8, 6, data, .50);
                                RenderSpecialTileAlpha(_SpecialTable[0xD0], j * 16 + 8, i * 16, 6, data, .50);
                                RenderSpecialTileAlpha(_SpecialTable[0xD0], j * 16 + 8, i * 16 + 8, 6, data, .50);
                                break;

                            case BlockProperty.TopSolid:
                                RenderSpecialTileAlpha(_SpecialTable[0xD0], j * 16, i * 16, 6, data, .50);
                                RenderSpecialTileAlpha(_SpecialTable[0xD0], j * 16 + 8, i * 16, 6, data, .50);
                                break;

                            case BlockProperty.Water:
                                RenderSpecialTileAlpha(_SpecialTable[0xD1], j * 16, i * 16, 6, data, .50);
                                RenderSpecialTileAlpha(_SpecialTable[0xD1], j * 16, i * 16 + 8, 6, data, .50);
                                RenderSpecialTileAlpha(_SpecialTable[0xD1], j * 16 + 8, i * 16, 6, data, .50);
                                RenderSpecialTileAlpha(_SpecialTable[0xD1], j * 16 + 8, i * 16 + 8, 6, data, .50);
                                break;
                        }
                    }

                    if (_ShowSpecialBlocks)
                    {
                        SpecialBlock sb = (SpecialBlock)SpecialDefnitions[tileValue];
                        if (sb == null) continue;
                        int SpecialPaletteIndex = sb.Palette;
                        RenderSpecialTileAlpha(_SpecialTable[sb[0, 0]], j * 16, i * 16, SpecialPaletteIndex, data, .75);
                        RenderSpecialTileAlpha(_SpecialTable[sb[0, 1]], j * 16, i * 16 + 8, SpecialPaletteIndex, data, .75);
                        RenderSpecialTileAlpha(_SpecialTable[sb[1, 0]], j * 16 + 8, i * 16, SpecialPaletteIndex, data, .75);
                        RenderSpecialTileAlpha(_SpecialTable[sb[1, 1]], j * 16 + 8, i * 16 + 8, SpecialPaletteIndex, data, .75);
                    }
                }
            }
            BackBuffer.UnlockBits(data);
            Invalidate();
        }

        private void RenderTile(Tile tile, int x, int y, int PaletteIndex, BitmapData data)
        {
            byte* dataPointer = (byte*)data.Scan0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    long offset = (data.Stride * (y + i)) + (x * 3);
                    long xOffset = (j * 3) + offset;
                    Color c = QuickColorLookup[PaletteIndex, tile[j, i]];
                    *(dataPointer + xOffset) = c.B;
                    *(dataPointer + xOffset + 1) = c.G;
                    *(dataPointer + xOffset + 2) = c.R;
                }
            }
        }

        private void RenderBlank(int x, int y, BitmapData data)
        {
            byte* dataPointer = (byte*)data.Scan0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    long offset = (data.Stride * (y + i)) + (x * 3);
                    long xOffset = (j * 3) + offset;
                    Color c;
                    if ((i + j) % 2 == 0)
                    {
                        c = Color.Black;
                    }
                    else
                    {
                        c = Color.Red;
                    }

                    *(dataPointer + xOffset) = c.B;
                    *(dataPointer + xOffset + 1) = c.G;
                    *(dataPointer + xOffset + 2) = c.R;
                }
            }
        }

        private void RenderSpecialTileAlpha(Tile tile, int x, int y, int PaletteIndex, BitmapData data, double alpha)
        {
            byte* dataPointer = (byte*)data.Scan0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    long offset = (data.Stride * (y + i)) + (x * 3);
                    long xOffset = (j * 3) + offset;
                    Color c = SpecialColors[PaletteIndex, tile[j, i]];
                    if (c == Color.Empty) continue;

                    *(dataPointer + xOffset) = (byte)((1 - alpha) * (*(dataPointer + xOffset)) + (alpha * c.B));
                    *(dataPointer + xOffset + 1) = (byte)((1 - alpha) * (*(dataPointer + xOffset + 1)) + (alpha * c.G));
                    *(dataPointer + xOffset + 2) = (byte)((1 - alpha) * (*(dataPointer + xOffset + 2)) + (alpha * c.R));
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(BackBuffer, 0, 0);
            Rectangle rect =  new Rectangle((SelectedIndex % 16) * 16, (SelectedIndex / 16) * 16, 15, 15);
            e.Graphics.DrawRectangle(Pens.White, rect);
            rect.X += 1;
            rect.Y += 1;
            rect.Width -= 2;
            rect.Height -= 2;
            e.Graphics.DrawRectangle(Pens.Red, rect);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {

        }

        public void UpdateSelection()
        {
            Rectangle rect = new Rectangle((SelectedIndex % 16) * 16, (SelectedIndex / 16) * 16, 16, 16);
            BitmapData data = BackBuffer.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int tileValue = BlockLayout.Layout[SelectedIndex];
            if (tileValue >= 0)
            {
                int PaletteIndex = tileValue / 0x40;
                Block b = CurrentDefiniton[tileValue];
                RenderTile(_CurrentTable[b[0, 0]], 0, 0, PaletteIndex, data);
                RenderTile(_CurrentTable[b[0, 1]], 0, 8, PaletteIndex, data);
                RenderTile(_CurrentTable[b[1, 0]], 8, 0, PaletteIndex, data);
                RenderTile(_CurrentTable[b[1, 1]], 8, 8, PaletteIndex, data);

                if (_ShowBlockProperties)
                {
                    switch (ProjectController.SpecialManager.GetProperty(DefinitionIndex, tileValue))
                    {
                        case BlockProperty.Solid:
                            RenderSpecialTileAlpha(_SpecialTable[0xD0], 0, 0, 6, data, .50);
                            RenderSpecialTileAlpha(_SpecialTable[0xD0], 0, 8, 6, data, .50);
                            RenderSpecialTileAlpha(_SpecialTable[0xD0], 8, 0, 6, data, .50);
                            RenderSpecialTileAlpha(_SpecialTable[0xD0], 8, 8, 6, data, .50);
                            break;

                        case BlockProperty.TopSolid:
                            RenderSpecialTileAlpha(_SpecialTable[0xD0], 0, 0, 6, data, .50);
                            RenderSpecialTileAlpha(_SpecialTable[0xD0], 8, 0, 6, data, .50);
                            break;

                        case BlockProperty.Water:
                            RenderSpecialTileAlpha(_SpecialTable[0xD1], 0, 0, 6, data, .50);
                            RenderSpecialTileAlpha(_SpecialTable[0xD1], 0, 8, 6, data, .50);
                            RenderSpecialTileAlpha(_SpecialTable[0xD1], 8, 0, 6, data, .50);
                            RenderSpecialTileAlpha(_SpecialTable[0xD1], 8, 8, 6, data, .50);
                            break;
                    }
                }

                if (_ShowSpecialBlocks)
                {
                    SpecialBlock sb = (SpecialBlock)SpecialDefnitions[tileValue];
                    if (sb != null)
                    {
                        int SpecialPaletteIndex = sb.Palette;
                        RenderSpecialTileAlpha(_SpecialTable[sb[0, 0]], 0, 0, SpecialPaletteIndex, data, .75);
                        RenderSpecialTileAlpha(_SpecialTable[sb[0, 1]], 0, 8, SpecialPaletteIndex, data, .75);
                        RenderSpecialTileAlpha(_SpecialTable[sb[1, 0]], 8, 0, SpecialPaletteIndex, data, .75);
                        RenderSpecialTileAlpha(_SpecialTable[sb[1, 1]], 8, 8, SpecialPaletteIndex, data, .75);
                    }
                }
            }
            else
            {
                RenderBlank(0, 0, data);
                RenderBlank(0, 8, data);
                RenderBlank(8, 0, data);
                RenderBlank(8, 8, data);
            }
            BackBuffer.UnlockBits(data);
            Invalidate(rect);
        }

        public int SelectedTileIndex
        {
            get
            {
                if (BlockLayout == null || BlockLayout.Layout == null) return 0;
                if (BlockLayout.Layout[SelectedIndex] < 0) return 0;
                return BlockLayout.Layout[SelectedIndex];
            }
            set
            {
                if (BlockLayout == null || BlockLayout.Layout == null) return;
                bool found = false;
                for (int i = 0; i < 256; i++)
                {
                    if (BlockLayout.Layout[i] == value)
                    {
                        SelectedIndex = i;
                        found = true;
                        break;
                    }
                }

                if(!found)
                {
                    BlockLayout = ProjectController.LayoutManager.BlockLayouts[0];
                    SelectedIndex = value;
                }

                SelectedBlock = _CurrentDefiniton[BlockLayout.Layout[SelectedIndex]];

                if (SelectionChanged != null)
                {
                    SelectionChanged(this, null);
                }

                Invalidate();
            }
        }

        public int SelectedIndex { get; set; }
        public Block SelectedBlock { get; private set; }

        private void PatternTableViewer_MouseDown(object sender, MouseEventArgs e)
        {
            if (BlockLayout == null)
            {
                MessageBox.Show("There is no selected layout to edit. Click add to add a new layout before editing");
                return;
            }

            if (SelectedBlock != null)
            {
                SelectedBlock.DefinitionChanged -= SelectedBlock_DefinitionChanged;
            }

            SelectedIndex = e.X / 16 + ((e.Y / 16) * 16);
            if (BlockLayout.Layout[SelectedIndex] == -1)
            {
                Invalidate();
                return;
            }

            SelectedBlock = CurrentDefiniton[BlockLayout.Layout[SelectedIndex]];
            if (SelectionChanged != null)
            {
                SelectionChanged(this, new TEventArgs<MouseButtons>(e.Button));
            }

            SelectedBlock.DefinitionChanged += new EventHandler(SelectedBlock_DefinitionChanged);
            Invalidate();
        }

        void SelectedBlock_DefinitionChanged(object sender, EventArgs e)
        {
            UpdateSelection();
        }

        public void Redraw()
        {
            UpdateColors();
            FullRender();
            Invalidate();
        }

        private bool _ShowBlockProperties;
        public bool ShowBlockProperties
        {
            get { return _ShowBlockProperties; }
            set
            {
                _ShowBlockProperties = value;
                FullRender();
                Invalidate();
            }
        }
    }
}