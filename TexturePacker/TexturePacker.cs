using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace TexturePacker
{
    public class TextureRect
    {
        public Texture texture;
        public Rect rect;
        public int packerIndex;

        public TextureRect(Texture texture, Rect rect, int packerIndex)
        {
            this.texture = texture;
            this.rect = rect;
            this.packerIndex = packerIndex;
        }
    }

    public class TextureBin
    {
        public int width;
        public int height;
        public List<TextureRect> textureRects = new List<TextureRect>();
    }

    public class TexturePacker
    {
        public static TexturePacker Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TexturePacker();

                return _instance;
            }
        }

        private static TexturePacker _instance;


        public TexturePacker()
        {
            m_RectPackers = new List<MaxRectsBinPacker>();
            m_TextureLoader = new TextureLoader();
            m_Textures = new List<List<TextureRect>>();
        }

        public bool PackerTexturesInPath(string path, int binWidth, int binHeight, string targetPath, string targetName, FreeRectChoiceHeuristic heuristic = FreeRectChoiceHeuristic.RectBottomLeftRule)
        {
            List<string> paths = Directory.GetFiles(path).ToList();

            paths.Sort();

            return PackerTextures(paths, binWidth, binHeight, targetPath, targetName, heuristic);
        }

        //需要保证files内部的顺序
        public bool PackerTextures(List<string> files, int binWidth, int binHeight, string targetPath, string targetName, FreeRectChoiceHeuristic heuristic = FreeRectChoiceHeuristic.RectBottomLeftRule)
        {
            try
            {
                Init(files, binWidth, binHeight, targetPath, targetName, heuristic);

                InsertTextures();

                PackTexturesToFiles();
            }
            catch(Exception e)
            {
                return false;
            }

            return true;
        }

        private void Init(List<string> files, int binWidth, int binHeight, string targetPath, string targetName, FreeRectChoiceHeuristic heuristic)
        {
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            m_TextureLoader.Init(files);

            MaxRectsBinPacker packer = new MaxRectsBinPacker(binWidth, binHeight);
            m_RectPackers.Add(packer);
            m_Textures.Add(new List<TextureRect>());

            this.m_TargetPath = targetPath;
            this.m_TargetFileName = targetName;
            this.m_BinWidth = binWidth;
            this.m_BinHeight = binHeight;
            this.m_HeuristicMethod = heuristic;
        }

        private void InsertTextures()
        {
            foreach(var texture in m_TextureLoader.m_Textures)
            {
                if (texture.width > m_BinWidth || texture.height > m_BinHeight)
                    throw new Exception("输入的图片超出图集最大值, 请选择适当的图集大小");

                Insert(texture);
            }
        }
        private void Insert(Texture texture)
        {
            Rect addRect = null;
            for(int i = 0; i != m_RectPackers.Count; ++i)
            {
                addRect = m_RectPackers[i].Insert(texture.width, texture.height, m_HeuristicMethod);
                if (Rect.IsEmpty(addRect))
                    continue;
                else
                {
                    m_Textures[i].Add(new TextureRect(texture, addRect, i));
                    return;
                }
            }

            MaxRectsBinPacker newPacker = new MaxRectsBinPacker(m_BinWidth, m_BinHeight);
            m_RectPackers.Add(newPacker);

            addRect = newPacker.Insert(texture.width, texture.height, m_HeuristicMethod);

            m_Textures.Add(new List<TextureRect>());
            m_Textures[m_Textures.Count - 1].Add(new TextureRect(texture, addRect, m_RectPackers.Count - 1));
        }

        private void PackTexturesToFiles()
        {
            for(int i = 0; i != m_RectPackers.Count; ++i)
            {
                List<TextureRect> textures = m_Textures[i];

                int maxHeight = textures.Max(x=>
                {
                    return x.rect.y + x.rect.height;
                });
                int maxWidth = textures.Max(x=>
                {
                    return x.rect.x + x.rect.width;
                });

                string targetFileName = m_TargetFileName + i.ToString();
                string targetDebugFileName = m_TargetFileName + i.ToString() + "debug";
                if (!targetFileName.Contains(".png"))
                {
                    targetFileName += ".png";
                    targetDebugFileName += ".png";
                }

                targetFileName = Path.Combine(m_TargetPath, targetFileName);
                targetDebugFileName = Path.Combine(m_TargetPath, targetDebugFileName);

                Bitmap binImage = new Bitmap(maxWidth, maxHeight, PixelFormat.Format32bppArgb);

                Bitmap freeImage = new Bitmap(maxWidth, maxHeight, PixelFormat.Format32bppArgb);

                using (Graphics gBinImage = Graphics.FromImage(binImage))
                {
                    gBinImage.Clear(Color.Black);

                    //gBinImage.Transform = new Matrix(new RectangleF(0, 0, 1024, 1024), new PointF[] { new PointF(0, 1024), new PointF(1024, 1024), new PointF(0, 0) });

                    foreach (var texture in textures)
                    {
                        gBinImage.DrawImage(texture.texture.bitmap, texture.rect.x, texture.rect.y);
                    }
                }

                using (Graphics gFreeImage = Graphics.FromImage(freeImage))
                {
                    gFreeImage.Clear(Color.Transparent);

                    /*
                    for (int j = 0; j != m_RectPackers[i].freeRects.Count; ++j)
                    {
                        var xbegin = m_RectPackers[i].freeRects[j].x;
                        var xend = xbegin + m_RectPackers[i].freeRects[j].width;
                        var ybegin = m_RectPackers[i].freeRects[j].y;
                        var yend = ybegin + m_RectPackers[i].freeRects[j].height;
                        SolidBrush brush = new SolidBrush(Color.Pink);
                        SolidBrush fontBrush = new SolidBrush(Color.Black);
                        Pen pen = new Pen(brush);
                        Font font = new Font(new FontFamily(System.Drawing.Text.GenericFontFamilies.Serif), 2.0f);

                        gFreeImage.DrawRectangle(pen, xend - xbegin, yend - ybegin, m_RectPackers[i].freeRects[j].width, m_RectPackers[i].freeRects[j].height);
                    }
                    */
                }

                binImage.Save(targetFileName);
                freeImage.Save(targetDebugFileName);
            }
        }

        private List<MaxRectsBinPacker> m_RectPackers;
        private FreeRectChoiceHeuristic m_HeuristicMethod;

        private TextureLoader m_TextureLoader;

        private List<List<TextureRect>> m_Textures;

        private string m_TargetPath;
        private string m_TargetFileName;
        private int m_BinWidth;
        private int m_BinHeight;
        
    }
}
