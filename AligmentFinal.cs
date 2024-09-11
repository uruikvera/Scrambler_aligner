using Sewer56.BitStream.ByteStreams;
using Sewer56.BitStream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using rawbitstream;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Scrambler_align3.Classes
{
    public class AlignmentFinal
    {
        public int frameSize;
        public int firstFrameOffset;
        public int secondFrameOffset;
        public byte[] trueFas;
        public bool isReversed = false;
        public int fasCount = 0;
        public int frameCount = 0;
        bool exitBothLoops = false;
        public byte MFAS = 0x00;
        public int firstMFAS = 0;
        public int fasErrors = 3;
        public int FASlosts = 0;
        public int MFASlosts = 0;
        public int limitFasErrInRow = 0;
        public int MFASerrors = 11;
        public bool lastFrame = false;
        public int bitOffset = 0;
        public int errors = 0;
        public int indexer = 0;
        public int flag = 0;

        public bool findFirstMFAS = false;
        public int lastMFAS;
        public int lastLastMFAS = 0;
        public int buff = 0;
        public int MFASCounter = 0;
        public Tuple<int, int, bool>  Alignment(string filePath, string outputFilePath, IProgress<int> progress)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (FileStream outputfs = File.Open(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                ResetParameters();
                
                long lengthOfFile = new FileInfo(filePath).Length;
                long lengthOfFileFixed = new FileInfo(filePath).Length;
                int bytesToRead = 180000 * 6; 
                byte[] bufferForBit = new byte[bytesToRead]; // Инициализация буфера, через который определяем размер кадра и заголовок
                bufferForBit = ReadBinaryData(fs, bytesToRead/8);

                (frameSize, int soFirstFrameOffset, trueFas) = Info(bufferForBit);

                firstMFAS += soFirstFrameOffset;
                bytesToRead = frameSize/8 + 1 + trueFas.Length/8;
                bufferForBit = new byte[bytesToRead * 8];
                fs.Seek(soFirstFrameOffset / 8, SeekOrigin.Begin);
                lengthOfFile -= soFirstFrameOffset / 8;

                int futureMux = (int)(lengthOfFile / ((frameSize / 8) * 100));

                while (lengthOfFile > 0 && fs.Position < lengthOfFileFixed)
                {
                    if ((bytesToRead < frameSize / 8) || flag == -1)
                    {
                        progress.Report(100);
                        break;//конец записи больше нет целых кадров 
                    }

                    bufferForBit = new byte[bytesToRead * 8];
                    bufferForBit = ReadBinaryData(fs, bytesToRead);
                    (firstFrameOffset, secondFrameOffset) = FindOneFrame(bufferForBit, trueFas);

                    if (findFirstMFAS == true)
                    {
                        WriteBinaryData(bufferForBit, firstFrameOffset, frameSize, outputfs);
                        UpdateAlignmentControl(soFirstFrameOffset);
                    }
                    else firstMFAS += frameSize;

                    lengthOfFile -= frameSize/8;
                    if (lengthOfFile < bytesToRead)
                    {
                        bytesToRead = (int)lengthOfFile;
                        lastFrame = true;
                    }
                    fs.Seek(-(trueFas.Length / 8 + 1), SeekOrigin.Current);

                    progress.Report((int)((lengthOfFileFixed-lengthOfFile)*100 / lengthOfFileFixed));
                    Application.DoEvents();
                }
                return new Tuple<int, int, bool>(soFirstFrameOffset, frameSize, isReversed);
            }
        }
        private void ResetParameters()
        {
            flag = 0;
            limitFasErrInRow = 0;
            lastFrame = false;
            findFirstMFAS = false;
        }
        public Tuple<int, int> FindOneFrame(byte[] firstBlock, byte[] trueFas)
        {
            firstFrameOffset = FindePattern(firstBlock, trueFas, 0, 8);//поиск в первом байте начало FAS
            secondFrameOffset = FindePattern(firstBlock, trueFas, firstBlock.Length - trueFas.Length - 8);
            // установка bitOffset 
            if (firstFrameOffset == -1)
            {
                if (secondFrameOffset == -1)
                    FASlosts++;
                else
                    bitOffset = secondFrameOffset - frameSize;
            }
            else
                bitOffset = firstFrameOffset;

            int trueBitOffset = bitOffset;

            //MFAS
            ProcessMfas(firstBlock, trueBitOffset);

            // считаем новый bitOffset, проверяем нет ли опять двух offset 
            bitOffset = NewBitOffset(firstFrameOffset, secondFrameOffset);

            return new Tuple<int, int>(trueBitOffset, firstFrameOffset);
        }
        public int NewBitOffset(int firstFrameOffset, int secondFrameOffset)
        {
            if (firstFrameOffset != -1)
            {
                frameCount++;
                if (bitOffset == 0)
                    bitOffset = 8;
                else { bitOffset = firstFrameOffset;}
                if (limitFasErrInRow == 1)
                    limitFasErrInRow--;
            }
            else if (secondFrameOffset != -1)
            {
                frameCount++;
                firstFrameOffset = secondFrameOffset - frameSize;
                if (bitOffset == 0)
                    bitOffset = 8;
                else { bitOffset = firstFrameOffset; }
                if (limitFasErrInRow == 1)
                    limitFasErrInRow--;
            }
            else if (firstFrameOffset == -1 && secondFrameOffset == -1)
            {
                limitFasErrInRow++;
                if (limitFasErrInRow == 2)
                {
                    MessageBox.Show($"Отсутствуют 3 FAS подрят. Плохой сигнал");
                    flag = -1;
                }
            }
            return bitOffset;
        }
        public void ProcessMfas(byte[] firstBlock, int bitOffset)
        {
            if (firstBlock.Length - (bitOffset + 48) > 8)
            {
                MFAS = CheckMFAS(firstBlock, bitOffset + 48); //следим за MFAS

                if (isReversed == true)
                    MFAS = ReverseBits(MFAS);

                if (findFirstMFAS == true && lastMFAS + 1 != MFAS)
                {
                    errors++;
                }
                MFASCounter++;

                if ((0 == MFAS && lastMFAS == -1) || (lastMFAS == -1 && lastLastMFAS  == 254)|| MFASCounter == 256) // уменьшаем вероятность ошибиться
                {
                    MFASCounter = 0;
                    errors = 0;
                    lastMFAS = 0; 
                    lastLastMFAS = 255;
                    findFirstMFAS = true;
                }
                else if ((255 == MFAS && lastMFAS == 254) || (lastMFAS == 254 && lastLastMFAS == 253)|| MFASCounter == 255) // уменьшаем вероятность ошибиться
                {
                    if (errors > MFASerrors)
                        MFASlosts++;

                    lastLastMFAS = lastMFAS;
                    lastMFAS = -1;
                }
                else
                {
                    buff = lastMFAS;
                    lastMFAS = MFAS;
                    lastLastMFAS = buff;
                }
               
            }
        }
            
        public Tuple<int, int, byte[]> Info(byte[] firstBlock)
        {
            byte[] fas = { 0xF6, 0xF6, 0xF6, 0x28, 0x28, 0x28 };
            byte[] fasInvert = { 0x6F, 0x6F, 0x6F, 0x14, 0x14, 0x14 };
            byte[] fasByteArray = ConvertFas(fas);//6F  82
            byte[] fasInvertByteArray = ConvertFas(fasInvert);//F6  41

            if (FindePattern(firstBlock, fasByteArray) != -1)
            {
                trueFas = fasByteArray;
                isReversed = false;
            }
            else if (FindePattern(firstBlock, fasInvertByteArray) != -1)
            {
                trueFas = fasInvertByteArray;
                isReversed = true;
            }
            else {MessageBox.Show("не распознан FAS"); }

            firstFrameOffset = FindePattern(firstBlock, trueFas);
            int secondFrameOffset = FindePattern(firstBlock, trueFas, firstFrameOffset + trueFas.Length);
            int thirdFrameOffset = FindePattern(firstBlock, trueFas, secondFrameOffset + trueFas.Length);
            int fourthFrameOffset = FindePattern(firstBlock, trueFas, thirdFrameOffset + trueFas.Length);
            int supposframe1Size = secondFrameOffset - firstFrameOffset;
            int supposframe2Size = thirdFrameOffset - secondFrameOffset;
            int supposframe3Size = fourthFrameOffset - thirdFrameOffset;
            
            if((supposframe1Size == supposframe2Size|| supposframe2Size == supposframe3Size) && (supposframe2Size < 2*supposframe1Size))
                frameSize = supposframe2Size;
            else if (supposframe1Size == supposframe3Size)
                frameSize = supposframe3Size;
            else if (supposframe1Size+supposframe2Size == supposframe3Size)//можно 1 раз неправильно посчитать FAS на 2 кадра (1/130560)
                frameSize = supposframe3Size;
            else if(supposframe3Size+supposframe2Size == supposframe1Size)
                frameSize = supposframe1Size;
            else
                MessageBox.Show($"Не определён размер. Первый кадр: {supposframe1Size}, второй кадр: {supposframe2Size}, третий:{supposframe3Size} ");

            return new Tuple<int, int, byte[]>(frameSize, firstFrameOffset, trueFas);
        }
        public byte[] ConvertFas(byte[] anyFas)
        {
            byte[] result = new byte[48];
            byte fasBit;
            int index = 0;
            byte mask = 0b00000001;
            for (int l = 0; l < 6; l++)
            {
                for (int i = 0; i < 8; i++)
                {
                    fasBit = (byte)(anyFas[l] & (mask));
                    anyFas[l] = (byte)(anyFas[l] >> 1);
                    result[index] = fasBit;
                    index++;
                }
            }
            return result;
        }
        public int FindePattern(byte[] firstBlock, byte[] trueFas, int offset = 0, int end = 0)
        {
            int index = offset;
            int errors = 0;
            if (end == 0)
                end = firstBlock.Length - trueFas.Length;
            while (index < end)
            {
                exitBothLoops = false;
                errors = 0;
                for (int i = 0; i < trueFas.Length; i++)
                {
                    if (firstBlock[index+i] != trueFas[i] && errors > fasErrors)
                    {
                        exitBothLoops = true;
                        break;
                    }
                    else if (firstBlock[index + i] != trueFas[i])
                    {
                        errors++;
                    }
                    else
                    {
                        if (i == 47)
                        {
                            fasCount++;
                            int frameOffset = index;
                            return frameOffset;
                        }
                    }
                    if (exitBothLoops)
                        break;
                }
                index++;
            }
            return -1;
        }
        public byte CheckMFAS(byte[] firstBlock, int MFASStart)
        {
            MFAS = 0x00;
            for (int i = 0; i<8; i++ )
            {
                MFAS|= (byte)(firstBlock[MFASStart + i]<<i);
            }
            return (byte)(MFAS^0xFF);
        }
        public static byte[] ReadBinaryData(FileStream fs, int bytesToRead)
        {
            byte[] data = new byte[bytesToRead];
            fs.Seek(0, SeekOrigin.Current);
            fs.Read(data, 0, bytesToRead);
            var res = new byte[data.Length * 8];
            var stream = new ArrayByteStream(data);
            var bitStream = new BitStream<ArrayByteStream>(stream, 0);

            for (var i = 0; i < res.Length; i++)
            {
                res[i] = bitStream.ReadBit();
            }
            return res;
        }
        public static void WriteBinaryData(byte[] data, int offset, int end, FileStream outputfs)
        {
            for (var i = offset; i < end; i += 8)
            {
                byte rawByte = 0;
                for (byte j = 0; j < 8; j++)
                {
                    if (i + j >= end)
                        break;
                    rawByte |= (byte)(data[i + j] << j);
                }
                outputfs.WriteByte(ReverseBits(rawByte));
            }
        }

        public static byte[] BitReverseTable =
        {
            0x00, 0x80, 0x40, 0xc0, 0x20, 0xa0, 0x60, 0xe0,
    0x10, 0x90, 0x50, 0xd0, 0x30, 0xb0, 0x70, 0xf0,
    0x08, 0x88, 0x48, 0xc8, 0x28, 0xa8, 0x68, 0xe8,
    0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8,
    0x04, 0x84, 0x44, 0xc4, 0x24, 0xa4, 0x64, 0xe4,
    0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4,
    0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac, 0x6c, 0xec,
    0x1c, 0x9c, 0x5c, 0xdc, 0x3c, 0xbc, 0x7c, 0xfc,
    0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2,
    0x12, 0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2,
    0x0a, 0x8a, 0x4a, 0xca, 0x2a, 0xaa, 0x6a, 0xea,
    0x1a, 0x9a, 0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa,
    0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6,
    0x16, 0x96, 0x56, 0xd6, 0x36, 0xb6, 0x76, 0xf6,
    0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee,
    0x1e, 0x9e, 0x5e, 0xde, 0x3e, 0xbe, 0x7e, 0xfe,
    0x01, 0x81, 0x41, 0xc1, 0x21, 0xa1, 0x61, 0xe1,
    0x11, 0x91, 0x51, 0xd1, 0x31, 0xb1, 0x71, 0xf1,
    0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9,
    0x19, 0x99, 0x59, 0xd9, 0x39, 0xb9, 0x79, 0xf9,
    0x05, 0x85, 0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5,
    0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 0xf5,
    0x0d, 0x8d, 0x4d, 0xcd, 0x2d, 0xad, 0x6d, 0xed,
    0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd,
    0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3, 0x63, 0xe3,
    0x13, 0x93, 0x53, 0xd3, 0x33, 0xb3, 0x73, 0xf3,
    0x0b, 0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb,
    0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb,
    0x07, 0x87, 0x47, 0xc7, 0x27, 0xa7, 0x67, 0xe7,
    0x17, 0x97, 0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7,
    0x0f, 0x8f, 0x4f, 0xcf, 0x2f, 0xaf, 0x6f, 0xef,
    0x1f, 0x9f, 0x5f, 0xdf, 0x3f, 0xbf, 0x7f, 0xff

        };
        public static byte ReverseBits(byte b)
        {
            return BitReverseTable[b];
        }
        public void UpdateAlignmentControl(int soFirstFrameOffset)
        {
            string frameInfo = $"Первый FAS: {soFirstFrameOffset / 8} байт {soFirstFrameOffset % 8} бит\n" +
                               $"Первый MFAS: {firstMFAS / 8} байт {firstMFAS % 8} бит\n" +
                               $"Размер кадра {frameSize}\n" +
                               $"Байты {(isReversed ? "развёрнуты" : "не развёрнуты")}\n" +
                               $"Неуверенно распознано кадров {FASlosts}\n" +
                               $"Неуверенно распознано последовательностей MFAS (0-255) {MFASlosts}";

            Manager.UsedAlignmentControl.label5.Invoke(new Action(() => Manager.UsedAlignmentControl.label5.Text = frameInfo));
        }
        public void OnProgressChange(object sender, int e)
        {
            Manager.UsedAlignmentControl.progressBar2.Value = e;
        }

    }
}
