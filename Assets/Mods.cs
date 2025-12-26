using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;


public class ModInfo
{

    public void Init(DirectoryInfo dir, bool IsNexus = false, bool IsWorkshop = false, bool IsGithub = false)
    {
        this.IsWorkshop = IsWorkshop;
        this.IsNexus = IsNexus;
        this.IsGitHub = IsGithub;
        this.foldername = dir.Name;
        this.modpath = dir;
        string str = string.Empty;
        string str2 = string.Empty;
        string text = string.Empty;
        this.modid = string.Empty;
        string lang = LocalizeManager.Instance.curlang;
        requiremods = new List<string>();
        bool flag = File.Exists(dir.FullName + "/Info/" + lang + "/info.xml");
        if (!flag)
        {
            lang = "en";
            flag = File.Exists(dir.FullName + "/Info/" + lang + "/info.xml");
        }
        if (!flag)
        {
            lang = "kr";
            flag = File.Exists(dir.FullName + "/Info/" + lang + "/info.xml");
        }
        if (flag)
        {
            string xml = File.ReadAllText(dir.FullName + "/Info/" + lang + "/info.xml");
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            XmlNode xmlNode = xmlDocument.SelectSingleNode("/info/name");
            XmlNodeList xmlNodeList = xmlDocument.SelectSingleNode("/info/descs").SelectNodes("desc");
            this.modname = xmlNode.InnerText;
            str = string.Concat(new string[]
            {
                "Folder : ",
                this.foldername,
                Environment.NewLine,
                "Name : ",
                this.modname,
                Environment.NewLine
            });
            xmlNode = xmlDocument.SelectSingleNode("/info/ID");
            bool flag2 = xmlNode != null;
            if (flag2)
            {
                str2 = "ID : " + xmlNode.InnerText + Environment.NewLine;
                this.modid = xmlNode.InnerText;
            }
            if (xmlNodeList != null)
            {
                foreach (object obj in xmlNodeList)
                {
                    XmlNode xmlNode2 = (XmlNode)obj;
                    text = text + Environment.NewLine + xmlNode2.InnerText;
                }
            }
            bool flag3 = File.Exists(dir.FullName + "/Info/GlobalInfo.xml");
            if (flag3)
            {
                xml = File.ReadAllText(dir.FullName + "/Info/GlobalInfo.xml");
                xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);
                xmlNode = xmlDocument.SelectSingleNode("/info/ID");
                bool flag4 = xmlNode != null;
                if (flag4)
                {
                    str2 = "ID : " + xmlNode.InnerText + Environment.NewLine;
                    this.modid = xmlNode.InnerText;
                }
                XmlNodeList requires = xmlDocument.SelectSingleNode("/info").SelectNodes("Require");
                if (requires != null)
                {
                    foreach (object obj in requires)
                    {
                        XmlNode xmlNode2 = (XmlNode)obj;
                        requiremods.Add(xmlNode2.InnerText);
                    }
                }
            }
            this.modinfo = str + str2 + text;
        }
        else
        {
            str = string.Concat(new string[]
           {
                "Folder : ",
                this.foldername,
                Environment.NewLine,
                "No Name",
                Environment.NewLine,
                "No ID"
           });
            this.modinfo = str;
            this.modname = this.foldername;
        }
    }
    public List<string> requiremods;

    public string foldername;

    public string modname;

    public string modinfo;

    public string modid;

    public DirectoryInfo modpath;

    public bool IsWorkshop;

    public bool IsNexus;

    public bool IsGitHub;
}
[Serializable]
public class ModListXml
{
    public static void SerializeData(ModListXml data, string path)
    {
        using (StreamWriter streamWriter = new StreamWriter(path))
        {
            new XmlSerializer(typeof(ModListXml)).Serialize(streamWriter, data);
        }
    }
    public static ModListXml LoadData(string path)
    {
        ModListXml result;
        using (StringReader stringReader = new StringReader(File.ReadAllText(path)))
        {
            result = (ModListXml)new XmlSerializer(typeof(ModListXml)).Deserialize(stringReader);
        }
        return result;
    }
    public List<ModInfoXml> list = new List<ModInfoXml>();
}
[Serializable]
public class ModInfoXml
{
    public ModInfoXml()
    {
    }
    public ModInfoXml(string modfolderpath)
    {
        modfoldername = modfolderpath;
    }
    public ModInfoXml(string modfolderpath, int modid, int fileid)
    {
        modfoldername = modfolderpath;
        IsNexus = modid != -1;
        this.modid = modid;
        this.fileid = fileid;
    }
    public ModInfoXml(string modfolderpath, string modid, long fileid)
    {
            modfoldername = modfolderpath;
            IsGitHub = true;
            this.g_modid = modid;
            this.g_fileid = fileid;
    }
    public string modfoldername = string.Empty;
    public bool Useit = true;
    public bool IsWorkShop = false;
    public bool IsNexus = false;
    public bool IsGitHub = false;
    public int modid = -1;
    public int fileid = -1;
    public string g_modid = "";
    public long g_fileid = -1;
}

