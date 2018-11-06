using ConvNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace ConvNetTester
{
    public class Stuff
    {
        public static int ReadInt(byte[] bytes, int start)
        {
            int res = 0;
            for (int i = 0; i < 4; i++)
            {
                res ^= (bytes[start + (3 - i)] << (i * 8));
            }
            return res;

        }
        public static CifarVolumePrepared ReadCifarImage(byte[] bb, long start)
        {
            var w = 32;
            var h = 32;
            Bitmap bmp = new Bitmap(32, 32);
            var label = bb[start];
            CifarVolumePrepared ret = new CifarVolumePrepared();
            ret.label = label;
            start++;

            NativeBitmap b = new NativeBitmap(bmp);


            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    var _ind = (i * w + j);
                    b.SetPixel(j, i, new byte[] {  bb[start + _ind], bb[start + 1024 + _ind], bb[start + 2048 + _ind] , 0xff });
                }
            }

            ret.Bmp = b.GetBitmap();

                        
            return ret;
        }

        public static MnistItem ReadImage(byte[] bb, int start, int w, int h)
        {
            byte[,] data = new byte[w, h];
            int cntr = 0;
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    data[i, j] = bb[start + cntr];
                    cntr++;
                }
            }
            return new MnistItem() { Data = data };
        }
        public static CifarVolumePrepared[] LoadCifarImages(string imgPath, Action<float> progressReport, bool withBitmap = false)
        {
            List<CifarVolumePrepared> bmps = new List<CifarVolumePrepared>();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var bytes = File.ReadAllBytes(imgPath);
            int imagesCnt = 10000;//10000
            long indexer = 0;
            for (int i = 0; i < imagesCnt; i++)
            {
                bmps.Add(ReadCifarImage(bytes, indexer));
                indexer += 3073;
                if (progressReport != null)
                {
                    progressReport(i / (float)imagesCnt);
                }
            }

            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            sw.Stop(); sw.Reset();
            sw.Start();
            /*if (withBitmap)
            {
                foreach (var mnistItem in bmps)
                {
                    mnistItem.GetBitmap();
                }
            }*/
            sw.Stop();
            var ms2 = sw.ElapsedMilliseconds;
            sw.Stop();



            return bmps.ToArray();

        }
        public static MnistItem[] LoadImages(string imgPath, string labelsPath, bool withBitmap = false)
        {
            List<MnistItem> bmps = new List<MnistItem>();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var bytes = File.ReadAllBytes(imgPath);
            int imagesCnt = ReadInt(bytes, 4);
            int indexer = 8;
            var w = ReadInt(bytes, indexer);
            indexer += 4;
            var h = ReadInt(bytes, indexer);
            indexer += 4;
            for (int i = 0; i < imagesCnt; i++)
            {
                bmps.Add(ReadImage(bytes, indexer, w, h));
                indexer += w * h;

            }
            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            sw.Stop(); sw.Reset();
            sw.Start();
            if (withBitmap)
            {
                foreach (var mnistItem in bmps)
                {
                    mnistItem.GetBitmap();
                }
            }
            sw.Stop();
            var ms2 = sw.ElapsedMilliseconds;
            sw.Stop(); sw.Reset();
            sw.Start();

            #region load lables
            bytes = File.ReadAllBytes(labelsPath);
            imagesCnt = ReadInt(bytes, 4);
            indexer = 8;
            for (int i = 0; i < imagesCnt; i++)
            {
                bmps[i].Label = bytes[indexer];
                indexer++;
            }
            sw.Stop();
            var ms3 = sw.ElapsedMilliseconds;


            #endregion

            return bmps.ToArray();

        }
    }
}
