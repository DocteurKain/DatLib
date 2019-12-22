using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DATLib
{
    internal enum DatError
    {
        InvalidDAT,
        IOError,
        Success,
        WrongSize
    };

    internal class DatReaderError
    {
        internal DatReaderError(DatError error, string Message)
        {
            this.Error = error;
            this.Message = Message;
        }
        internal DatError Error { get; set; }
        internal string Message { get; set; }
    }

    internal static class DATReader
    {
        // Based on code by Dims
        public static DAT ReadDat(string filename, out DatReaderError error)
        {
            if (String.IsNullOrEmpty(filename))
            {
                error = new DatReaderError(DatError.IOError, "Invalid DAT filename.");
                return null;
            }

            DAT dat = new DAT();
            dat.DatFileName = filename;
            BinaryReader br = null;
            try
            {
                br = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
            }
            catch (IOException io)
            {
                error = new DatReaderError(DatError.IOError, io.Message);
                return null;
            }
            dat.br = br;
            br.BaseStream.Seek(-8, SeekOrigin.End);
            dat.TreeSize = br.ReadInt32();
            dat.FileSizeFromDat = br.ReadInt32();

            // Check Dat version
            if (br.BaseStream.Length != dat.FileSizeFromDat)
            {
                br.BaseStream.Seek(0, SeekOrigin.Begin);
                dat.DirCount = ToLittleEndian(br.ReadInt32());
                Int64 datMarker = br.ReadInt64(); // unknown1, unknown2
                if (datMarker == 0x0A000000 || datMarker == 0x5E000000) // it's Fallout 1 dat
                {
                    if (dat.DirCount != 0x01 && dat.DirCount != 0x33)
                    {
                        error = new DatReaderError(DatError.InvalidDAT, "Fallout 1 DATs not supported.");
                        return null;
                    }
                    dat.TreeSize = 0;
                    dat.FileSizeFromDat = 0;
                    br.BaseStream.Position += 4; // unknown3

                    error = new DatReaderError(DatError.Success, string.Empty);
                    return dat;
                }
                error = new DatReaderError(DatError.WrongSize, "Size is incorrect.");
                return null;
            }

            br.BaseStream.Seek(-(dat.TreeSize + 8), SeekOrigin.End);
            dat.FilesTotal = br.ReadInt32();

            // Read DirTree data
            //byte[] buff = new byte[dat.TreeSize];
            //br.Read(buff, 0, (int)(dat.TreeSize - 4));

            error = new DatReaderError(DatError.Success, string.Empty);
            return dat;
        }

        public static void WriteDat(DAT dat, string filename)
        {
            dat.br.Close();
            BinaryWriter bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            int i = 0;
            while (i < dat.FilesTotal) // Write DataBlock
            {
                dat.FileList[i].Offset = (int)bw.BaseStream.Position;
                bw.Write(dat.FileList[i].dataBuffer, 0, dat.FileList[i].dataBuffer.Length);
                i++;
            }
            bw.Write((int)dat.FilesTotal);
            i = 0;
            int treeSize = (int)bw.BaseStream.Position;
            while (i < dat.FilesTotal) // Write DirTree
            {
                bw.Write((int)dat.FileList[i].FileNameSize);
                bw.Write(dat.FileList[i].FilePath.ToCharArray(0, dat.FileList[i].FilePath.Length));
                bw.Write(dat.FileList[i].Compression);
                bw.Write(dat.FileList[i].UnpackedSize);
                bw.Write(dat.FileList[i].PackedSize);
                bw.Write(dat.FileList[i].Offset);
                i++;
            }
            bw.Write((int)(bw.BaseStream.Position - treeSize) + 4);
            bw.Write((int)bw.BaseStream.Position + 4);
        }

        internal static List<DATFile> FindFiles(DAT dat)
        {
            List<DATFile> DatFiles = new List<DATFile>();
            BinaryReader br = dat.br;

            if (dat.IsFallout2Type) {
                uint FileIndex = 0;
                br.BaseStream.Seek(-(dat.TreeSize + 4), SeekOrigin.End);
                while (FileIndex < dat.FilesTotal) {
                    DATFile file = new DATFile();
                    file.br = br;

                    file.FileNameSize = br.ReadInt32();
                    char[] namebuf = new Char[file.FileNameSize];
                    br.Read(namebuf, 0, (int)file.FileNameSize);
                    string pathFile = new String(namebuf, 0, namebuf.Length);
                    file.FileName = Path.GetFileName(pathFile);
                    file.FilePath = pathFile.ToLower();
                    file.Path = Path.GetDirectoryName(pathFile);

                    file.Compression = (br.ReadByte() == 0x1);
                    file.UnpackedSize = br.ReadInt32();
                    file.PackedSize = br.ReadInt32();
                    file.Offset = br.ReadInt32();

                    if (!file.Compression && (file.UnpackedSize != file.PackedSize)) file.Compression = true;

                    DatFiles.Add(file);
                    FileIndex++;
                }
            } else { // Implement FO1 dat support: https://falloutmods.fandom.com/wiki/DAT_file_format
                List<string> directories = new List<string>();
                for (var i = 0; i < dat.DirCount; i++)
                {
                    directories.Add(ReadString(br));
                }

                br = new BinaryBigEndian(br.BaseStream);
                for (var i = 0; i < dat.DirCount; i++)
                {
                    var fileCount = br.ReadInt32();
                    br.BaseStream.Position += 12; // unknown4, unknown5, unknown6
                    for (var n = 0; n < fileCount; n++)
                    {
                        DAT1File file = new DAT1File();
                        file.br = br;
                        file.FileName = ReadString(br);
                        file.Compression = (((BinaryBigEndian)br).ReadUInt() == 0x40000000);
                        file.Offset = br.ReadInt32();
                        file.UnpackedSize = br.ReadInt32();
                        file.PackedSize = br.ReadInt32();
                        file.Path = directories[i];
                        file.FilePath = (directories[i] + '\\' + file.FileName).ToLower();

                        DatFiles.Add(file);
                    }
                }
            }
            return DatFiles;
        }

        internal static DATFile GetFile(DAT dat, string filename)
        {
            filename = filename.ToLower();

            foreach (DATFile file in dat.FileList)
            {
                if (file.FilePath == filename) return file;
            }
            return null;
        }

        private static Int32 ToLittleEndian(Int32 value)
        {
            byte[] temp = BitConverter.GetBytes(value);
            Array.Reverse(temp);
            return BitConverter.ToInt32(temp, 0);
        }

        private static string ReadString(BinaryReader br)
        {
            var len = br.ReadByte();
            return Encoding.ASCII.GetString(br.ReadBytes(len));
        }
    }
}
