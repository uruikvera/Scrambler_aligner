using Sewer56.BitStream.ByteStreams;
using Sewer56.BitStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Scrambler_align3.Classes
{
    public class Scrambler
    {
        public ForScrambler forScrambler;
        public void Scram(string fileName, string outputFilePath, IProgress<int> progress, string polynomial = "10001000000001011", int frameSize = 130560)
        {
            forScrambler = new ForScrambler();
            ushort polynomialUshort = forScrambler.ConvertToUshort(polynomial); // 0xA009
            byte[] LFSRArray = forScrambler.GenerateSequence(polynomialUshort);
            int lengthLFSR = LFSRArray.Length;
            int lengthLFSRdinamic = LFSRArray.Length;

            int fasRem = 48;
            int count = 0;
            int index = 48;

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (FileStream outputfs = File.Open(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                long lengthOfFile = new FileInfo(fileName).Length;
                int bytesToRead = frameSize;
                byte[] bufferForBit = new byte[bytesToRead];
                int backToFAS = frameSize - 48;
                int units = (int)((lengthOfFile * 8) / frameSize);
                int remains =(int)((lengthOfFile * 8) % frameSize);

                while (count < units)
                {
                    bufferForBit = ReadBinaryData(fs, bytesToRead / 8);
                    while (index < frameSize - 48)
                    {
                        if (backToFAS < lengthLFSR && backToFAS > 0)
                        {
                            lengthLFSRdinamic = backToFAS;
                        }
                        backToFAS -= lengthLFSRdinamic;
                        for (int lfsr = 0; lfsr < lengthLFSRdinamic; lfsr++) 
                        {
                            bufferForBit[index] = (byte)(LFSRArray[lfsr] ^ bufferForBit[index]);
                            index++;
                        }
                    }
                    WriteBinaryData(bufferForBit, 0, frameSize, outputfs);
                    progress.Report(count * 100 / units);
                    Application.DoEvents();

                    backToFAS = Convert.ToInt32(frameSize) - 48;
                    lengthLFSRdinamic = lengthLFSR;
                    count++;
                    index = 48;
                }
                progress.Report(100);
            }

        }
        public void OnProgressChange(object sender, int e)
        {
            Manager.UsedScramblerControl.progressBar1.Value = e;
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
            ForScrambler scrambler = new ForScrambler();
            for (var i = offset; i < end; i += 8)
            {
                byte rawByte = 0;
                for (byte j = 0; j < 8; j++)
                {
                    if (i + j >= end)
                        break;
                    rawByte |= (byte)(data[i + j] << j);
                }
                outputfs.WriteByte(scrambler.ReverseBits(rawByte));
            }
        }
        
    }

    public class ForScrambler
    {
        static ushort register = 0xFFFF; // Регистр сдвига (16 бит)

        public byte[] GenerateSequence(ushort polynomialUshort)
        {
            int lfsrSize = 65535;
            byte[] generatedBits = new byte[lfsrSize];
            for (int i = 0; i < lfsrSize; i++)
            {
                generatedBits[i] = GenerateNextBit(polynomialUshort);
            }
            return generatedBits;
        }
        
        public byte GenerateNextBit(ushort polynomialUshort)
        {
            bool newBit = false;
            byte oldBit = Convert.ToByte(register & 1);
            for (int i = 0; i <= 15; i++)
            {
                if (((polynomialUshort >> i) & 1) == 1)//смотрим где единицы 
                {
                    newBit ^= ((register >> i) & 1) == 1;
                }
            }
            register >>= 1;
            register |= (ushort)(Convert.ToUInt16(newBit) << 15); // установка нового бита в старший разряд
            return oldBit;
        }
        public ushort ConvertToUshort(string polynomial)
        {
            string reversedPolynomial = new string(polynomial.Reverse().Skip(1).ToArray());
            ushort polynomialUshort = Convert.ToUInt16(reversedPolynomial, 2);
            return polynomialUshort;
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

        public byte ReverseBits(byte b)
        {
            return BitReverseTable[b];
        }
    }
}
