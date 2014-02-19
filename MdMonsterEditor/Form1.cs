﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using ManicDigger;
using System.Diagnostics;
using ManicDigger.Renderers;
using System.IO;

namespace MdMonsterEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        IGetFileStream getfile;
        private void Form1_Load(object sender, EventArgs e)
        {
            string[] datapaths = new[] { Path.Combine(Path.Combine(Path.Combine("..", ".."), ".."), "data"), "data" };
            getfile = new GetFileStream(datapaths);
            RichTextBoxContextMenu(richTextBox1);
            RichTextBoxContextMenu(richTextBox2);
            UpdateLabels();
            the3d = new ManicDigger.The3d() { d_Config3d = config3d };
            glControl1.Paint += new PaintEventHandler(glControl1_Paint);
            glControl1.MouseWheel += new System.Windows.Forms.MouseEventHandler(glControl1_MouseWheel);
            loaded = true;
            GL.ClearColor(Color.SkyBlue);
            overheadcameraK.Distance = 3;
            SetupViewport();
            Application.Idle += new EventHandler(Application_Idle);
            sw.Start();
        }
        private void RichTextBoxContextMenu(RichTextBox richTextBox)
        {
            ContextMenu cm = new ContextMenu();
            MenuItem mi = new MenuItem("Cut");
            mi.Click += (a, b) => { richTextBox.Cut(); };
            cm.MenuItems.Add(mi);

            mi = new MenuItem("Copy");
            mi.Click += (a, b) =>
                {
                    richTextBox.Copy();
                };
            cm.MenuItems.Add(mi);

            mi = new MenuItem("Paste");
            mi.Click += (a, b) =>
                {
                    richTextBox.Paste(DataFormats.GetFormat(DataFormats.UnicodeText));
                };
            cm.MenuItems.Add(mi);

            richTextBox.ContextMenu = cm;
        }
        private void UpdateLabels()
        {
            label1.Text = string.Format("Heading: {0} degrees.", HeadingDeg());
            label2.Text = string.Format("Pitch: {0} degrees.", PitchDeg());
            label3.Text = string.Format("Speed: {0} / second.", trackBar3.Value * 0.1);
        }
        private float HeadingDeg()
        {
            return -trackBar1.Value * 30;
        }
        private float PitchDeg()
        {
            return -trackBar2.Value * 15;
        }
        Stopwatch sw = new Stopwatch();
        void Application_Idle(object sender, EventArgs e)
        {
            // no guard needed -- we hooked into the event in Load handler

            sw.Stop(); // we've measured everything since last Idle run
            double milliseconds = sw.Elapsed.TotalMilliseconds;
            sw.Reset(); // reset stopwatch
            sw.Start(); // restart stopwatch

            dt = (float)(milliseconds / 1000);

            glControl1.Invalidate();
        }
        float dt;
        void glControl1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                overheadcameraK.Distance -= 0.002f * e.Delta;
                glControl1.Invalidate();
            }
        }
        bool loaded;
        int playertexture = -1;
        void glControl1_Paint(object sender, PaintEventArgs e)
        {
            Render();
        }
        private void Render()
        {
            if (!loaded)
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            /*
            GL.Color3(Color.Yellow);
            GL.Begin(BeginMode.Triangles);
            GL.Vertex2(10, 20);
            GL.Vertex2(100, 20);
            GL.Vertex2(100, 50);
            GL.End();
            */
            OverheadCamera();
            if (playertexture == -1)
            {
                LoadPlayerTexture(MyStream.ReadAllBytes(getfile.GetFile("mineplayer.png")));
            }
            DrawGrass();
            DrawAxisLine(new Vector3(), HeadingDeg(), PitchDeg());
            GL.Enable(EnableCap.Texture2D);
            bool exception = false;
            byte headingbyte = (byte)(HeadingDeg() / 360 * 256);
            byte pitchbyte = (byte)(PitchDeg() / 360 * 256);
            float speed = 1.0f;
            d.AnimPeriod = 1.0 / (trackBar3.Value * 0.1);
            progressBar1.Value = (int)((animstate.interp % (d.AnimPeriod)) / d.AnimPeriod * 100);
            try
            {
                d.DrawCharacter(animstate, new OpenTK.Vector3(0, 0, 0),
                   headingbyte, pitchbyte, true, dt, playertexture, new AnimationHint(), speed);
            }
            catch (Exception ee)
            {
                if (richTextBox2Text != ee.ToString())
                {
                    richTextBox2Text = ee.ToString();
                    richTextBox2.Text = ee.ToString();
                }
                exception = true;
            }
            if (!exception)
            {
                richTextBox2.Text = "";
                richTextBox2Text = "";
            }

            glControl1.SwapBuffers();
        }
        string richTextBox2Text = "";
        private void DrawAxisLine(Vector3 v, float myheadingdeg, float mypitchdeg)
        {
            GL.Disable(EnableCap.Texture2D);
            
            //GL.Rotate(HeadingByteToOrientationY(headingbyte), 0, 1, 0);
            //GL.Rotate(PitchByteToOrientationX(pitchbyte), 1, 0, 0);
            GL.PushMatrix();
            GL.Translate(v);
            GL.Rotate(myheadingdeg, 0, 1, 0);
            GL.Rotate(mypitchdeg, 0, 0, 1);

            GL.Begin(BeginMode.Lines);            
            GL.Color3(Color.Red);
            GL.Vertex3(0, 0.1, 0);
            GL.Vertex3(2, 0.1, 0);
            GL.Color3(Color.Green);
            GL.Vertex3(0, 0.1, 0);
            GL.Vertex3(0, 2, 0);
            GL.Color3(Color.Blue);
            GL.Vertex3(0, 0.1, 0);
            GL.Vertex3(0, 0.1, 2);
            GL.End();
            GL.Color3(Color.White);
            GL.PopMatrix();
        }
        private double PitchByteToOrientationX(byte pitchbyte)
        {
            return (((float)pitchbyte / 256) * 360);
        }
        private double HeadingByteToOrientationY(byte headingbyte)
        {
            return ((((float)headingbyte) / 256) * -360) - 90;
        }
        IThe3d the3d;
        int grasstexture = -1;
        private void DrawGrass()
        {
            if (grasstexture == -1)
            {
                grasstexture = LoadTexture(getfile.GetFile("grass.png"));
            }
            GL.BindTexture(TextureTarget.Texture2D, grasstexture);
            GL.Enable(EnableCap.Texture2D);
            GL.Color3(Color.White);
            GL.Begin(BeginMode.Quads);
            Rectangle r = new Rectangle(-10, -10, 20, 20);
            DrawWaterQuad(r.X, r.Y, r.Width, r.Height, 0);
            GL.End();
        }
        public int LoadTexture(Stream file)
        {
            using (file)
            {
                using (Bitmap bmp = new Bitmap(file))
                {
                    return the3d.LoadTexture(bmp);
                }
            }
        }
        private void LoadPlayerTexture(byte[] file)
        {
            playertexture = the3d.LoadTexture(new Bitmap(new MemoryStream(file)));
        }
        void DrawWaterQuad(float x1, float y1, float width, float height, float z1)
        {
            RectangleF rect = new RectangleF(0, 0, 1 * width, 1 * height);
            float x2 = x1 + width;
            float y2 = y1 + height;
            GL.TexCoord2(rect.Right, rect.Bottom); GL.Vertex3(x2, z1, y2);
            GL.TexCoord2(rect.Right, rect.Top); GL.Vertex3(x2, z1, y1);
            GL.TexCoord2(rect.Left, rect.Top); GL.Vertex3(x1, z1, y1);
            GL.TexCoord2(rect.Left, rect.Bottom); GL.Vertex3(x1, z1, y2);
        }
        CharacterRendererMonsterCode d = new CharacterRendererMonsterCode() { game = new ManicDiggerGameWindow() };
        //CharacterDrawerBlock d = new CharacterDrawerBlock();
        AnimationState animstate = new AnimationState();
        Config3d config3d = new Config3d();
        Kamera overheadcameraK = new Kamera();
        Vector3 up = new Vector3(0f, 1f, 0f);
        private void SetupViewport()
        {
            int w = glControl1.Width;
            int h = glControl1.Height;
            /*
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, w, 0, h, -1, 1); // Bottom-left corner pixel has coordinate (0, 0)
            GL.Viewport(0, 0, w, h); // Use all of the glControl painting area
            */
            float aspect_ratio = Width / (float)Height;
            Matrix4 perpective = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, znear, zfar);
            //Matrix4 perpective = Matrix4.CreateOrthographic(800 * 0.10f, 600 * 0.10f, 0.0001f, zfar);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perpective);
            OverheadCamera();
        }
        private void OverheadCamera()
        {
            GL.MatrixMode(MatrixMode.Modelview);
            var camera = Matrix4.LookAt(overheadcameraK.Position, overheadcameraK.Center, up);
            GL.LoadMatrix(ref camera);
        }
        float znear = 0.1f;
        float zfar { get { return ENABLE_ZFAR ? config3d.viewdistance * 3f / 4 : 99999; } }
        bool ENABLE_ZFAR = false;
        public float fov = MathHelper.PiOver3;
        int oldmousex = 0;
        int oldmousey = 0;
        private void glControl1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            oldmousex = e.X;
            oldmousey = e.Y;
            down = true;
        }
        bool down = false;
        private void glControl1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (down)
            {
                int deltax = e.X - oldmousex;
                int deltay = e.Y - oldmousey;
                oldmousex = e.X;
                oldmousey = e.Y;
                overheadcameraK.tt += (float)deltax * 0.05f;
                overheadcameraK.Angle += (float)deltay * 1f;
                if (overheadcameraK.Angle > 89) { overheadcameraK.Angle = 89; }
                if (overheadcameraK.Angle < -89) { overheadcameraK.Angle = -89; }
                glControl1.Invalidate();
            }
        }
        private void glControl1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            down = false;
        }
        private void glControl1_MouseHover(object sender, EventArgs e)
        {
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            UpdateLabels();
        }
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            UpdateLabels();
        }
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            d.Load(new List<string>(richTextBox1.Lines));
            listBox1.Items.Clear();
            listBox1.Items.AddRange(d.Animations());
        }
        private void trackBar3_ValueChanged(object sender, EventArgs e)
        {
            UpdateLabels();
        }
        private void trackBar3_Scroll(object sender, EventArgs e)
        {
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            d.SetAnimation((string)listBox1.SelectedItem);
        }
        private void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
        }
        private void loadTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                LoadPlayerTexture(File.ReadAllBytes(openFileDialog1.FileName));
            }
        }

        private void loadModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog2.ShowDialog();
            if (result == DialogResult.OK)
            {
                richTextBox1.Text = File.ReadAllText(openFileDialog2.FileName);
            }
        }
    }
}
