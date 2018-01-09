using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MarkdownMonsterImgurUploaderAddin
{
    internal class ClipboardImageHelper
    {
        public static BitmapFrame ImageFromClipboardDib(MemoryStream dibStream)
        {
            var dibBuffer = new byte[dibStream.Length];
            dibStream.Read(dibBuffer, 0, dibBuffer.Length);

            var infoHeader = FromByteArray<BITMAPINFOHEADER>(dibBuffer);

            var fileHeaderSize = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
            var infoHeaderSize = infoHeader.biSize;
            var fileSize = fileHeaderSize + infoHeader.biSize + infoHeader.biSizeImage;

            var fileHeader = new BITMAPFILEHEADER();
            fileHeader.bfType = BITMAPFILEHEADER.BM;
            fileHeader.bfSize = fileSize;
            fileHeader.bfReserved1 = 0;
            fileHeader.bfReserved2 = 0;
            fileHeader.bfOffBits = fileHeaderSize + infoHeaderSize + infoHeader.biClrUsed * 4;

            var fileHeaderBytes = ToByteArray(fileHeader);

            var msBitmap = new MemoryStream();
            msBitmap.Write(fileHeaderBytes, 0, fileHeaderSize);
            msBitmap.Write(dibBuffer, 0, dibBuffer.Length);
            msBitmap.Seek(0, SeekOrigin.Begin);

            return BitmapFrame.Create(msBitmap);
        }

        private static T FromByteArray<T>(byte[] bytes)
            where T : struct
        {
            var ptr = IntPtr.Zero;
            try
            {
                var size = Marshal.SizeOf(typeof(T));
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, ptr, size);
                var obj = Marshal.PtrToStructure(ptr, typeof(T));
                return (T)obj;
            }
            finally
            {
                if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr);
            }
        }

        private static byte[] ToByteArray<T>(T obj)
            where T : struct
        {
            var ptr = IntPtr.Zero;
            try
            {
                var size = Marshal.SizeOf(typeof(T));
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, true);
                var bytes = new byte[size];
                Marshal.Copy(ptr, bytes, 0, size);
                return bytes;
            }
            finally
            {
                if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct BITMAPFILEHEADER
        {
            public static readonly short BM = 0x4d42; // BM

            public short bfType;

            public int bfSize;

            public short bfReserved1;

            public short bfReserved2;

            public int bfOffBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public readonly int biSize;

            public readonly int biWidth;

            public readonly int biHeight;

            public readonly short biPlanes;

            public readonly short biBitCount;

            public readonly int biCompression;

            public readonly int biSizeImage;

            public readonly int biXPelsPerMeter;

            public readonly int biYPelsPerMeter;

            public readonly int biClrUsed;

            public readonly int biClrImportant;
        }
    }
}