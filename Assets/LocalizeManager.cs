using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Xml.Serialization;

public class LocalizeManager : Singleton<LocalizeManager>
{
	public void Init(string lang)
	{
		string localizepath = Path.Combine(Application.dataPath, "Localize", lang);
        if (!Directory.Exists(localizepath)) return;
        localizedic = new Dictionary<string, string>();
        curlang = lang;
        foreach (FileInfo file in new DirectoryInfo(localizepath).GetFiles())
        {
            try
            {
                LocalizeDataList list = LocalizeDataList.LoadData(file.FullName);
                foreach (LocalizeData data in list.list)
                {
                    localizedic[data.id] = data.value;
                }
            }
            catch (Exception e)
            {
                continue;
            }
        }
        SaveManager.Instance.Save("Lang",curlang);
    }
    public string GetText(string key)
    {
        if (localizedic == null) return null;
        if (!localizedic.ContainsKey(key)) return null;
        return localizedic[key];
    }
    public SupportLanguage GetCurLang()
    {
        switch (curlang)
        {
            case "kr":
                return SupportLanguage.KR;
            case "en":
                return SupportLanguage.EN;
            case "jp":
                return SupportLanguage.JP;
            case "ru":
                return SupportLanguage.RU;
            case "cn":
                return SupportLanguage.CN;
            case "cn_tr":
                return SupportLanguage.CN;
            default:
                return SupportLanguage.KR;
        }
    }
	public string curlang = "kr";
	public Dictionary<string, string> localizedic;
}
public class LocalizeDataList
{
    public static void SerializeData(LocalizeDataList data, string path)
    {
        using (StreamWriter streamWriter = new StreamWriter(path))
        {
            new XmlSerializer(typeof(LocalizeDataList)).Serialize(streamWriter, data);
        }
    }
    public static LocalizeDataList LoadData(string path)
    {
        LocalizeDataList result;
        using (StringReader stringReader = new StringReader(File.ReadAllText(path)))
        {
            result = (LocalizeDataList)new XmlSerializer(typeof(LocalizeDataList)).Deserialize(stringReader);
        }
        return result;
    }
    public List<LocalizeData> list = new List<LocalizeData>();
}
public class LocalizeData
{
    public string id = String.Empty;
    public string value = String.Empty;
}