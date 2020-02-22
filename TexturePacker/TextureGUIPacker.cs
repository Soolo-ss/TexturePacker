using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Xml;
using System.Drawing.Drawing2D;

namespace TexturePacker
{
    public enum ErrorCode
    {
        ERROR_TARGET_NAME,
        ERROR_TARGET_PATH,

        ERROR_NO
    }


    public class TextureGUIPacker
    {
        public static TextureGUIPacker Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TextureGUIPacker();

                return _instance;
            }
        }

        public static TextureGUIPacker _instance;

        public List<string> AllFiles { get { return m_FilePaths; } }

        //可能是改变设置了 重新生成一份
        public void Refresh()
        {
            //copy one
            List<string> paths = new List<string>();
            foreach(var filePath in m_FilePaths)
            {
                paths.Add(filePath);
            }

            Init();

            InsertTexture(paths.ToArray());

            ReShow();
        }

        public void ReShow()
        {
            if (m_ShowImageAction != null)
            {
                m_ShowImageAction.Invoke(m_ShowBitmaps);
            }
        }

        public void SetForm(Form form)
        {
            m_Form = form;
        }

        public void SetShowAction(Action<List<Bitmap>> action)
        {
            m_ShowImageAction = action;
        }

        public void SetShowSelectedAction(Action<TextureRect> action)
        {
            m_ShowSelectedImageAction = action;
        }

        public void SetMaxBinSize(int size)
        {
            m_BinHeight = size;
            m_BinWidth = size;

            Refresh();
        }

        public void SetHeuristic(FreeRectChoiceHeuristic heuristic)
        {
            m_Heuristic = heuristic;

            Refresh();
        }

        public void SetTargetPath(string path)
        {
            m_TargetPath = path;
        }

        public void InsertTexture(string[] filepaths)
        {
            foreach(var path in filepaths)
            {
                if (m_FilePaths.Contains( path ))
                {
                    continue;
                }

                Texture texture = new Texture(path);

                TextureRect addedOne = InsertTextureInternal(texture);

                m_FilePathToTextureRect[path] = addedOne;
                m_IndexToTextureRect[m_FilePathToTextureRect.Count() - 1] = addedOne;
            }

            foreach (var item in filepaths){
                if (!m_FilePaths.Contains( item )){
                    m_FilePaths.Add( item );
                }
            }

            m_ShowBitmaps = PackTexturesToImage();
            
            ReShow();
        }

        public ErrorCode SaveToFiles()
        {
            if (string.IsNullOrEmpty(m_TargetName))
                return ErrorCode.ERROR_TARGET_NAME;

            if (string.IsNullOrEmpty(m_TargetPath))
                return ErrorCode.ERROR_TARGET_PATH;

            if (!Directory.Exists(m_TargetPath))
            {
                try
                {
                    Directory.CreateDirectory(m_TargetPath);
                }
                catch (Exception ee)
                {
                    return ErrorCode.ERROR_TARGET_PATH;
                }
            }

            List<string> tpsheets = PackTexturesToTpSheet();

            for(int i = 0; i != m_ShowBitmaps.Count; ++i)
            {
                string targetName = m_TargetName + (i + 1).ToString() + ".png";
                string targetDataName = m_TargetName + (i + 1).ToString() + ".tpsheet";
                string targetPath = Path.Combine(m_TargetPath, targetName);
                string targetDataPath = Path.Combine(m_TargetPath, targetDataName);

                m_ShowBitmaps[i].Save(targetPath);

                if (i >= tpsheets.Count)
                    continue;

                using (FileStream fs = new FileStream(targetDataPath, FileMode.Create))
                {
                    var bytes = Encoding.UTF8.GetBytes(tpsheets[i]);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }

            return ErrorCode.ERROR_NO;
        }

        public void Reset()
        {
            m_TextureLoader.Reset();
            m_RectPackers.Clear();
            m_Textures.Clear();
            m_FilePaths.Clear();
            m_FilePathToTextureRect.Clear();
            m_ShowBitmaps.Clear();
            m_IndexToTextureRect.Clear();
        }

        public void ShowSelectedTextureRect(int selectedIndex)
        {
            if (!m_IndexToTextureRect.ContainsKey(selectedIndex))
                return;

            TextureRect texture = m_IndexToTextureRect[selectedIndex];

            if (m_ShowSelectedImageAction != null)
                m_ShowSelectedImageAction(texture);
        }

        public void SetTextureRectBoader(Boader boader, int selectedIndex)
        {
            if (!m_IndexToTextureRect.ContainsKey(selectedIndex))
                return;

            TextureRect texture = m_IndexToTextureRect[selectedIndex];

            texture.texture.boader = boader;

            if (m_ShowSelectedImageAction != null)
                m_ShowSelectedImageAction(texture);
        }

        public void SetTextureRectSingleBoader(int type, int size, int selectedIndex)
        {
            if (!m_IndexToTextureRect.ContainsKey(selectedIndex))
                return;

            TextureRect texutre = m_IndexToTextureRect[selectedIndex];

            if (type == 1)
                texutre.texture.boader.left = size;
            else if (type == 2)
                texutre.texture.boader.right = size;
            else if (type == 3)
                texutre.texture.boader.top = size;
            else if (type == 4)
                texutre.texture.boader.bottom = size;

            if (m_ShowSelectedImageAction != null)
                m_ShowSelectedImageAction(texutre);
        }

        public TextureRect GetTextureRectSelected(int selectedIndex)
        {
            if (!m_IndexToTextureRect.ContainsKey(selectedIndex))
                return null;

            return m_IndexToTextureRect[selectedIndex];
        }


        public void PackTexturesToGUIAtlas(string[] packerProjectsPath, string savePath)
        {
            XmlDocument xmlDocument = new XmlDocument();

            var rootNode = xmlDocument.CreateElement("root");
            xmlDocument.AppendChild(rootNode);

            for(int i = 0; i != packerProjectsPath.Length; ++i)
            {
                if (!packerProjectsPath[i].Contains(".solo"))
                    throw new Exception("请选择项目文件 ->" + packerProjectsPath[i]);

                using (FileStream fs = new FileStream(packerProjectsPath[i], FileMode.Open))
                {
                    XmlDocument projectDocument = new XmlDocument();
                    projectDocument.Load(fs);

                    var proRootNode = projectDocument.SelectSingleNode("PackTextures");

                    foreach(XmlNode texturesNode in proRootNode.ChildNodes)
                    {
                        var contentNode = xmlDocument.CreateElement("content");
                        rootNode.AppendChild(contentNode);

                        var key = texturesNode.Attributes["Key"].Value;
                        string firstKey = string.Empty;
                        string secondKey = string.Empty;
                        if (key.Length >= 4)
                        {
                            firstKey = key.Substring(0, 2);
                            secondKey = key.Substring(2, 2);
                        }
                        else
                        {
                            firstKey = key;
                            secondKey = key;
                        }

                        contentNode.SetAttribute("H", texturesNode.Attributes["Height"].Value);
                        contentNode.SetAttribute("W", texturesNode.Attributes["Width"].Value);
                        contentNode.SetAttribute("AssetPath", "UIAtlas/" + firstKey + "/" + secondKey + "/" + key + ".png");
                        contentNode.SetAttribute("TypeName", "UnityEngine.Texture");
                        contentNode.SetAttribute("Type", "Picture");
                        contentNode.SetAttribute("Version", key);
                        contentNode.SetAttribute("Extension", ".png");
                        contentNode.SetAttribute("Key", key);

                        foreach(XmlNode textureNode in texturesNode.ChildNodes)
                        {
                            var spriteNode = xmlDocument.CreateElement("Sprites");
                            contentNode.AppendChild(spriteNode);

                            string x = textureNode.Attributes["X"].Value;
                            string y = textureNode.Attributes["Y"].Value;
                            string width = textureNode.Attributes["Width"].Value;
                            string height = textureNode.Attributes["Height"].Value;
                            string spriteKey = textureNode.LocalName.Split('_')[1];
                            spriteNode.SetAttribute("rect", string.Format("({0}, {1}, {2}, {3})", x, y, width, height));
                            spriteNode.SetAttribute("pivot", "(0.5, 0.5)");
                            spriteNode.SetAttribute("name", spriteKey);
                            spriteNode.SetAttribute( "border" , "(0.0, 0.0, 0.0, 0.0)");
                            spriteNode.SetAttribute("alignment", "0");
                            spriteNode.SetAttribute( "atlasKey" , key);
                        }
                    }
                }
            }

            xmlDocument.Save(Path.Combine(savePath, "GUIAtlas.xml"));
        }

        public void PackTexturesToGUIAtlasEx( string packerProjectPath , string savePath )
        {
            XmlDocument xmlDocument = new XmlDocument();

            var rootNode = xmlDocument.CreateElement( "root" );
            xmlDocument.AppendChild( rootNode );

            if (!packerProjectPath.Contains( ".solo" ))
                throw new Exception( "请选择项目文件 ->" + packerProjectPath );

            using (FileStream fs = new FileStream( packerProjectPath , FileMode.Open ))
            {
                XmlDocument projectDocument = new XmlDocument();
                projectDocument.Load( fs );

                var proRootNode = projectDocument.SelectSingleNode( "PackTextures" );

                foreach (XmlNode texturesNode in proRootNode.ChildNodes)
                {
                    var contentNode = xmlDocument.CreateElement( "content" );
                    rootNode.AppendChild( contentNode );

                    var key = texturesNode.Attributes["Key"].Value;
                    string firstKey = string.Empty;
                    string secondKey = string.Empty;
                    if (key.Length >= 4)
                    {
                        firstKey = key.Substring( 0 , 2 );
                        secondKey = key.Substring( 2 , 2 );
                    }
                    else
                    {
                        firstKey = key;
                        secondKey = key;
                    }

                    contentNode.SetAttribute( "H" , texturesNode.Attributes["Height"].Value );
                    contentNode.SetAttribute( "W" , texturesNode.Attributes["Width"].Value );
                    contentNode.SetAttribute( "AssetPath" , "UIAtlas/" + firstKey + "/" + secondKey + "/" + key + ".png" );
                    contentNode.SetAttribute( "TypeName" , "UnityEngine.Texture" );
                    contentNode.SetAttribute( "Type" , "Picture" );
                    contentNode.SetAttribute( "Version" , key );
                    contentNode.SetAttribute( "Extension" , ".png" );
                    contentNode.SetAttribute( "Key" , key );

                    foreach (XmlNode textureNode in texturesNode.ChildNodes)
                    {
                        var spriteNode = xmlDocument.CreateElement( "Sprites" );
                        contentNode.AppendChild( spriteNode );

                        string x = textureNode.Attributes["X"].Value;
                        string y = textureNode.Attributes["Y"].Value;
                        string width = textureNode.Attributes["Width"].Value;
                        string height = textureNode.Attributes["Height"].Value;
                        string spriteKey = textureNode.LocalName.Split( '_' )[1];
                        spriteNode.SetAttribute( "rect" , string.Format( "({0}, {1}, {2}, {3})" , x , y , width , height ) );
                        spriteNode.SetAttribute( "pivot" , "(0.5, 0.5)" );
                        spriteNode.SetAttribute( "name" , spriteKey );
                        spriteNode.SetAttribute( "border" , "(0.0, 0.0, 0.0, 0.0)" );
                        spriteNode.SetAttribute( "alignment" , "0" );
                        spriteNode.SetAttribute( "atlasKey" , key );
                    }
                }
            }

            xmlDocument.Save( Path.Combine( savePath , Path.GetFileNameWithoutExtension( packerProjectPath ) + "0.png" ) );
        }


        public void CreateTextureProject(string targetfile)
        {
            Reset();
            ReShow();

            m_ProjectPath = targetfile;

            using (FileStream fs = new FileStream(m_ProjectPath, FileMode.Create))
            {
                XmlDocument xmlDocument = new XmlDocument();

                var PackTextures = xmlDocument.CreateElement("PackTextures");
                xmlDocument.AppendChild(PackTextures);

                xmlDocument.Save(fs);
            }
        }

        public void OpenOldTextureProject(string targetfile)
        {
            Reset();
            ReShow();

            m_ProjectPath = targetfile;

            if (!File.Exists(m_ProjectPath))
                throw new Exception("打开的项目不存在了");


            List<string> filePaths = new List<string>();

            XmlDocument xmlDocument = new XmlDocument();

            try
            {
                xmlDocument.Load(m_ProjectPath);
            }
            catch (Exception ee)
            {
                throw ee;
            }


            XmlNode structNode = xmlDocument.SelectSingleNode("data/struct");
            XmlNodeList mapNodes = structNode.SelectNodes("map");
            string filepath = string.Empty;
            string projectPath = System.IO.Path.GetDirectoryName(targetfile);
            string nowpath = string.Empty;

            XmlNodeList gKeyNodes = structNode.SelectNodes("key");

            if (projectPath == string.Empty)
            {
                throw new Exception("打开项目文件失败");
            }

            foreach(XmlNode mapNode in mapNodes)
            {
                foreach(XmlAttribute attribute in mapNode.Attributes)
                {
                    if (attribute.Value == "IndividualSpriteSettingsMap")
                    {
                        XmlNodeList keyNodes = mapNode.SelectNodes("key");

                        foreach(XmlNode keyNode in keyNodes)
                        {
                            if (projectPath == string.Empty)
                                throw new Exception("读取项目文件错误");

                            string singleFilePath = keyNode.InnerText;

                            string singleAbsFilePath = System.IO.Path.Combine(projectPath, singleFilePath);

                            filePaths.Add(singleAbsFilePath);
                        }
                    }
                }
            }

            InsertTexture(filePaths.ToArray());
        }

        public void OpenTextureProject(string targetfile)
        {
            Reset();
            ReShow();

            m_ProjectPath = targetfile;

            if (!File.Exists(m_ProjectPath))
                throw new Exception("打开的项目文件不存在");

            XmlDocument xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.Load(m_ProjectPath);
            }
            catch (Exception ee)
            {
                throw ee;
            }

            XmlNode packTexturesNode = xmlDocument.SelectSingleNode("PackTextures");

            List<string> paths = new List<string>();
            List<Boader> boaders = new List<Boader>();
            foreach(XmlNode texturesNodes in packTexturesNode.ChildNodes)
            {
                foreach (XmlNode textureNode in texturesNodes)
                {
                    paths.Add(textureNode.Attributes["FilePath"].Value);

                    Boader boader = new Boader();

                    boader.left = Convert.ToInt32(textureNode.Attributes["Left"].Value);
                    boader.right = Convert.ToInt32(textureNode.Attributes["Right"].Value);
                    boader.top = Convert.ToInt32(textureNode.Attributes["Top"].Value);
                    boader.bottom = Convert.ToInt32(textureNode.Attributes["Bottom"].Value);

                    boaders.Add(boader);
                }
            }

            InsertTexture(paths.ToArray());

            for(int i = 0; i != m_IndexToTextureRect.Count; ++i)
            {
                var texture = m_IndexToTextureRect[i];
                texture.texture.boader = boaders[i];
            }
        }

        public void SaveTextureProject()
        {
            if (string.IsNullOrEmpty(m_ProjectPath))
                throw new Exception("项目位置未知");

            List<Bitmap> bitmaps = PackTexturesToImage();

            using (FileStream fs = new FileStream(m_ProjectPath, FileMode.Create))
            {
                XmlDocument xmlDocument = new XmlDocument();

                var PackTextures  = xmlDocument.CreateElement("PackTextures");
                xmlDocument.AppendChild(PackTextures);

                for(int i = 0; i != m_Textures.Count; ++i)
                {
                    int height = m_ShowBitmaps[i].Height;
                    int width = m_ShowBitmaps[i].Width;
                    string key = m_TargetName + (i + 1).ToString();

                    var packNode = xmlDocument.CreateElement("PackTexture" + (i + 1).ToString());
                    PackTextures.AppendChild(packNode);

                    packNode.SetAttribute("Height", height.ToString());
                    packNode.SetAttribute("Width", width.ToString());
                    packNode.SetAttribute("Key", key);

                    for(int j = 0; j != m_Textures[i].Count; ++j)
                    {
                        var filename = Path.GetFileName(m_Textures[i][j].texture.filePath);
                        var filenameNoExt = Path.GetFileNameWithoutExtension(m_Textures[i][j].texture.filePath);
                        int x = m_Textures[i][j].rect.x;
                        int y = m_Textures[i][j].rect.y;
                        int rwidth = m_Textures[i][j].rect.width;
                        int rheight = m_Textures[i][j].rect.height;

                        var textureNode = xmlDocument.CreateElement("Index_" + filenameNoExt);
                        packNode.AppendChild(textureNode);
                        textureNode.SetAttribute("FileName", filename);
                        textureNode.SetAttribute("FilePath", m_Textures[i][j].texture.filePath);

                        textureNode.SetAttribute("X", x.ToString());
                        textureNode.SetAttribute("Y", y.ToString());
                        textureNode.SetAttribute("Width", rwidth.ToString());
                        textureNode.SetAttribute("Height", rheight.ToString());

                        textureNode.SetAttribute("Left", m_Textures[i][j].texture.boader.left.ToString());
                        textureNode.SetAttribute("Right", m_Textures[i][j].texture.boader.right.ToString());
                        textureNode.SetAttribute("Top", m_Textures[i][j].texture.boader.top.ToString());
                        textureNode.SetAttribute("Bottom", m_Textures[i][j].texture.boader.bottom.ToString());
                    }
                }

                xmlDocument.Save(fs);
            }
        }

        private List<string> PackTexturesToTpSheet()
        {
            List<string> tpsheets = new List<string>();

            for (int i = 0; i != m_RectPackers.Count; ++i)
            {
                List<TextureRect> textures = m_Textures[i];

                if (textures.Count <= 0)
                    continue;

                int maxHeight = textures.Max(x=>
                {
                    return x.rect.y + x.rect.height;
                });
                int maxWidth = textures.Max(x=>
                {
                    return x.rect.x + x.rect.width;
                });

                int suitableWidth = GetSuitableSize(maxWidth);
                int suitableHeight = GetSuitableSize(maxHeight);

                Bitmap binImage = new Bitmap(suitableWidth, suitableHeight, PixelFormat.Format32bppArgb);

                var tpsheet = string.Empty;

                tpsheet += ":format=40300\r\n";

                var targetName = m_TargetName + (i + 1).ToString() + ".png";

                tpsheet += string.Format(":texture={0}\r\n", targetName);

                tpsheet += string.Format(":size={0}x{1}\r\n", suitableWidth, suitableHeight);

                tpsheet += ":pivotpoints=enabled\r\n";

                tpsheet += string.Format(":borders=disabled\r\n");

                tpsheet += "\r\n";

                foreach(var texture in textures)
                {
                    var textureInfo = string.Empty;
                    textureInfo += texture.texture.fileName;
                    textureInfo += ";";

                    //x, y
                    textureInfo += texture.rect.x.ToString();
                    textureInfo += ";";
                    textureInfo += texture.rect.y.ToString();
                    textureInfo += ";";

                    //width, height
                    textureInfo += texture.rect.width.ToString();
                    textureInfo += ";";
                    textureInfo += texture.rect.height.ToString();
                    textureInfo += ";";
                    textureInfo += " ";

                    //pivot
                    textureInfo += "0.5;";
                    textureInfo += "0.5;";
                    textureInfo += " ";

                    //boader
                    textureInfo += (texture.texture.boader.left.ToString());
                    textureInfo += ";";
                    textureInfo += (texture.texture.boader.right.ToString());
                    textureInfo += ";";
                    textureInfo += (texture.texture.boader.top.ToString());
                    textureInfo += ";";
                    textureInfo += (texture.texture.boader.bottom.ToString());

                    textureInfo += "\r\n";

                    tpsheet += textureInfo;
                }

                tpsheets.Add(tpsheet);
            }

            return tpsheets;
        }

        private List<Bitmap> PackTexturesToImage()
        {
            List<Bitmap> bitmaps = new List<Bitmap>();

            for(int i = 0; i != m_RectPackers.Count; ++i)
            {
                List<TextureRect> textures = m_Textures[i];

                if (textures.Count <= 0)
                    continue;

                int maxHeight = textures.Max(x=>
                {
                    return x.rect.y + x.rect.height;
                });
                int maxWidth = textures.Max(x=>
                {
                    return x.rect.x + x.rect.width;
                });

                int suitableWidth = GetSuitableSize(maxWidth);
                int suitableHeight = GetSuitableSize(maxHeight);

                Bitmap binImage = new Bitmap(suitableWidth, suitableHeight, PixelFormat.Format32bppArgb);
                //Bitmap testImage = new Bitmap(suitableWidth, suitableHeight, PixelFormat.Format32bppArgb);

                //int borderSize = 5;

                using (Graphics gBinImage = Graphics.FromImage(binImage))
                {
                    gBinImage.Clear(Color.Empty);

                    foreach (var texture in textures)
                    {
                        int posx = texture.rect.x;
                        int posy = suitableHeight - texture.rect.y - texture.rect.height;
                        int width = texture.rect.width;
                        int height = texture.rect.height;

                        //gBinImage.DrawImage(texture.texture.bitmap, texture.rect.x + borderSize, (suitableHeight - texture.rect.y - texture.rect.height + borderSize), texture.rect.width - borderSize, texture.rect.height - borderSize);
                        gBinImage.DrawImage(texture.texture.bitmap, texture.rect.x, (suitableHeight - texture.rect.y - texture.rect.height), texture.rect.width, texture.rect.height);
                    }
                }

                /*
                using (Graphics gTestImage = Graphics.FromImage(testImage))
                {
                    gTestImage.Clear(Color.Empty);

                    SolidBrush solidBrush = new SolidBrush(Color.Black);
                    Pen pen = new Pen(new SolidBrush(Color.Black));
                    Font font = new Font(new FontFamily(System.Drawing.Text.GenericFontFamilies.Serif), 9);

                    for(int j = 0; j != textures.Count; ++j)
                    {
                        int orix = textures[j].rect.x;
                        int oriy = (suitableHeight - textures[j].rect.y - textures[j].rect.height);
                        gTestImage.DrawRectangle(pen, orix, oriy, textures[j].rect.width, textures[j].rect.height);
                        gTestImage.DrawString((j + 1).ToString(), font, solidBrush, new PointF(orix + textures[j].rect.width / 2, oriy + textures[j].rect.height / 2));
                    }
                }
                */

                bitmaps.Add(binImage);
                //bitmaps.Add(testImage);
            }

            return bitmaps;
        }

        private TextureRect InsertTextureInternal(Texture texture)
        {
            Rect addRect = null;
            TextureRect addedOne = null;
            int packerIndex = 0;

            for(int i = 0; i != m_RectPackers.Count; ++i)
            {
                addRect = m_RectPackers[i].Insert(texture.width, texture.height, m_Heuristic);
                if (Rect.IsEmpty(addRect))
                    continue;
                else
                {
                    packerIndex = i;
                    addedOne = new TextureRect(texture, addRect, packerIndex);
                    m_Textures[i].Add(addedOne);
                    return addedOne;
                }
            }

            MaxRectsBinPacker newPacker = new MaxRectsBinPacker(m_BinWidth, m_BinHeight);
            m_RectPackers.Add(newPacker);

            addRect = newPacker.Insert(texture.width, texture.height, m_Heuristic);

            packerIndex += 1;
            m_Textures.Add(new List<TextureRect>());
            addedOne = new TextureRect(texture, addRect, packerIndex);
            m_Textures[m_Textures.Count - 1].Add(addedOne);

            return addedOne;
        }

        public void Init()
        {
            Reset();

            MaxRectsBinPacker packer = new MaxRectsBinPacker(m_BinWidth, m_BinHeight);
            m_RectPackers.Add(packer);
            m_Textures.Add(new List<TextureRect>());

            ReShow();
        }

        private int GetSuitableSize(int width)
        {
            int size = 1;

            int basei = 2;

            while(size < width)
            {
                size *= basei;
            }

            return size;
        }

        public Form m_Form;
        public Bitmap m_ResultImage;

        private int m_BinWidth = 1024;
        private int m_BinHeight = 1024;

        private string m_TargetPath;
        private string m_TargetName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(m_ProjectPath);
            }
        }

        public string m_ProjectPath;

        private FreeRectChoiceHeuristic m_Heuristic = FreeRectChoiceHeuristic.RectBestAreaFit;

        private TextureLoader m_TextureLoader = new TextureLoader();
        private List<MaxRectsBinPacker> m_RectPackers = new List<MaxRectsBinPacker>();

        private List<List<TextureRect>> m_Textures = new List<List<TextureRect>>();

        private Dictionary<string, TextureRect> m_FilePathToTextureRect = new Dictionary<string, TextureRect>();

        private Dictionary<int, TextureRect> m_IndexToTextureRect = new Dictionary<int, TextureRect>();

        public List<string> m_FilePaths = new List<string>();

        private Action<List<Bitmap>> m_ShowImageAction;
        private Action<TextureRect> m_ShowSelectedImageAction;

        private List<Bitmap> m_ShowBitmaps = new List<Bitmap>();
    }
}
