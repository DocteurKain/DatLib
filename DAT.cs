using System;
using System.Collections.Generic;
using System.IO;
using DATLib.FO1;

namespace DATLib
{
    // This is a helper class for handling multiple DAT files.
    // https://fallout.fandom.com/wiki/DAT_file
    public class DAT
    {
        internal BinaryReader br { get; set; }

        internal String DatFileName { get; set; }
        internal List<DATFile> FileList { get; set; }

        internal long FileSizeFromDat { get; set; }
        internal long TreeSize { get; set; }
        internal long FilesTotal { get; set; }

        // only for Fallout 1 DAT
        internal long DirCount { get; set; }

        public bool IsFallout2Type
        {
            get { return DirCount == 0; }
        }

        internal void Close()
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
                if ((pattern == string.Empty) || (file.FilePath.Contains(pattern) && ((CountChar(file.FilePath, '\\') - 1 == CountChar(pattern, '\\')))))
                    Files.Add(file);
            }
            return Files;
        }

        public void AddFile(string filename, string virtualfilename)
        {
            DATFile file = new DATFile();
            file.FilePath = virtualfilename;
            file.FileNameSize = System.Text.ASCIIEncoding.ASCII.GetByteCount(file.FilePath);
            file.dataBuffer = File.ReadAllBytes(filename);
            file.UnpackedSize = file.dataBuffer.Length;
            file.PackedSize = file.dataBuffer.Length;
            file.Compression = false;
            FileList.Add(file);
            FilesTotal++;
        }

        public DATFile GetFileByName(string fileName)
        {
            foreach (DATFile file in FileList) {
                if (file.FilePath == fileName) return file;
            }
            return null;
        }
    }
}
