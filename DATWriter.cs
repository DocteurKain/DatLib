using System.Collections.Generic;
using System.IO;

namespace DATLib
{
    #if SaveBuild
    internal static class DATWriter
    {
        #region Fallout 1 dat format
        
        private static SortedDictionary<string, List<DATFile>> BuildDict(DAT dat)
        {
            SortedDictionary<string, List<DATFile>> data = new SortedDictionary<string, List<DATFile>>();
            
            foreach (var file in dat.FileList)
            {
                if (file.IsDeleted) continue;
                
                string dir = file.Path;
                
                if (!data.ContainsKey(dir)) data.Add(dir, new List<DATFile>());
                data[dir].Add(file);
            }
            return data;
        }
        
        private static void UpdateFileData(SortedDictionary<string, List<DATFile>> data, WBinaryBigEndian bw)
        {
            foreach (var files in data.Values)
            {
                bw.BaseStream.Position += 16;

                foreach (var file in files)
                {
                    bw.BaseStream.Position += file.FileName.Length + 1;
                    bw.WriteInt32BE((file.Compression) ? 0x40 : 0x20);
                    bw.WriteInt32BE(file.Offset);
                    bw.WriteInt32BE(file.UnpackedSize);
                    bw.WriteInt32BE(file.PackedSize);
                }
            }
        }

        public static void FO1_BuildDat(DAT dat)
        {
            SortedDictionary<string, List<DATFile>> data = BuildDict(dat);

            WBinaryBigEndian bw = new WBinaryBigEndian(File.Open(dat.DatFileName + ".tmp", FileMode.Create, FileAccess.Write));

            bw.WriteInt32BE(dat.DirCount);

            // Unknown fields
            bw.WriteInt32BE(dat.DirCount); // 10
            bw.Write((long)0); // 8-bytes

            // Write dirs
            foreach (var dir in data.Keys)
            {
                bw.Write((byte)dir.Length);
                bw.Write(dir.ToCharArray());
            }

            int startFileDataAddr = (int)bw.BaseStream.Position;

            // Write files data
            foreach (var files in data.Values)
            {
                bw.WriteInt32BE(files.Count);

                // Unknown fields
                bw.WriteInt32BE(files.Count);
                bw.WriteInt32BE(16); // 16
                bw.WriteInt32BE(0);

                foreach (var file in files)
                {
                    bw.Write((byte)file.FileName.Length);
                    bw.Write(file.FileName.ToCharArray());
                    bw.BaseStream.Position += 16;
                }
            }

            // key offset => value index
            List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>(); 
            for (int i = 0; i < dat.FileList.Count; i++)
            {
                if (!dat.FileList[i].IsDeleted) list.Add(new KeyValuePair<int, int>(dat.FileList[i].Offset, i));
            }
            // сортируем по значению offset
            list.Sort((x, y) => x.Key.CompareTo(y.Key));
            
            bool hasVirtual = false;

            // Copy and write files content from source dat
            foreach (var item in list)
            {
                int i = item.Value;
   
                if (dat.FileList[i].IsVirtual) {
                    hasVirtual = true;
                    continue;
                }

                int offset = (int)bw.BaseStream.Position;
                bw.Write(dat.FileList[i].GetDirectFileData());
                dat.FileList[i].Offset = offset;
            }

            // Write virtual files content to save dat
            if (hasVirtual) {
                for (int i = 0; i < dat.FileList.Count; i++)
                {
                    if (dat.FileList[i].IsVirtual) {
                        dat.FileList[i].Offset = (int)bw.BaseStream.Position;
                        bw.Write(dat.FileList[i].GetCompressedData(), 0, dat.FileList[i].PackedSize);
                    }
                }
            }
            bw.Seek(startFileDataAddr, SeekOrigin.Begin);
            UpdateFileData(data, bw);

            bw.Flush();
            bw.Dispose();
            dat.br.Close();
            
            File.Delete(dat.DatFileName);
            File.Move(dat.DatFileName + ".tmp", dat.DatFileName);

            RBinaryBigEndian br = new RBinaryBigEndian(File.Open(dat.DatFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
            dat.br = br;
            for (int i = 0; i < dat.FileList.Count; i++) dat.FileList[i].br = br;
        }
        
        #endregion

        #region Fallout 2 dat format

        private static void WriteDirTreeSub(DAT dat, BinaryWriter bw)
        {
            int startDirTreeAddr = (int)bw.BaseStream.Position;
            
            // Write DirTree
            for (int i = 0; i < dat.FilesTotal; i++) {
                bw.Write(dat.FileList[i].FileNameSize);
                bw.Write(dat.FileList[i].FilePath.ToCharArray());
                bw.Write(dat.FileList[i].Compression);
                bw.Write(dat.FileList[i].UnpackedSize);
                bw.Write(dat.FileList[i].PackedSize);
                bw.Write(dat.FileList[i].Offset);
            }
            // TreeSize
            dat.TreeSize = (int)bw.BaseStream.Position - startDirTreeAddr + 4;
            bw.Write(dat.TreeSize);
            // DatSize
            int datSize = (int)bw.BaseStream.Position + 4;
            bw.Write(datSize);

            if (dat.FileSizeFromDat > datSize) bw.BaseStream.SetLength(datSize);

            dat.FileSizeFromDat = datSize;
        }
        
        public static void WriteDirTree(DAT dat)
        {
            BinaryWriter bw = new BinaryWriter(dat.br.BaseStream);

            bw.BaseStream.Seek(-(dat.TreeSize + 4), SeekOrigin.End);

            WriteDirTreeSub(dat, bw);
        }

        public static void WriteAppendFilesDat(DAT dat)
        {
            BinaryWriter bw = new BinaryWriter(dat.br.BaseStream);

            bw.BaseStream.Seek(-(dat.TreeSize), SeekOrigin.End); // позиция FilesTotal

            foreach (var file in dat.FileList)
            {
                if (!file.IsVirtual) continue;

                file.Offset = (int)bw.BaseStream.Position;

                bw.Write(file.GetCompressedData(), 0, file.PackedSize);
                file.Compression = file.PackedSize != file.UnpackedSize;
                file.br = dat.br;
            }
            bw.Write((int)dat.FilesTotal);

            WriteDirTreeSub(dat, bw);
        }

        // Create new dat
        public static void FO2_BuildDat(DAT dat)
        {
            BinaryWriter bw = new BinaryWriter(File.Open(dat.DatFileName + ".tmp", FileMode.Create));

            int i = 0;
            while (i < dat.FilesTotal) // Write DataBlock
            {
                dat.FileList[i].Offset = (int)bw.BaseStream.Position;
                bw.Write(dat.FileList[i].GetCompressedData(), 0, dat.FileList[i].PackedSize);
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

            dat.br.Close();
            File.Delete(dat.DatFileName);

            dat.br = new BinaryReader(bw.BaseStream);
        }

        #endregion

        //public static void TruncateDat(DAT dat, long size)
        //{
        //    using (var file = File.Open(dat.DatFileName, FileMode.Open, FileAccess.ReadWrite)) {
        //        file.SetLength(file.Length - size);
        //    }
        //}
    }
    #endif
}
