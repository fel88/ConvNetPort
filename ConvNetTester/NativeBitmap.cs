using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ConvNetTester
{
    public class NativeBitmap
    {
        public static Bitmap BytesToBmp(byte[] bmpBytes, Size imageSize, PixelFormat format)
        {
            Bitmap bmp = new Bitmap(imageSize.Width, imageSize.Height);

            BitmapData bData = bmp.LockBits(new Rectangle(new Point(), bmp.Size),
                ImageLockMode.WriteOnly,
                //PixelFormat.Format24bppRgb);
                format);

            // Copy the bytes to the bitmap object
            Marshal.Copy(bmpBytes, 0, bData.Scan0, bmpBytes.Length);
            bmp.UnlockBits(bData);
            return bmp;
        }

        public static byte[] BmpToBytes_Unsafe(Bitmap bmp)
        {
            BitmapData bData = bmp.LockBits(new Rectangle(new Point(), bmp.Size),
                ImageLockMode.ReadOnly,
                bmp.PixelFormat);
            // number of bytes in the bitmap
            int byteCount = bData.Stride * bmp.Height;
            byte[] bmpBytes = new byte[byteCount];

            // Copy the locked bytes from memory
            Marshal.Copy(bData.Scan0, bmpBytes, 0, byteCount);

            // don't forget to unlock the bitmap!!
            bmp.UnlockBits(bData);

            return bmpBytes;
        }
        public byte[] Bytes;
        private int stride;
        public int bytesPerPixel;
        public int Width;
        public int Height;
        public PixelFormat Format;
        public Size Size;
        public NativeBitmap(Bitmap bmp)
        {
            Size = bmp.Size;
            Format = bmp.PixelFormat;
            Width = bmp.Width;
            Height = bmp.Height;
            Bytes = BmpToBytes_Unsafe(bmp);

            int bitsPerPixel = ((int)bmp.PixelFormat & 0xff00) >> 8;
            bytesPerPixel = (bitsPerPixel + 7) / 8;
            stride = 4 * ((bmp.Width * bytesPerPixel + 3) / 4);
        }

        public byte GetPixel(int i, int j)
        {
            int index = j * stride + i * bytesPerPixel;
            var val = (Bytes[index]);
            return val;

        }
        public byte[] GetPixel3(int i, int j)
        {
            int index = j * stride + i * bytesPerPixel;
            byte[] bb = new byte[3];
            for (int k = 0; k < 3; k++)
            {
                bb[k] = (Bytes[index + k]);
            }
            return bb;

        }
        public void SetPixel(int i, int j, byte val)
        {
            int index = j * stride + i * bytesPerPixel;
            Bytes[index] = val;
        }
        public void SetPixel3(int i, int j, byte val)
        {
            int index = j * stride + i * bytesPerPixel;
            for (int k = 0; k < 3; k++)
            {
                Bytes[index + k] = val;

            }
            Bytes[index + 3] = 255;

        }
        public void SetPixel(int i, int j, byte[] val)
        {
            int index = j * stride + i * bytesPerPixel;
            for (int k = 0; k < bytesPerPixel; k++)
            {
                Bytes[index + k] = val[k];

            }
        }


        public Bitmap GetBitmap()
        {
            return BytesToBmp(Bytes, Size, Format);
        }
    }
}