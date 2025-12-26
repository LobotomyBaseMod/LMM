using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.IO;
using System.Security.AccessControl;

public class SaveManager : Singleton<SaveManager>
{
    public void Save<T>(string FileName, T data)
	{
        var savedata = JsonConvert.SerializeObject(data);
        FileInfo f = new FileInfo(Path.Combine(Application.persistentDataPath, FileName));
        if (!Directory.Exists(f.Directory.FullName)) Directory.CreateDirectory(f.Directory.FullName);
        File.WriteAllText(Path.Combine(Application.persistentDataPath,FileName), savedata);
    }
    public void SaveExpand<T>(string FileName, T data)
    {
        var savedata = JsonConvert.SerializeObject(data);
        FileInfo f = new FileInfo(FileName);
        if (!Directory.Exists(f.Directory.FullName)) Directory.CreateDirectory(f.Directory.FullName);
        File.WriteAllText(FileName, savedata);
    }

    public T Load<T>(string FileName)
	{
        try
        {
            var path = Path.Combine(Application.persistentDataPath, FileName);
            if (!File.Exists(path)) return default(T);
            T loaddata = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            return loaddata;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message + Environment.NewLine + e.StackTrace);
            return default(T);
        }
    }
    public void Delete(string FileName)
    {
        if (File.Exists(Path.Combine(Application.persistentDataPath, FileName))) File.Delete(Path.Combine(Application.persistentDataPath, FileName));
    }
}