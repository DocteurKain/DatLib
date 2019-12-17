using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DATLib
{
    public static class DATManage
    {
        static private List<DAT> openDat = new List<DAT>();

        public static bool OpenDatFile(string datFile, out string eMessage)
        {
            foreach (var dat in openDat)
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

        public static bool UnpackFile(string unpackPath, string file, string datFile)
        {
            foreach (var dat in openDat)
            {
                if (dat.DatFileName == datFile) {
                    DATFile datfile = DATReader.GetFile(dat, file);
                    if (datfile == null) return false;

                    string unpackedPathFile = unpackPath + file;
                    string path = Path.GetFullPath(Path.GetDirectoryName(unpackedPathFile));
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    File.WriteAllBytes(unpackedPathFile, datfile.GetFileData());
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
                    foreach (var file in files)
                    {
                        DATFile datfile = DATReader.GetFile(dat, file);
                        if (datfile == null) return false;

                        string unpackedPathFile = unpackPath + file;
                        string path = Path.GetFullPath(Path.GetDirectoryName(unpackedPathFile));
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        File.WriteAllBytes(unpackedPathFile, datfile.GetFileData());
                    }
                    return true;
                }
            }
            return false;
        }

        public static void CloseAllDatFiles()
        {
            foreach (DAT dat in openDat) dat.Close();
            openDat.Clear();
        }
    }
}
