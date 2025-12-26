using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

    public static class ExtensionUtil
    {
    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Debug.Log("CopyFilesRecursively - CopyDir : " + dirPath + " to " + targetPath);
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            Debug.Log("CopyFilesRecursively - CopyFile : " + newPath + " to " + targetPath);
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
    public static void DirMove(string curdir, string movedir)
    {
        if (Path.GetPathRoot(curdir) == Path.GetPathRoot(movedir))
        {
            if(Directory.Exists(movedir)) Directory.Delete(movedir, true);
            Directory.Move(curdir, movedir);
        }
        else
        {
            if (!Directory.Exists(movedir))
            {
                Directory.CreateDirectory(movedir);
            }
            CopyFilesRecursively(curdir, movedir);
            Directory.Delete(curdir, true);
        }
    }
    public static Sprite TryCreateSprite(byte[] data)
    {
        Texture2D texture = new Texture2D(2, 2);

        texture.LoadImage(data);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        return sprite;
    }
    public static string ImageEncode(string filepath)
    {
        return ImageEncode(File.ReadAllBytes(filepath));
    }
    public static string ImageEncode(byte[] b)
    {
        return Convert.ToBase64String(b);
    }
    public static byte[] ImageDecode(string data)
    {
        return Convert.FromBase64String(data);
    }
    public static string StringDecode(string code)
    {
        return Encoding.UTF8.GetString(ImageDecode(code));
    }
    public static DirectoryInfo DoubleFolderChecking(DirectoryInfo dir)
    {
        var dirlist = new List<DirectoryInfo>(dir.GetDirectories());
        var filelist = new List<FileInfo>(dir.GetFiles());
        if (dirlist.Find(x => x.Name == "Info") != null) return dir;
        if (dirlist.Find(x => x.Name == "Creature") != null) return dir;
        if (dirlist.Find(x => x.Name == "Equipment") != null) return dir;
        if (dirlist.Find(x => x.Name == "Localize") != null) return dir;
        if (filelist.Find(x => x.Name.Contains(".dll")) != null) return dir;
        if (dirlist.Count > 1) return dir;
        if (filelist.Count > 0) return dir;
        Debug.Log("Detected DoubleFolder");
        return DoubleFolderChecking(dirlist[0]);
    }
    public static int CompareVersions(string version1, string version2)
    {
        UnityEngine.Debug.Log("version 1 = " + version1);
        UnityEngine.Debug.Log("version 2 = " + version2);
        string[] parts1 = version1.Split('.');
        string[] parts2 = version2.Split('.');

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            int part1 = (i < parts1.Length) ? int.Parse(parts1[i]) : 0;
            int part2 = (i < parts2.Length) ? int.Parse(parts2[i]) : 0;

            if (part1 < part2)
            {
                return -1;
            }
            else if (part1 > part2)
            {
                return 1;
            }
        }

        return 0;
    }
    public static void ConsoleWrite<Key, Value>(this Dictionary<Key, Value> dic)
        {
            foreach (KeyValuePair<Key, Value> pair in dic)
            {
            Debug.Log(pair.Key.ToString() + " : " + pair.Value.ToString());
            }
        }
    public static string GetPathWithoutExtension(this string filePath)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

        string directoryPath = Path.GetDirectoryName(filePath);

        string pathWithoutExtension = Path.Combine(directoryPath, fileNameWithoutExtension);

        return pathWithoutExtension;
    }
    public static string GetTextureNameInPicURI(this string url)
    {
       string[] array = url.Split('/');
        string name = array[array.Length - 1];
        return Path.GetFileNameWithoutExtension(name);
    }
}
