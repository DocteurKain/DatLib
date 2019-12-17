using System;
using System.Collections.Generic;
using System.IO;
using DATLib.FO1;

namespace DATLib
{
    // This is a helper class for handling multiple DAT files.
    // https://fallout.fandom.com/wiki/DAT_file
    internal class DAT
    {
        public BinaryReader br { get; set; }

        public String DatFileName { get; set; }
        public List<DATFile> FileList { get; set; }

        public long FileSizeFromDat { get; set; }
        public long TreeSize { get; set; }
        public long FilesTotal { get; set; }

        // only for Fallout 1 DAT
        public long DirCount { get; set; }

        public bool IsFallout2Type
        {
            get { return DirCount == 0; }
        }

        public void Close()
        {
            br.Close();
        }

        private int CountChar(string s, char c)
        {
            int count = 0;
            foreach (char ch in s) {
                if (ch == c) count++;
            }
            return count;
        }

        public List<DATFile> GetFilesByPattern(string pattern)
        {
            List<DATFile> Files = new List<DATFile>();
            foreach (DATFile file in FileList) {
                if ((pattern == string.Empty) || (file.Path.Contains(pattern) && ((CountChar(file.Path, '\\') - 1 == CountChar(pattern, '\\')))))
                    Files.Add(file);
            }
            return Files;
        }

        public void AddFile(string filename, string virtualfilename)
        {
            DATFile file = new DATFile();
            file.Path = virtualfilename;
            file.FileNameSize = System.Text.ASCIIEncoding.ASCII.GetByteCount(file.Path);
            file.dataBuffer = File.ReadAllBytes(filename);
            file.UnpackedSize = file.dataBuffer.Length;
            file.PackedSize = file.dataBuffer.Length;
            file.Compression = false;
            FileList.Add(file);
            FilesTotal++;
        }

        public DATFile GetFileByName(string filename)
        {
            foreach (DATFile file in FileList) {
                if (file.FileName == filename) return file;
            }
            return null;
        }
    }
}
