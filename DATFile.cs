using System;
using System.IO;

namespace DATLib
{
    public class DATFile
    {
        protected static byte[] tempBuffer { get; set; } // temp buffer for extracted file

        internal BinaryReader br  { get; set; }
        internal String FilePath  { get; set; } // path and name in lower case
        internal String FileName  { get; set; } // file name with case letters
        internal String Path      { get; set; } // only path to file

        internal bool Compression { get; set; }
        internal int UnpackedSize { get; set; }
        internal int PackedSize   { get; set; }
        internal int Offset       { get; set; }
        internal int FileNameSize { get; set; }

        //internal long FileIndex { get; set; } // index of file in DAT
        internal string ErrorMsg  { get; set; }

        internal byte[] dataBuffer { get; set; } // Whole file

        private byte[] compressStream(MemoryStream mem)
        {
            MemoryStream outStream = new MemoryStream();
            zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream, zlib.zlibConst.Z_BEST_COMPRESSION);
            byte[] data;
            try
            {
                byte[] buffer = new byte[512];
                int len;
                while ((len = mem.Read(buffer, 0, 512)) > 0)
                {
                    outZStream.Write(buffer, 0, len);
                }
                outZStream.finish();
                data = outStream.ToArray();
            }
            finally
            {
                outZStream.Close();
                outStream.Close();
            }
            return data;
        }

        private byte[] decompressStream(MemoryStream mem)
        {
            byte[] data;
            using (MemoryStream outStream = new MemoryStream()) {
                using (zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream)) {
                    byte[] buffer = new byte[mem.Length];
                    int len;
                    try
                    {
                        while ((len = mem.Read(buffer, 0, (int)mem.Length)) > 0) {
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

        private byte[] decompressData()
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

        internal byte[] GetCompressedData()
        {
            if (Compression)
                return dataBuffer;
            else
            {
                using (MemoryStream st = new MemoryStream(dataBuffer)) {
                    byte[] compressed = compressStream(st);
                    PackedSize = compressed.Length;
                    return compressed;
                }
            }
        }

        private byte[] GetStreamData()
        {
            if (Compression) {
                using (MemoryStream st = new MemoryStream(dataBuffer))
                {
                    return decompressStream(st);
                }
            }
            return dataBuffer;
        }

        internal virtual byte[] GetData()
        {
            return (Compression) ? decompressData() : tempBuffer;
        }

        // Read whole file into a buffer
        internal byte[] GetFileData()
        {
            //if (dataBuffer == null) {
                br.BaseStream.Seek(Offset, SeekOrigin.Begin);
                int size = (Compression) ? PackedSize : UnpackedSize;
                tempBuffer = new Byte[size];
                br.Read(tempBuffer, 0, size);
            //}
            return GetData();
        }
    }
}
