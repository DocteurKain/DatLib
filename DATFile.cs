﻿using System;
using System.IO;

namespace DATLib
{
    public class DATFile
    {
        protected static byte[] tempBuffer; // temp buffer for extracted file

        internal BinaryReader br;

        internal String FilePath  { get; set; } // path and name in lower case
        internal String FileName  { get; set; } // file name with case letters
        internal String Path      { get; set; } // only path to file

        internal bool Compression  { get; set; }
        internal int  UnpackedSize { get; set; }
        internal int  PackedSize   { get; set; }
        internal int  Offset       { get; set; }
        internal int  FileNameSize { get; set; }

        internal string ErrorMsg  { get; set; }

        #if SaveBuild

        internal String RealFile  { get; set; } // path to file on disc

        internal bool IsVirtual { get { return PackedSize == -1; } }   // True - файл расположен вне DAT
        internal bool IsDeleted { get;  set; }                         // True - файл будет удален из DAT при сохранении

        private byte[] compressStream(FileStream file)
        {
            MemoryStream outStream = new MemoryStream();
            zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream, zlib.zlibConst.Z_BEST_COMPRESSION);
            try
            {
                byte[] buffer = new byte[512];
                int len;
                while ((len = file.Read(buffer, 0, 512)) > 0)
                {
                    outZStream.Write(buffer, 0, len);
                }
                outZStream.finish();
                tempBuffer = outStream.ToArray();
            }
            finally
            {
                outZStream.Close();
                outStream.Close();
            }
            return tempBuffer;
        }

        internal virtual byte[] GetCompressedData()
        {
            if (RealFile == null) return null;

            using (FileStream file = new FileStream(RealFile, FileMode.Open, FileAccess.Read)) {
                byte[] compressed = compressStream(file);
                PackedSize = compressed.Length;
                RealFile = null;
                return compressed;
            }
        }

        #endif

        private byte[] decompressStream()
        {
            byte[] data;
            using (MemoryStream outStream = new MemoryStream())
            {
                using (zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream)) {
                    byte[] buffer = new byte[br.BaseStream.Length];
                    int len;
                    try
                    {
                        while ((len = br.Read(buffer, 0, (int)br.BaseStream.Length)) > 0) {
                            outZStream.Write(buffer, 0, len);
                        }
                        outZStream.Flush();
                        data = outStream.ToArray();
                    }
                    catch(zlib.ZStreamException ex)
                    {
                        ErrorMsg = ex.Message;
                        return null;
                    }
                    finally
                    {
                        outZStream.finish();
                    }
                }
            }
            return data;
        }

        protected virtual byte[] DecompressData()
        {
            byte[] data;
            using (MemoryStream outStream = new MemoryStream())
            {
                using (zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream))
                {
                    try
                    {
                        outZStream.Write(tempBuffer, 0, tempBuffer.Length);
                        outZStream.Flush();
                        data = outStream.ToArray();
                    }
                    catch(zlib.ZStreamException ex)
                    {
                        ErrorMsg = ex.Message;
                        return null;
                    }
                    finally
                    {
                        outZStream.finish();
                    }
                }
            }
            return data;
        }

        // Read whole file into a temp buffer
        internal byte[] GetFileData()
        {
            if (br == null) return null;

            br.BaseStream.Seek(Offset, SeekOrigin.Begin);
            int size = (Compression) ? PackedSize : UnpackedSize;

            if (tempBuffer == null || size != tempBuffer.Length) tempBuffer = new byte[size];

            br.Read(tempBuffer, 0, size);
            return (Compression) ? DecompressData() : tempBuffer;;
        }

        // Read file content from dat
        internal byte[] GetDirectFileData()
        {
            if (br == null) return null;

            br.BaseStream.Seek(Offset, SeekOrigin.Begin);
            return br.ReadBytes((Compression) ? PackedSize : UnpackedSize);
        }
    }
}
