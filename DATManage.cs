using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DATLib
{
    public struct Info
    {
        public int  Size;
        public int  PackedSize;
        public bool IsPacked;
    }

    public struct FileInfo
    {
        public string name;
        public Info info;
    }

    public static class DATManage
    {
        static private List<DAT> openDat = new List<DAT>();
        private static string lastCheckFolder = string.Empty;

        private static void UnsetCheckFolder()
        {
            lastCheckFolder = string.Empty;
        }

        public static bool OpenDatFile(string datFile, out string eMessage)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    eMessage = string.Empty;
                    return true; // DAT file is already open
                }
            }
            DatReaderError err;
            DAT datData = DATReader.ReadDat(datFile, out err);
            if (datData != null) {
                datData.FileList = DATReader.FindFiles(datData);
                openDat.Add(datData);
            }
            eMessage = err.Message;
            return (err.Error == DatError.Success);
        }

        private static void SaveFile(string filePath, DATFile datfile)
        {
            byte[] data = datfile.GetFileData();
            if (data == null) {
                return;
            }

            if (lastCheckFolder != datfile.Path) {
                string path = Path.GetFullPath(Path.GetDirectoryName(filePath));
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                lastCheckFolder = datfile.Path;
            }
            File.WriteAllBytes(filePath, data);
        }

        public static bool ExtractAllFiles(string unpackPath, string datFile)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    UnsetCheckFolder();
                    foreach (DATFile datfile in dat.FileList)
                    {
                        int n = datfile.FilePath.LastIndexOf('\\') + 1;
                        string filePath = datfile.FilePath.Remove(n) + datfile.FileName;

                        OnExtracted(filePath);

                        SaveFile(unpackPath + filePath, datfile);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool UnpackFile(string unpackPath, string file, string datFile)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    UnsetCheckFolder();
                    DATFile datfile = DATReader.GetFile(dat, file);
                    if (datfile == null) return false;

                    SaveFile(unpackPath + file, datfile);
                    return true;
                }
            }
            return false;
        }

        public static bool UnpackFileList(string unpackPath, string[] files, string datFile)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    UnsetCheckFolder();
                    foreach (var file in files)
                    {
                        DATFile datfile = DATReader.GetFile(dat, file);
                        if (datfile == null) return false;

                        OnExtracted(file);

                        SaveFile(unpackPath + file, datfile);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool UnpackFileList(string unpackPath, string[] files, string datFile, string cutoffPath)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    UnsetCheckFolder();
                    int len = cutoffPath.Length;
                    if (len > 0) len++;
                    foreach (var file in files)
                    {
                        DATFile datfile = DATReader.GetFile(dat, file);
                        if (datfile == null) return false;

                        OnExtracted(file);

                        string unpackedPathFile = unpackPath + file.Substring(len);
                        SaveFile(unpackedPathFile, datfile);
                    }
                    return true;
                }
            }
            return false;
        }

        public static Dictionary<String, String> GetFileList(string datFile)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    Dictionary<String, String> fileList = new Dictionary<String, String>();
                    foreach (var file in dat.FileList)
                    {
                        fileList.Add(file.FilePath, file.FileName);
                    }
                    return fileList;
                }
            }
            return null;
        }

        public static Dictionary<String, FileInfo> GetFiles(string datFile)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    Dictionary<String, FileInfo> fileList = new Dictionary<String, FileInfo>();
                    foreach (var file in dat.FileList)
                    {
                        FileInfo f = new FileInfo();
                        f.name            = file.FileName;
                        f.info.Size       = file.UnpackedSize;
                        f.info.IsPacked   = file.Compression;
                        f.info.PackedSize = file.PackedSize;
                        fileList.Add(file.FilePath, f);
                    }
                    return fileList;
                }
            }
            return null;
        }

        public static long GetFileTotal(string datFile)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) return dat.FilesTotal;
            }
            return 0;
        }

        public static Info GetFileInfo(string datFile, string file)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    DATFile _file = dat.GetFileByName(file);
                    return new Info() {
                        Size       = _file.UnpackedSize,
                        PackedSize = _file.PackedSize,
                        IsPacked   = _file.Compression
                    };
                }
            }
            return new Info();
        }

        public static void CloseDatFile(string datFile)
        {
            foreach (DAT dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    dat.Close();
                    openDat.Remove(dat);
                    break;
                }
            }
        }

        public static void CloseAllDatFiles()
        {
            foreach (DAT dat in openDat) dat.Close();
            openDat.Clear();
        }

        #region Event
        public static event ExtractEvent ExtractUpdate;

        public static void OnExtracted(string file)
        {
            if (ExtractUpdate != null) {
                ExtractUpdate(new ExtractEventArgs(file));
            }
        }
        #endregion
    }
}
