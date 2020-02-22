using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using TexturePacker;

namespace TexturePackerGUI
{
    public partial class Form1 : Form
    {
        bool IsCmdMode = false;


        public Form1( string[] args )
        {
            InitializeComponent();

            TextureGUIPacker.Instance.SetShowAction(new Action<List<Bitmap>>(ShowImages));
            TextureGUIPacker.Instance.SetShowSelectedAction(new Action<TextureRect>(ShowSelectedImage));
            TextureGUIPacker.Instance.Init();
            CommandLineHandle( args );
        }

        private void CommandLineHandle( string[] args )
        {
            if ( null == args || args.Length<1 ){
                return;
            }

            IsCmdMode = true;

            foreach (var item in args){
                if (string.IsNullOrEmpty(item)){
                    ShowMessage( "Command line parameter error!" );
                    Environment.Exit( 0 );
                    return;
                }
            }

            //-pj工程目录
            //-pjupdate 0/1是否更新
            //-todir导出目录
            //-cfgtodir导出配置目录
            string[] cmds = { "-pj" , string.Empty , "-todir" , string.Empty, "-cfgtodir" , string.Empty , "-pjupdate" , "1" };

            Dictionary<string , string> _CommandPars = new Dictionary<string , string>();
            for (int i = 0 ; i < cmds.Length ; i++)
            {
                _CommandPars.Add( cmds[i++] , cmds[i] );
            }

            for (int i = 0 ; i < args.Length ; i++){
                var item = args[i];
                Console.WriteLine( item );
                if (_CommandPars.ContainsKey(item)){
                    if (i+1 < args.Length){
                        string str = args[i + 1];
                        if ("-todir" == item || "-cfgtodir" == item)
                        {
                            str = Path.GetFullPath( str );
                        }
                        _CommandPars[item] = str;
                    }
                }
            }

            foreach (var item in _CommandPars)
            {
                if (string.IsNullOrEmpty( item.Value ) )
                {
                    ShowMessage( string.Format( "{0} parameter error!" , item.Key ) );
                    Environment.Exit( 0 );
                    return;
                }

                bool err = false;

                if (item.Key == "-pj")
                {
                    if (!File.Exists( item.Value ))
                    {
                        err = true;
                    }
                }
                if (item.Key == "-todir" || item.Key == "-cfgtodir")
                {
                    if (!Directory.Exists( item.Value ))
                    {
                        err = true;
                    }
                }

                if (err)
                {
                    ShowMessage( string.Format( "{0} parameter error = {1} !" , item.Key , item.Value ) );
                    Environment.Exit( 0 );
                    return;
                }
            }

            TextureGUIPacker.Instance.SetTargetPath( _CommandPars["-todir"] );

            string targetpath = _CommandPars["-pj"];
            targetpath = Path.GetFullPath( targetpath );
            string extension = System.IO.Path.GetExtension( targetpath );

            try
            {
                if (extension == ".tps")
                {
                    TextureGUIPacker.Instance.OpenOldTextureProject( targetpath );
                }
                else if (extension == ".solo")
                {
                    TextureGUIPacker.Instance.OpenTextureProject( targetpath );
                }
                else
                    throw new Exception( "未知的文件类型" );
            }
            catch (Exception ee)
            {
                ShowMessage( ee.ToString() );
                Environment.Exit( 0 );
                return;
            }

            bool bSave = false;
            if (_CommandPars["-pjupdate"] == "1")
            {
                List<string> files = TextureGUIPacker.Instance.AllFiles;
                List<string> dirs = new List<string>();
                foreach (var item in files)
                {
                    string dir = Path.GetDirectoryName( item );
                    if (!dirs.Contains(dir))
                    {
                        dirs.Add( dir );
                    }
                }

                List<string> paths = new List<string>();
                foreach (var item in dirs)
                {
                    string [] pics =Directory.GetFiles( item , "*.png" );
                    foreach (string f in pics)
                    {
                        if (!paths.Contains( f ))
                        {
                            paths.Add( f );
                        }
                    }
                }

                TextureGUIPacker.Instance.InsertTexture( paths.ToArray() );
                bSave = true;
            }

            string solodir = Path.GetDirectoryName( targetpath );
            string[] selectedFiles=Directory.GetFiles( solodir , "*.solo" );
            TextureGUIPacker.Instance.PackTexturesToGUIAtlas( selectedFiles , _CommandPars["-cfgtodir"] );
            TextureGUIPacker.Instance.PackTexturesToGUIAtlasEx( selectedFiles[0] , _CommandPars["-cfgtodir"] );

            Button1_Click( null , null );

            if (bSave)
            {
                Console.WriteLine( "save solo" );
                TextureGUIPacker.Instance.SaveTextureProject();
            }

            Console.WriteLine( "exit" );
            Environment.Exit( 0 );
        }

        private void 打开项目ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Title = "请选择打开项目";
            fileDialog.Filter = "所有项目文件(*solo*)|*.solo*";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string filename = fileDialog.FileName;
            }
        }

        private void 添加精灵ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Title = "请选择精灵";
            fileDialog.Filter = "全部格式(*.bmp;*.gif;*.ico;*.jpeg;*.jpg;*.pbm;*.pgm;*.pkm;*.png;*.ppm;*.psd;*.pvr;*.pvr.ccz;*.pvr.gz;*.pvrtc;*.svg;*.svgz;*.swf;*.tga;*.tif;*.tiff;*.webp;*.xbm;*.xpm;)|";
            fileDialog.Multiselect = true;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] paths = fileDialog.FileNames;       
                
                this.精灵列表.BeginUpdate();
                TextureGUIPacker.Instance.InsertTexture(paths);

                foreach(var path in paths)
                {
                    this.精灵列表.Items.Add(Path.GetFileName(path));
                }

                this.精灵列表.EndUpdate();
            }
        }

        private Dictionary<string, Bitmap> m_PathToBitmap = new Dictionary<string, Bitmap>();
        private Dictionary<string, Bitmap> m_FileNameToBitmap = new Dictionary<string, Bitmap>();

        public void ShowSelectedImage(TextureRect texture)
        {
            this.m_ImagesSizes.Clear();

            this.panel1.Controls.Clear();

            int showWidth = 0;
            int showHeight = 0;

            float scale = 1.0f;
            if (texture.rect.width > this.panel1.Width)
                scale = texture.rect.width / this.panel1.Width;

            if (texture.rect.height > this.panel1.Height)
                scale = Math.Max(texture.rect.height / this.panel1.Height, scale);

            showWidth = (int)(texture.rect.width / scale);
            showHeight = (int)(texture.rect.height / scale);

            Boader boader = texture.texture.boader;

            textBox8.Text = boader.left.ToString();
            textBox9.Text = boader.right.ToString();
            textBox10.Text = boader.top.ToString();
            textBox11.Text = boader.bottom.ToString();

            Bitmap resizedImage = new Bitmap(texture.texture.bitmap, new Size(showWidth, showHeight));

            using (Graphics gResized = Graphics.FromImage(resizedImage))
            {
                //gResized.Clear(Color.White);

                //gResized.DrawImage(resizedImage, 0, 0, showWidth, showHeight);
                //gResized.DrawImage(resizedImage, new PointF(0, 0));

                int leftboader = (int)(boader.left / scale);
                int rightboader = (int)(boader.right / scale);
                int topboader = (int)(boader.top / scale);
                int bottomboader = (int)(boader.bottom / scale);

                Pen greenpen = new Pen(new SolidBrush(Color.Green));
                if (leftboader != 0)
                    gResized.DrawLine(greenpen, new Point(leftboader, 0), new Point(leftboader, resizedImage.Height));
                if (rightboader != 0)
                    gResized.DrawLine(greenpen, new Point(resizedImage.Width - rightboader, 0), new Point(resizedImage.Width - rightboader, resizedImage.Height));
                if (topboader != 0)
                    gResized.DrawLine(greenpen, new Point(0, topboader), new Point(resizedImage.Width, topboader));
                if (bottomboader != 0)
                    gResized.DrawLine(greenpen, new Point(0, resizedImage.Height - bottomboader), new Point(resizedImage.Width, resizedImage.Height - bottomboader));
            }

            int paddingx = (this.panel1.Width - resizedImage.Width) / 2;
            int paddingy = (this.panel1.Height - resizedImage.Height) / 2;

            PictureBox pictureBox = new PictureBox();
            pictureBox.Size = new Size(showWidth + 5, showHeight + 5);
            pictureBox.Location = new Point(paddingx, paddingy);
            pictureBox.Visible = true;
            pictureBox.BackgroundImage = resizedImage;
            pictureBox.BackgroundImageLayout = ImageLayout.Center;
            pictureBox.BackColor = Color.Black;

            this.panel1.Controls.Add(pictureBox);

            this.textBox8.Text = texture.texture.boader.left.ToString();
            this.textBox9.Text = texture.texture.boader.right.ToString();
            this.textBox10.Text = texture.texture.boader.top.ToString();
            this.textBox11.Text = texture.texture.boader.bottom.ToString();
        }

        public void ShowImages(List<Bitmap> images)
        {
            this.panel1.Controls.Clear();

            int rows = images.Count / 3 + 1;
            int padding = 10;

            int minx = this.panel1.Location.X + padding;
            int miny = this.panel1.Location.Y + padding;
            int maxx = this.panel1.Location.X + this.panel1.Size.Width - padding;
            int maxy = this.panel1.Location.Y + this.panel1.Size.Height - padding;
            int maxWidth = maxx - minx;
            int maxHeight = maxy - miny;

            List<List<Bitmap>> splitImages = SplitList(images, 3);

            int rowSize = splitImages.Count;
            for(int row = 0; row != splitImages.Count; ++row)
            {
                int colSize = splitImages[row].Count;
                for(int col = 0; col != colSize; ++col)
                {
                    int width = maxWidth / colSize;
                    int height = maxHeight / rowSize;
                    int x = width * col;
                    int y = height * row;

                    float xScale = (float)(splitImages[row][col].Width) / (float)width;
                    float yScale = (float)(splitImages[row][col].Height) / (float)height;

                    float scale = Math.Max(xScale, yScale);

                    int resizedWidth = splitImages[row][col].Width;
                    int resizedHieght = splitImages[row][col].Height;
                    if (scale > 1.0f)
                    {
                        resizedWidth = (int)(splitImages[row][col].Width / scale);
                        resizedHieght = (int)(splitImages[row][col].Height / scale);
                    }
                    
                    Bitmap resized = new Bitmap(splitImages[row][col], new Size(resizedWidth, resizedHieght));

                    using (Graphics gResized = Graphics.FromImage(resized))
                    {
                        gResized.Clear(Color.White);
                        gResized.DrawImage(splitImages[row][col], 0, 0, resizedWidth, resizedHieght);
                    }

                    PictureBox pictureBox = new PictureBox();
                    pictureBox.Size = new Size(resizedWidth + 5, resizedHieght + 5);
                    pictureBox.Location = new Point(x, y);
                    //pictureBox.Location = new Point(10, 10);
                    //pictureBox.Image = resized;
                    pictureBox.Visible = true;
                    pictureBox.BackgroundImage = resized;
                    pictureBox.BackgroundImageLayout = ImageLayout.Center;
                    pictureBox.BackColor = Color.Black;
                    pictureBox.MouseEnter += new EventHandler(OnPictureBoxMouseEnter);

                    m_ImagesSizes[new Point(x, y)] = new Point(splitImages[row][col].Width, splitImages[row][col].Height);

                    this.panel1.Controls.Add(pictureBox);
                }
            }
        }

        public Dictionary<Point, Point> m_ImagesSizes = new Dictionary<Point, Point>();

        public void OnPictureBoxMouseEnter(object sender, EventArgs e)
        {
            PictureBox nowpicture = (PictureBox)sender;
            this.textBox3.Text = m_ImagesSizes[nowpicture.Location].X.ToString();
            this.textBox7.Text = m_ImagesSizes[nowpicture.Location].Y.ToString();
        }

        public void OnPictureBoxMouseClick(object sender, EventArgs e)
        {
            PictureBox nowpicture = (PictureBox)sender;

        }

        public List<List<Bitmap>> SplitList (List<Bitmap> me, int size = 3)
        {
            var list = new List<List<Bitmap>>();
            for (int i = 0; i < me.Count; i += size)
            {
                list.Add(me.GetRange(i, Math.Min(size, me.Count - i)));
            }

            return list;
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void 精灵列表_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Listbox1_Clicked(object sender, EventArgs e)
        {
            ListBox listBox = (ListBox)sender;

            TextureGUIPacker.Instance.ShowSelectedTextureRect(listBox.SelectedIndex);

            m_IsShowSelectedImage = true;

            return;
        }

        private void 帮助ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void Label2_Click(object sender, EventArgs e)
        {

        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //目前只支持一种算法
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.comboBox1.Items.Add("MaxRects");
            this.comboBox1.SelectedIndex = 0;

            this.comboBox2.Items.Add("BestAreaFit");
            this.comboBox2.Items.Add("BottomLeftFit");
            this.comboBox2.SelectedIndex = 0;

            this.comboBox3.Items.Add("32");
            this.comboBox3.Items.Add("64");
            this.comboBox3.Items.Add("128");
            this.comboBox3.Items.Add("256");
            this.comboBox3.Items.Add("512");
            this.comboBox3.Items.Add("1024");
            this.comboBox3.Items.Add("2048");
            this.comboBox3.Items.Add("4096");
            this.comboBox3.SelectedIndex = 5;
        }

        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            FreeRectChoiceHeuristic heuristic = (FreeRectChoiceHeuristic)this.comboBox2.SelectedIndex;

            TextureGUIPacker.Instance.SetHeuristic(heuristic);
        }

        private void Label4_Click(object sender, EventArgs e)
        {

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            ErrorCode ec = TextureGUIPacker.Instance.SaveToFiles();

            if (ec == ErrorCode.ERROR_NO)
                ShowMessage("导出成功");
            else if (ec == ErrorCode.ERROR_TARGET_NAME)
            {
                ShowMessage("请先保存或打开项目后再进行导出");
            }
            else if (ec == ErrorCode.ERROR_TARGET_PATH)
                ShowMessage("导出路径设置错误");
        }

        public void ShowMessage( string str )
        {
            if (IsCmdMode)
            {
                Console.WriteLine( str );
            }
            else
            {
                MessageBox.Show( str );
            }
        }

        private void Label5_Click(object sender, EventArgs e)
        {

        }

        private void ComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            int maxSize = 0;
            try
            {
                maxSize = Convert.ToInt32(this.comboBox3.Text);
            }
            catch (Exception ee)
            {
                return;
            }

            TextureGUIPacker.Instance.SetMaxBinSize(maxSize);
        }

        private void Label8_Click(object sender, EventArgs e)
        {

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string filepath = folderBrowserDialog.SelectedPath;

                TextureGUIPacker.Instance.SetTargetPath(filepath);

                this.textBox2.Text = filepath;
            }
        }

        private void FileSystemWatcher1_Changed(object sender, FileSystemEventArgs e)
        {

        }

        private void 新建项目ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.精灵列表.BeginUpdate();
            this.精灵列表.Items.Clear();
            this.精灵列表.EndUpdate();


            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "项目文件(*.solo)|*.solo";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;
                if (!filePath.Contains(".solo"))
                    filePath += ".solo";
                try
                {
                    TextureGUIPacker.Instance.CreateTextureProject(filePath);
                }
                catch(Exception ee)
                {
                    ShowMessage(ee.ToString());
                    return;
                }

                ShowMessage("新建项目成功");
            }
            /*
            Form1 newOne = new Form1();

            newOne.Text = this.Text;
            newOne.Height = this.Height;
            newOne.Width = this.Width;
            newOne.Show();
            */
        }

        private void 删除精灵ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.精灵列表.BeginUpdate();
            this.精灵列表.Items.Clear();
            this.精灵列表.EndUpdate();

            TextureGUIPacker.Instance.Init();
        }

        private void TextBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            TextureGUIPacker.Instance.SetTargetPath(textBox2.Text);
        }

        private void 打开项目ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            this.精灵列表.BeginUpdate();
            this.精灵列表.Items.Clear();
            this.精灵列表.EndUpdate();

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "项目文件(*.solo, *.tps)|*.solo;*.tps";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string targetpath = openFileDialog.FileName;

                string extension = System.IO.Path.GetExtension(targetpath);

                try
                {
                    if (extension == ".tps")
                    {
                        TextureGUIPacker.Instance.OpenOldTextureProject(targetpath);
                    }
                    else if (extension == ".solo")
                    {
                        TextureGUIPacker.Instance.OpenTextureProject(targetpath);
                    }
                    else
                        throw new Exception("未知的文件类型");
                }
                catch(Exception ee)
                {
                    ShowMessage(ee.ToString());
                    return;
                }
            }

            this.精灵列表.BeginUpdate();
            foreach(var path in TextureGUIPacker.Instance.m_FilePaths)
            {
                this.精灵列表.Items.Add(Path.GetFileNameWithoutExtension(path));
            }
            this.精灵列表.EndUpdate();
        }

        private void 保存项目ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(TextureGUIPacker.Instance.m_ProjectPath))
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "项目文件(*.solo)|*.solo";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (saveFileDialog.FileName != string.Empty)
                    {
                        TextureGUIPacker.Instance.m_ProjectPath = saveFileDialog.FileName;
                    }
                }
            }

            string extension = System.IO.Path.GetExtension(TextureGUIPacker.Instance.m_ProjectPath);
            if (extension == ".tps")
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "项目文件(*.solo)|*.solo";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (saveFileDialog.FileName != string.Empty)
                    {
                        TextureGUIPacker.Instance.m_ProjectPath = saveFileDialog.FileName;
                    }
                }
            }

            try
            {
                TextureGUIPacker.Instance.SaveTextureProject();
            }
            catch (Exception ee)
            {
                ShowMessage(ee.ToString());
                return;
            }

            ShowMessage("保存成功");
        }

        private void 添加文件夹ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string filepath = folderBrowserDialog.SelectedPath;

                List<string> paths = new List<string>();

                this.精灵列表.BeginUpdate();
                foreach(string f in Directory.GetFiles(filepath))
                {
                    if (f.Contains(".png") || f.Contains(".jpeg") || f.Contains(".jpg"))
                    {
                        paths.Add(f);
                    }

                    this.精灵列表.Items.Add(Path.GetFileNameWithoutExtension(f));
                }

                this.精灵列表.EndUpdate();

                TextureGUIPacker.Instance.InsertTexture(paths.ToArray());
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox6.Text))
            {
                ShowMessage("请输入UIAtlas导出目录");
                return;
            }

            string UIAtlasPath = this.textBox6.Text;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "请选择项目文件";
            openFileDialog.Filter = "项目文件(*solo*)|*.solo*";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] selectedFiles = openFileDialog.FileNames;

                try
                {
                    TextureGUIPacker.Instance.PackTexturesToGUIAtlas(selectedFiles, UIAtlasPath);
                }
                catch (Exception ee)
                {
                    ShowMessage(ee.ToString());
                    return;
                }

                ShowMessage("导出UIAltas成功");
            }
        }

        private void Label10_Click(object sender, EventArgs e)
        {

        }

        private void TextBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private bool m_IsShowSelectedImage = false;

        private void Button4_Click(object sender, EventArgs e)
        {
            TextureGUIPacker.Instance.ReShow();
            m_IsShowSelectedImage = false;
        }

        private void Label16_Click(object sender, EventArgs e)
        {

        }

        private void TextBox8_TextChanged(object sender, EventArgs e)
        {
            if (!m_IsShowSelectedImage)
                return;

            int size = 0;
            if (!int.TryParse(textBox8.Text, out size))
                return;

            TextureGUIPacker.Instance.SetTextureRectSingleBoader(1, size, this.精灵列表.SelectedIndex);

            lastSelectedBox = textBox8;
        }

        private void TextBox9_TextChanged(object sender, EventArgs e)
        {
            if (!m_IsShowSelectedImage)
                return;

            int size = 0;
            if (!int.TryParse(textBox9.Text, out size))
                return;

            TextureGUIPacker.Instance.SetTextureRectSingleBoader(2, size, this.精灵列表.SelectedIndex);

            lastSelectedBox = textBox9;
        }

        private void TextBox10_TextChanged(object sender, EventArgs e)
        {
            if (!m_IsShowSelectedImage)
                return;

            int size = 0;
            if (!int.TryParse(textBox10.Text, out size))
                return;

            TextureGUIPacker.Instance.SetTextureRectSingleBoader(3, size, this.精灵列表.SelectedIndex);

            lastSelectedBox = textBox10;
        }

        private void TextBox11_TextChanged(object sender, EventArgs e)
        {
            if (!m_IsShowSelectedImage)
                return;

            int size = 0;
            if (!int.TryParse(textBox11.Text, out size))
                return;

            TextureGUIPacker.Instance.SetTextureRectSingleBoader(4, size, this.精灵列表.SelectedIndex);

            lastSelectedBox = textBox11;
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            if (!m_IsShowSelectedImage)
                return;

            Boader boader = new Boader();
            boader.left = Convert.ToInt32(textBox8.Text);
            boader.right = Convert.ToInt32(textBox9.Text);
            boader.top = Convert.ToInt32(textBox10.Text);
            boader.bottom = Convert.ToInt32(textBox11.Text);

            TextureGUIPacker.Instance.SetTextureRectBoader(boader, this.精灵列表.SelectedIndex);
        }

        public System.Windows.Forms.TextBox lastSelectedBox = null;

        private void HScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            if (lastSelectedBox != null)
            {
                HScrollBar hScroll = (HScrollBar)sender;

                TextureRect selected = TextureGUIPacker.Instance.GetTextureRectSelected(this.精灵列表.SelectedIndex);
                if (selected == null)
                    return;

                if (this.lastSelectedBox == null)
                    return;

                //left
                if (this.lastSelectedBox == textBox8 || this.lastSelectedBox == textBox9)
                {
                    hScroll.Maximum = selected.rect.width;
                    hScroll.Minimum = 0;
                }
                else if (this.lastSelectedBox == textBox10 || this.lastSelectedBox == textBox11)
                {
                    hScroll.Maximum = selected.rect.height;
                    hScroll.Minimum = 0;
                }

                try
                {
                    this.lastSelectedBox.Text = hScroll.Value.ToString();
                }
                catch (Exception ee)
                {

                }
            }
        }
    }

    
}
