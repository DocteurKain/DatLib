using System;
using System.IO;

namespace DATLib
{
    internal class DATFile
    {
        internal BinaryReader br { get; set; }
        internal String Path { get; set; }
        internal String FileName { get; set; }
        internal byte Compression { get; set; }
        internal int UnpackedSize { get; set; }
        internal int PackedSize { get; set; }
        internal int Offset { get; set; }
        internal long FileIndex { get; set; }
        internal long FileNameSize { get; set; }
        internal string ErrorMsg { get; set; }

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
            using (MemoryStream outStream = new MemoryStream())
            using (zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream))
            try
            {
                byte[] buffer = new byte[512];
                int len;
                while ((len = mem.Read(buffer, 0, 512)) > 0)
                {
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
            return data;
        }

        internal byte[] GetCompressedData()
        {
            if (Compression == 0x01)
                return dataBuffer;
            else
            {
                MemoryStream st = new MemoryStream(dataBuffer);
                byte[] compressed = compressStream(st);
                PackedSize = compressed.Length;
                return compressed;
            }
        }

        private byte[] GetData()
        {
            if (Compression == 0x01)
            {
                using (MemoryStream st = new MemoryStream(dataBuffer))
                    return decompressStream(st);
            }
            return dataBuffer;
        }

        // Read whole file into a buffer
        internal byte[] GetFileData()
        {
            if (dataBuffer == null) {
                br.BaseStream.Position = Offset;
                dataBuffer = new Byte[PackedSize];
                br.Read(dataBuffer, 0, PackedSize);
            }
            return GetData();
        }
    }
}
