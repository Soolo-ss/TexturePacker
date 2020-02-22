using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TexturePacker
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i != 12; ++i)
            {
                string path = @"D:\mt_project\mt_project\UI_Image\18\00" + i;
                string target = Path.GetFileName(path);
                TexturePacker packer = new TexturePacker();
                packer.PackerTexturesInPath(path, 1024, 1024, "./" + target, target);
            }
        }
    }
}
