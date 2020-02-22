using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace TexturePacker
{
    public class Boader
    {
        public int left;
        public int right;
        public int top;
        public int bottom;
    }

    public class Texture
    {
        public string fileName;
        public Bitmap bitmap;
        public int width;
        public int height;
        public string filePath;
        public Boader boader = new Boader();

        public Texture(string filepath)
        {
            this.filePath = filepath;
            bitmap = new Bitmap(filepath);
            fileName = Path.GetFileNameWithoutExtension(filepath);
            width = bitmap.Width;
            height = bitmap.Height;
        }

        public void SetBoader(Boader boader)
        {
            this.boader = boader;
        }
    }


    public class TextureLoader
    {

        public TextureLoader()
        {
            m_Textures = new List<Texture>();
            m_FileNameToImage = new Dictionary<string, Texture>();
        }

        public void Reset()
        {
            m_Textures.Clear();
            m_FileNameToImage.Clear();
        }

        public void Init(List<string> paths)
        {
            m_Textures.Clear();

            foreach(var path in paths)
            {
                Texture texture = new Texture(path);

                m_Textures.Add(texture);
                m_FileNameToImage.Add(texture.fileName, texture);
            }
        }

        public List<Texture> m_Textures;
        public Dictionary<string, Texture> m_FileNameToImage;
    }
}
