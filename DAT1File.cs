using System.IO;
using DATLib.FO1;

namespace DATLib
{
    internal class DAT1File : DATFile
    {
        private byte[] decompressStream(MemoryStream mem)
        {
            BinaryBigEndian bbr = new BinaryBigEndian(mem);
            bbr.BaseStream.Seek(0, SeekOrigin.Begin);

            var LZSS = new LZSS(bbr, base.UnpackedSize);
            var bytes = LZSS.Decompress();

            bbr.Dispose();
            return bytes;
        }

        internal override byte[] GetData()
        {
            if (Compression) {
                using (MemoryStream st = new MemoryStream(tempBuffer))
                {
                    return decompressStream(st);
                }
            }
            return tempBuffer;
        }

        /*
        public byte[] getBytes(FileStream stream)
        {
            var r = new BinaryBigEndian(stream);
            r.BaseStream.Seek(base.Offset, SeekOrigin.Begin);
            return r.ReadBytes(base.UnpackedSize);
        }

        private byte[] getCompressedBytes(FileStream stream)
        {
            // Create a new stream so that we are thread safe.
            var s = new MemoryStream();

            BinaryBigEndian r;
            stream.Seek(base.Offset, SeekOrigin.Begin);

            // Copy packedSize amount of bytes from the original dat stream to our new memory stream.
            for (int i = 0; i < base.PackedSize; i++)
                s.WriteByte((byte)stream.ReadByte());

            r = new BinaryBigEndian(s);
            r.BaseStream.Seek(0, SeekOrigin.Begin);

            var LZSS = new LZSS((BinaryBigEndian)base.br, base.UnpackedSize);
            var bytes = LZSS.Decompress();

            s.Dispose();
            r.Dispose();

            return bytes;
        }
        */
    }
}
