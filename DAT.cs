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

        public String DatFileName { get; set;}

        internal List<DATFile> FileList { get; set; }

        // only for Fallout 2 DAT
        internal uint FileSizeFromDat { get; set; }
        internal int  TreeSize { get; set; }
        internal int  FilesTotal { get; set; }

        // only for Fallout 1 DAT
        internal int DirCount { get; set; }

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

        public DATFile GetFileByName(string fileName)
        {
            foreach (DATFile file in FileList) {
                if (file.FilePath == fileName) return file;
            }
            return null;
        }

        #if SaveBuild

        public void AddFile(string filename, FileInfo virtualfile)
        {
            DATFile file = new DATFile();
            file.Path = virtualfile.pathTree;
            file.FileName = virtualfile.name;
            file.FilePath = virtualfile.pathTree + virtualfile.name;
            file.FileNameSize = System.Text.ASCIIEncoding.ASCII.GetByteCount(file.FilePath);

            file.RealFile = filename;

            file.UnpackedSize = virtualfile.info.Size;
            file.PackedSize = -1;
            file.Compression = false;
            FileList.Add(file);
            FilesTotal++;
        }

        public bool RemoveFile(List<string> filesList)
        {
            if (IsFallout2Type) FilesTotal -= filesList.Count;

            bool realDeleted = false;

            for (int i = 0; i < FileList.Count; i++)
            {
                if (FileList[i].IsDeleted) continue;

                for (int j = 0; j < filesList.Count; j++)
                {
                    if (FileList[i].FilePath == filesList[j]) {
                        if (FileList[i].IsVirtual) {
                            FileList.RemoveAt(i--);
                        } else {
                            FileList[i].IsDeleted = true;
                            realDeleted = true;
                        }
                        DATManage.OnRemove(filesList[j]);
                        filesList.RemoveAt(j);
                        break;
                    }
                }
                if (filesList.Count == 0) break;
            }
            return realDeleted;
        }
        #endif
    }
}
