using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using Sewer56.BitStream.ByteStreams;
using Sewer56.BitStream;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Scrambler_align3.Classes;
using System.IO;
using rawbitstream;
using System.Threading;

namespace Scrambler_align3
{

    public partial class AlignmentControl : UserControl
    {
        protected string fileName;
        protected byte[] newDataArray;
        protected string initialDirectory;
        protected int dotCount;
        public long countOfUnits;
        public int unitLength;
        public string outputFilePath;
        public MainForm mainForm;
        public AlignmentControl(MainForm form)
        {
            InitializeComponent();
            mainForm = form;
            Manager.UsedAlignmentControl = this;
            this.Click += new System.EventHandler(this.AlignmentControl_Click);

            saveFileDialog1.Filter = "Binary files(*.bin)|*.bin";
            progressBar2.Visible = false;
            label2.Visible = false;
            label4.Visible = false;
            label5.Visible = false;
            progressBar2.Maximum = 100;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            try
            {
                openFileDialog1.FileName = "";
                if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;
                textBox1.Text = openFileDialog1.FileName;
                fileName = openFileDialog1.FileName;
                initialDirectory = Path.GetDirectoryName(fileName);
                outputFilePath = Path.Combine(initialDirectory, Path.GetFileName(fileName) + ".align");
                textBox2.Text = outputFilePath;

            }
            finally
            {
                openFileDialog1.Dispose();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            try
            {
                saveFileDialog1.InitialDirectory = initialDirectory;
                saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(this.fileName) + ".align";

                if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;

                textBox2.Text = saveFileDialog1.FileName;
                outputFilePath = saveFileDialog1.FileName;
            }
            finally
            {
                saveFileDialog1.Dispose();
            }


        }
        public async void button2_Click_1(object sender, EventArgs e)
        {
            outputFilePath = textBox2.Text;
            unitLength = 0;
            label1.Visible = true;
            label2.Visible = false;
            label5.Visible = true;
            label5.Text = "Processing";
            progressBar2.Value = 0;

            timer1.Interval = 200; // Интервал между сменой точек (в миллисекундах)
            timer1.Start();
            timer1.Tick += timer1_Tick;
            progressBar2.Visible = true;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            AlignmentFinal af = new AlignmentFinal();
            var progress = new Progress<int>();
            progress.ProgressChanged += af.OnProgressChange;
            timer1.Stop();
            (int soFirstFrameOffset, int frameSize, bool inverted) = await Task.Run(() => af.Alignment(fileName, outputFilePath, progress));
            Application.DoEvents();
            label1.Visible = false;
            label2.Visible = true;
            stopWatch.Stop();

            progressBar2.Visible = false;
        }

        public void timer1_Tick(object sender, EventArgs e)
        {
            dotCount++;
            if (dotCount > 3)
            {
                dotCount = 0; // сбросить счетчик, если достигнуто максимальное количество точек
            }

            UpdateLabel();
        }
        public void UpdateLabel()
        {
            string dots = "";

            for (int i = 0; i < dotCount; i++)
            {
                dots += ". ";
            }
            label1.Text = dots;
        }

        public void AlignmentControl_Click(object sender, EventArgs e)
        {
            mainForm.panel1.Hide();
        }
    }
}
