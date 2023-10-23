using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WinHTTPAPI
{
    public enum FilesystemObjectType
    {
        File = 0,
        Directory,
        Unknown
    }
    public class FilesystemObject
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public long Size { get; set; }
        public FilesystemObjectType ObjectType { get; set; }
        public bool HashCalculated { get; set; }
        public string Sha256 { get; set; }
        public DateTime LastWrite { get; set; }
        public DateTime CreationTime { get; set; }
        public string HumanReadableSize
        {
            get
            {
                return GetHumanReadableSize(Size);
            }
        }
        public bool CompareWith(FilesystemObject o)
        {
            return Sha256 == o.Sha256 && HashCalculated && o.HashCalculated && ObjectType == FilesystemObjectType.File && o.ObjectType == FilesystemObjectType.File;
        }
        public void CopyTo(string path, bool overwrite)
        {
            if (ObjectType == FilesystemObjectType.Unknown)
            {
                throw new FileNotFoundException();
            }
            else if (ObjectType == FilesystemObjectType.Directory)
            {
                CopyDirectory(new DirectoryInfo(FullPath), new DirectoryInfo(path), overwrite);
            }
            else if (ObjectType == FilesystemObjectType.File)
            {
                File.Copy(FullPath, path, overwrite);
            }
        }
        private static string GetHumanReadableSize(long Size)
        {
            if (Size > 0)
            {
                double normalSize = 0;
                if ((normalSize = Size) >= 1024)
                {
                    normalSize /= 1024;
                    if (normalSize >= 1024)
                    {
                        normalSize /= 1024;
                        if (normalSize >= 1024)
                        {
                            normalSize /= 1024;
                            return Math.Round(normalSize, 2) + " GB";
                        }
                        else
                        {
                            return Math.Round(normalSize, 2) + " MB";
                        }
                    }
                    else
                    {
                        return Math.Round(normalSize, 2) + " KB";
                    }
                }
                else
                {
                    return Math.Round(normalSize, 0) + " B";
                }
            }
            else
            {
                return "0 B";
            }
        }
        public void CopyTo(string path)
        {
            CopyTo(path, false);
        }
        public void MoveTo(string path)
        {
            if (ObjectType == FilesystemObjectType.Unknown)
            {
                throw new FileNotFoundException();
            }
            else if (ObjectType == FilesystemObjectType.File)
            {
                File.Move(FullPath, path);
                FileInfo movedFile = new FileInfo(path);
                FullPath = movedFile.FullName;
                Name = movedFile.Name;
                LastWrite = movedFile.LastWriteTime;
                CreationTime = movedFile.CreationTime;
            }
            else if (ObjectType == FilesystemObjectType.Directory)
            {
                Directory.Move(FullPath, path);
                DirectoryInfo movedDirectory = new DirectoryInfo(path);
                FullPath = movedDirectory.FullName;
                Name = movedDirectory.Name;
                LastWrite = movedDirectory.LastWriteTime;
                CreationTime = movedDirectory.CreationTime;
            }
        }
        public void Remove()
        {
            RemoveObject(FullPath);
            ObjectType = FilesystemObjectType.Unknown;
        }
        public static FilesystemObject From(FileInfo fi, bool calcHash)
        {
            FilesystemObject result = new FilesystemObject();
            result.FullPath = fi.FullName;
            result.Name = fi.Name;
            if (fi.Exists)
            {
                result.Size = fi.Length;
                result.ObjectType = FilesystemObjectType.File;
                result.LastWrite = fi.LastWriteTime;
                result.CreationTime = fi.CreationTime;
                if (calcHash)
                {
                    try
                    {
                        result.Sha256 = FileSha256(fi);
                        result.HashCalculated = true;
                    }
                    catch
                    {
                        result.HashCalculated = false;
                    }
                }
            }
            else
            {
                result.ObjectType = FilesystemObjectType.Unknown;
            }
            return result;
        }

        public static FilesystemObject From(FileInfo fi)
        {
            return From(fi, false);
        }

        public static FilesystemObject From(string path, bool withSize = true)
        {
            if (Directory.Exists(path))
            {
                return From(new DirectoryInfo(path), withSize);
            }
            else
            {
                return From(new FileInfo(path));
            }
        }
        public static FilesystemObject From(DirectoryInfo di, bool withSize = true)
        {
            FilesystemObject result = new FilesystemObject();
            result.HashCalculated = false;
            result.FullPath = di.FullName;
            result.Name = di.Name;
            if (di.Exists)
            {
                result.ObjectType = FilesystemObjectType.Directory;
                result.LastWrite = di.LastWriteTime;
                result.CreationTime = di.CreationTime;
                try
                {
                    result.Size = withSize ? DirectorySize(di) : 0;
                }
                catch
                {
                    result.Size = 0;
                }
            }
            else
            {
                result.ObjectType = FilesystemObjectType.Unknown;
            }
            return result;
        }
        public static string FileSha256(FileInfo fi)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (Stream s = fi.OpenRead())
            {
                byte[] hashBytes = sha256.ComputeHash(s);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; ++i)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        public static long DirectorySize(DirectoryInfo di)
        {
            long result = 0;
            if (di.Exists)
            {
                foreach (FileInfo fi in di.GetFiles())
                {
                    try
                    {
                        result += fi.Length;
                    }
                    catch
                    {
                        continue;
                    }
                }
                foreach (DirectoryInfo ldi in di.GetDirectories())
                {
                    try
                    {
                        result += DirectorySize(ldi);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return result;
        }
        public static void CopyDirectory(DirectoryInfo di, DirectoryInfo destDir, bool overwrite)
        {
            if (!destDir.Exists)
            {
                destDir.Create();
            }
            foreach (FileInfo fi in di.GetFiles())
            {
                fi.CopyTo(Path.Combine(destDir.FullName, fi.Name), overwrite);
            }
            foreach (DirectoryInfo ldi in di.GetDirectories())
            {
                CopyDirectory(ldi, new DirectoryInfo(Path.Combine(destDir.FullName, ldi.Name)), overwrite);
            }
        }
        public static void RemoveObject(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                RemoveDir(new DirectoryInfo(path));
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
        private static void RemoveDir(DirectoryInfo di)
        {
            if (di.Exists)
            {
                foreach (FileInfo fi in di.GetFiles())
                {
                    fi.Delete();
                }
                foreach (DirectoryInfo ldi in di.GetDirectories())
                {
                    RemoveDir(ldi);
                }
                di.Delete();
            }
            else
            {
                throw new DirectoryNotFoundException();
            }
        }
        public FileInfo ToFileInfo()
        {
            return new FileInfo(FullPath);
        }
        public DirectoryInfo ToDirectoryInfo()
        {
            return new DirectoryInfo(FullPath);
        }
    }
}
