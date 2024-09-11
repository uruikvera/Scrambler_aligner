using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sewer56.BitStream.ByteStreams;
using Sewer56.BitStream;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Scrambler_align3.Classes;

namespace Scrambler_align3
{
    public partial class ScramblerControl : UserControl
    {
        protected string filePath;
        public string outputFilePath;
        protected string initialDirectory;
        protected byte[] NewdataArray;
        protected bool intelType = true;
        protected Animetion animetion;
        protected int dotCount;

        private MainForm mainForm;
        public ScramblerControl(MainForm form)
        {
            InitializeComponent();
            mainForm = form;
            Manager.UsedScramblerControl = this;
            this.Click += new System.EventHandler(this.ScramblerControl_Click);

            saveFileDialog1.Filter = "Binary files(*.bin)|*.bin|Dat files(*.dat)|*.dat";
            progressBar1.Visible = false;
            label2.Visible = false;
            label3.Visible = false;
            animetion = new Animetion();
        }

        public void button1_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            try
            {
                openFileDialog1.FileName = "";
                if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;

                textBox1.Text = openFileDialog1.FileName;
                filePath = openFileDialog1.FileName;
                initialDirectory = Path.GetDirectoryName(filePath);
                outputFilePath = Path.Combine(initialDirectory, Path.GetFileName(filePath) + ".descr");
                textBox2.Text = outputFilePath;
            }
            finally
            {
                openFileDialog1.Dispose();
            }

        }
        public async void button2_Click_1(object sender, EventArgs e)
        {
            outputFilePath = textBox2.Text;
            label2.Text = "Processing";
            label3.Visible = true;
            timer1.Interval = 200; // Интервал между сменой точек (в миллисекундах)
            timer1.Start();
            timer1.Tick += timer1_Tick;

            var progress = new Progress<int>();
            Scrambler scrambler = new Scrambler();
            progress.ProgressChanged += scrambler.OnProgressChange;
            progressBar1.Visible = true;
            label2.Visible = true;

            progressBar1.Value = 0;
            progressBar1.Maximum = 100;

            string polynomial = polynomialField.Text;
            string unitLenght = unit.Text;
            string correctedPolynom = CheckCorrect.CorrectPolynom(polynomial);
            if (correctedPolynom == null)
            {
                polynomialField.Text = "";
                return;
            }
            int correctedUnit = CheckCorrect.CorrectUnit(unitLenght);
            if (correctedUnit == 0)
            {
                unit.Text = "";
                return;
            }
            FileInfo fileInfo = new FileInfo(filePath);

            await Task.Run(() => scrambler.Scram(filePath, outputFilePath, progress, correctedPolynom, correctedUnit));
            Application.DoEvents();

            label2.Text = "Файл заскремблирован";
            timer1.Stop(); // Остановка таймера
            label3.Text = "";
            progressBar1.Visible = false;
        }
        public void timer1_Tick(object sender, EventArgs e)
        {
            dotCount++;
            if (dotCount > 3) // максимальное количество точек
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

            label3.Text = dots;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            try
            {
                saveFileDialog1.InitialDirectory = initialDirectory;
                saveFileDialog1.FileName = Path.GetFileName(filePath);
                saveFileDialog1.Filter = "Descrambled Files (*.descr)|*.descr|Scrambled Files (*.scr)|*.scr";
                saveFileDialog1.FilterIndex = 1;
                //saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;

                // Получаем выбранный тип файла из фильтра
                string fileType = saveFileDialog1.Filter.Split('|')[saveFileDialog1.FilterIndex * 2 - 1];

                // Получаем имя файла без расширения
                string fileName = Path.GetFileNameWithoutExtension(saveFileDialog1.FileName);

                // Формируем новое имя файла с добавлением типа файла
                if (fileType == "*.scr")
                {
                    outputFilePath = Path.Combine(initialDirectory, fileName + ".scr");
                }
                else { outputFilePath = Path.Combine(initialDirectory, fileName + ".descr");
                }

                textBox2.Text = outputFilePath;
            }
            finally
            {
                saveFileDialog1.Dispose();
            }
        }
        class CheckCorrect
        {
            public static string CorrectPolynom(string input)
            {
                // Проверка на количество элементов и наличие символов кроме 0 и 1
                if (input.Length > 17 || !IsBinaryString(input))
                {
                    MessageBox.Show("Некорректно введён полином\n(превышает допустимую степень или содержить другие символы кроме 0 и 1)");
                    return null;
                }

                // Дополнение строки нулями в начале, если она короче 17 символов
                while (input.Length < 17)
                {
                    input = "0" + input;
                }
                return input;
            }
            public static int CorrectUnit(string unitLenght)
            {
                int output = 0; // Значение по умолчанию
                try
                {
                    output = int.Parse(unitLenght);
                    return output;
                }
                catch (FormatException)
                {
                    MessageBox.Show("Некорректно введён размер кадра");
                    return 0;
                }
            }
            static bool IsBinaryString(string input)
            {
                foreach (char c in input)
                {
                    if (c != '0' && c != '1')
                    {
                        return false;
                    }
                }
                return true;
            }

        }

        public void ScramblerControl_Click(object sender, EventArgs e)
        {
            mainForm.panel2.Hide();
        }
    }
}
