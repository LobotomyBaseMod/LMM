using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SimpleFileBrowser;
using TMPro;
using System.IO;
using Steamworks;
using System.Net;
using System.Diagnostics;
using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Common;
using SFB;
using System.Net.WebSockets;
using System.Threading;
using Newtonsoft.Json;
using System.Web;

public class GlobalManager : SingletonBehavior<GlobalManager>
{
    public override void OnApplicationQuit()
    {

        base.OnApplicationQuit();
        if (steaminit)
        {
            SteamAPI.Shutdown();
        }
    }
    public void Start()
    {
        GamePath = string.Empty;
        modlistdata = null;
        modlistfile = null;
        DeveloperViewController.Instance.CheckAuth();
        ConfigViewController.Instance.sidCheck();
        /*
        var s = SaveManager.Instance.Load<string>("CurGameName");
        if (s == null)
        {
            CurGameName = "LobotomyCorp";
        }
        else
        {
            CurGameName = s;
        }
        */
        //UnityEngine.Debug.Log(CurGameName);
        //SaveManager.Instance.Save("CurGameName", CurGameName);

        foreach (var pair in Views.dic)
        {
            //pair.value.SetActive(true);
            pair.value.SetActive(false);
        }

        versiontext.text = curversion + "v";
        string lang = SaveManager.Instance.Load<string>("Lang");
        LangSelectView.SetActive(lang == null);
        if (lang != null)
        {
            LocalizeManager.Instance.Init(lang);
            UITextDataLoader.AllChange();
        }
        UnityEngine.Debug.Log("Start GlobalManager");
        SteamAPI.Shutdown();
        var id = CurAppId.m_AppId;
        SaveManager.Instance.SaveExpand(Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, "steam_appid.txt"), id);
        if (SteamAPI.Init())
        {
            UnityEngine.Debug.Log("Init start");
            steaminit = true;
            SetGamePath(GetGamePath());
            UnityEngine.Debug.Log("Inited");
        }
        SpriteManager.Instance.Init();
        if (GamePath != String.Empty)
        {
            modlistdata = ModListFile;
            DirectoryInfo info = new DirectoryInfo(ModListFolderPath);
            List<ModInfoXml> exlist = new List<ModInfoXml>();
            if (modlistdata != null)
            {
                UnityEngine.Debug.Log($"Cur Mod Count : {modlistdata.list.Count}");
            }
            foreach (var subdir in info.GetDirectories())
            {
                if (modlistdata.list.Find(x => x.modfoldername == subdir.Name) != null) continue;
                ModInfoXml xml = new ModInfoXml(subdir.Name);
                modlistdata.list.Add(xml);
                UnityEngine.Debug.Log($"Load New Mod {subdir.Name}");
            }

            foreach (var i in modlistdata.list.ToArray())
            {
                if (info.GetDirectories().ToList().Find(x => x.Name == i.modfoldername) == null)
                {
                    modlistdata.list.Remove(i);
                    UnityEngine.Debug.Log($"Remove Mod {i.modfoldername}");
                }
                if (modlistdata.list.Find(x => x != i && i.modfoldername == x.modfoldername) != null)
                {
                    modlistdata.list.Remove(i);
                    UnityEngine.Debug.Log($"Remove Mod {i.modfoldername}");
                    continue;
                }
            }
            ModListFile = modlistdata;
            UIModPanelController.Instance.Init();
        }
        TestDo();
    }
    public async void TestDo()
    {
        //var r = await GitHubApiManager.Instance.Starred("LobotomyBaseMod", "LMM", "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",false);

        /*
        var r = await GitHubApiManager.Instance.GetCommentIssue("LobotomyBaseMod", "LMM");
        var r2 = await GitHubApiManager.Instance.UploadComment("LobotomyBaseMod", "LMM", r.number, "LMM Upload Test", GitHubApiManager.KEY);
        SaveManager.Instance.Save("TestDo.txt", r2);
        */
        //test = await GitHubApiManager.Instance.GetRelease();
    }
    public string testtext;
    public async void NMSSOTest()
    {
        string redirectURL = "http://127.0.0.1:50627";
        var url = $"https://users.nexusmods.com/oauth/authorize" + $"?client_id=vortex_loopback&response_type=code&scope=openid%20profile%20email&redirect_uri={redirectURL}";
        Application.OpenURL(url);

        var token = await StartLocalServer(redirectURL);
    }
    public static string Stringify(Dictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        foreach (var kvp in parameters)
        {
            if (sb.Length > 0)
            {
                sb.Append("&");
            }

            sb.AppendFormat("{0}={1}", kvp.Key, kvp.Value);
        }
        return sb.ToString();
    }
    public async Task<string> StartLocalServer(string redirectURL)
    {
        string clientId = "b4b76ce9e57fbeee2163";
        string clientSecret = "7a4406bd25c1639d96b2036c347c796d9c57ab35";
        using (var listener = new HttpListener())
        {
            listener.Prefixes.Add(redirectURL + "/");
            listener.Start();
            UnityEngine.Debug.Log("Waiting for NexusNod authorization...");

            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            // 넥서스로부터 받은 인증 코드 읽기
            string code = request.QueryString["code"];

            // 넥서스로부터 받은 인증 코드 출력
            UnityEngine.Debug.Log($"Received authorization code: {code}");

            // 수신된 인증 코드에 대한 응답 보내기
            string responseString = "<html><head><title>GitHub Authorization</title></head><body><h1>Authorization Received</h1><p>Thank you for authorizing the application. You can now close this window.</p></body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();

            // 서버 종료
            listener.Stop();

            var url = "https://users.nexusmods.com/oauth/token";
            var data = new Dictionary<string, string>();
            data["grant_type"] = "authorization_code";
            data["client_id"] = "vortex_loopback";
            data["code"] = code;
            data["redirect_uri"] = "http://127.0.0.1:50627";
            string d = Stringify(data);
            var dic = new Dictionary<string, string>();
            dic["Content-Type"] = "application/x-www-form-urlencoded; charset=UTF-8";
            var result = await GitHubApiManager.Instance.Request(url, "", d, "POST", dic);
            var result_str = result.key;
            UnityEngine.Debug.Log($"accessToken: {result_str}");
            return result_str;
        }
    }
    public void ChangeCurGame(string gamename)
    {
        CurGameName = gamename;
        Start();
    }
    public void OnClickStartGame()
    {
        //NMSSOTest();
        //return;
        /*
        var upload = new GitHubModInfo_Upload();
        upload.modname = "TESTMOD";
        upload.owner = "abcdcode";
        upload.thumbdata = ExtensionUtil.ImageEncode("C:/Users/kj022/Downloads/사고방식이.jpg");
        upload.filepath = "C:/Program Files (x86)/Steam/steamapps/common/LobotomyCorp/LobotomyCorp_Data/BaseMods/abcdcode_InvenOp_MOD.zip";
        upload.desc = "BYE!!!!";
        upload.topics = new List<string>() {};

        GitHubApiManager.Instance.UploadMod_AuthTest(upload);
        */

        if (Started) return;
        Started = true;
        StartGame();

        //bool result = PatchProgram.Patch();

    }
    public bool Started;

    public void StartGame()
    {

        string args0 = GlobalManager.Instance.GameManagedFolderPath;
        string args1 = Application.dataPath + PatchFilePath;
        if (File.Exists(Path.Combine(args1, "BaseMod", "BackUp"))) File.Delete(Path.Combine(args1, "BaseMod", "BackUp"));
        string args2 = Application.dataPath;
        string args3 = GlobalOption_StartOption.ToString();
        string args = "\"" + args0 + "\"" + " " + "\"" + args1 + "\"" + " " + "\"" + args2 + "\"" + " " + "\"" + args3 + "\"";
        UnityEngine.Debug.Log(args);
        Process p = Process.Start(Application.dataPath + PatchPath, args);
        p.WaitForExit();
        int result = p.ExitCode;
        if (result == 0)
        {
            if (steaminit)
            {
                string steamAppUrl = "steam://run/" + CurAppId.m_AppId;
                Process.Start(steamAppUrl);
            }
            else
            {
                Process.Start(GamePath);

            }
            //Process.Start(info.FullName);
            Application.Quit();
        }
        else
        {
            UnityEngine.Debug.Log("게임 패치 중 문제가 발생했습니다. 게임을 실행하지 않습니다.");
            Started = false;
            PopupViewController.Instance.PopupNormal(NormalPopupPair.GameStartError);
        }
    }
    public void SaveNewModListDataList(ModListXml xml)
    {
        ModListFile = xml;
    }
    public void SetGamePath(string path)
    {
        GamePath = path;
        pathtext.text = path;
        SaveManager.Instance.Save(SavePath, path);

        modlistdata = ModListFile == null ? new ModListXml() : ModListFile;
        UnityEngine.Debug.Log($"Cur Mod Count : {modlistdata.list.Count}");
        DirectoryInfo info = new DirectoryInfo(ModListFolderPath);
        List<ModInfoXml> exlist = new List<ModInfoXml>();
        foreach (var subdir in info.GetDirectories())
        {
            if (modlistdata.list.Find(x => x.modfoldername == subdir.Name) != null) continue;
            ModInfoXml xml = new ModInfoXml(subdir.Name);
            modlistdata.list.Add(xml);
            UnityEngine.Debug.Log($"Load New Mod {subdir.Name}");
        }
        foreach (var i in modlistdata.list.ToArray())
        {
            if (info.GetDirectories().ToList().Find(x => x.Name == i.modfoldername) == null)
            {
                UnityEngine.Debug.Log($"Remove Mod {i.modfoldername}");
                modlistdata.list.Remove(i);
                continue;
            }
            if (modlistdata.list.Find(x => x != i && i.modfoldername == x.modfoldername) != null)
            {
                UnityEngine.Debug.Log($"Remove Mod {i.modfoldername}");
                modlistdata.list.Remove(i);
                continue;
            }
        }
        ModListFile = modlistdata;
        UnityEngine.Debug.Log($"Cur Mod Count : {modlistdata.list.Count}");
        UIModPanelController.Instance.Init();
    }
    public void AddMod(string[] paths)
    {
        string path = paths[0];
        string filename = Path.GetFileNameWithoutExtension(path);
        UnityEngine.Debug.Log(filename);
        if (ModListFile.list.Find(x => x.modfoldername == filename) != null)
        {
            UnityEngine.Debug.Log("Already Exist Mod!");
            return;
        }
        ModInfoXml xml = AddMod(path);
        UIModPanelController.Instance.AddMod(xml);
    }

    public static void ExtractZipFile(string zipFilePath, string extractPath)
    {
        UnityEngine.Debug.Log("zipFilePath : " + zipFilePath);
        UnityEngine.Debug.Log("extractPath : " + extractPath);
        // 폴더가 존재하지 않으면 생성
        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
        }
        var path = @"c:\LMMT";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        // 압축 파일 열기
        using (var archive = ZipFile.OpenRead(zipFilePath))
        {
            // 압축 파일 내의 모든 파일 및 폴더에 대해 반복
            foreach (ZipArchiveEntry entry in archive.Entries)
            {

                try
                {
                    var fullpath = Path.Combine(path, entry.FullName);
                    if (fullpath.EndsWith('/'))
                    {
                        fullpath = fullpath.Remove(fullpath.Length - 1);
                        Directory.CreateDirectory(fullpath);
                        continue;
                    }
                    DirectoryInfo d = Directory.CreateDirectory(Path.GetDirectoryName(fullpath));
                    // 파일인 경우 압축 해제
                    entry.ExtractToFile(fullpath, true);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log(e.Message + Environment.NewLine + e.StackTrace);
                }
            }
        }
        File.Delete(zipFilePath);
        ExtensionUtil.DirMove(path, extractPath);
        Console.WriteLine("압축 해제가 완료되었습니다.");
    }

    public ModInfoXml AddMod(string filepath, bool removezip = false, int modid = -1, int fileid = -1)
    {
        string filename = Path.GetFileNameWithoutExtension(filepath);
        string modpath = Path.Combine(GlobalManager.Instance.ModListFolderPath, filename);

        //ZipArchive zipfile = ZipFile.Open(filepath, ZipArchiveMode.Read);
        // zipfile.ExtractToDirectory(modpath, true);
        //zipfile.Dispose();

        ExtractZipFile(filepath, modpath);

        /*
        using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(filepath)))
        {
            ZipEntry entry;
            // 모든 엔트리(파일 또는 폴더)에 대해 반복
            while ((entry = zipInputStream.GetNextEntry()) != null)
            {
                string entryPath = Path.Combine(modpath, entry.Name);
                if (entry.IsDirectory)
                {
                    // 디렉토리인 경우 폴더 생성
                    Directory.CreateDirectory(entryPath);
                }
                else
                {
                    // 파일인 경우 파일로 추출
                    using (FileStream outputStream = File.Create(entryPath))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        // 버퍼를 사용하여 데이터를 읽어서 파일에 쓰기
                        while ((bytesRead = zipInputStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outputStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }*/

        if (removezip) File.Delete(filepath);

        DirectoryInfo dir = new DirectoryInfo(modpath);
        DirectoryInfo Finaldir = ExtensionUtil.DoubleFolderChecking(dir);
        if (Finaldir.FullName != dir.FullName)
        {
            ExtensionUtil.DirMove(Finaldir.FullName, dir.FullName + "_LMMtmp");
            Directory.Delete(dir.FullName);
            ExtensionUtil.DirMove(dir.FullName + "_LMMtmp", dir.FullName);
        }

        return new ModInfoXml(filename, modid, fileid);
    }
    public void FindLocalMod()
    {
        //GlobalManager.LoadFileWindow(new FileBrowser.OnSuccess(this.AddMod), FileBrowser.PickMode.Files ,"모드 압축 파일 찾기(.zip)");
        var paths = StandaloneFileBrowser.OpenFilePanel("모드 압축 파일 찾기(.zip)", "", "zip", false);
        AddMod(paths);
    }
    public void SetGamePath(string[] paths)
    {
        SetGamePath(paths[0]);
    }
    public void FindGamePath()
    {
        //GlobalManager.LoadFileWindow(new FileBrowser.OnSuccess(this.SetGamePath), FileBrowser.PickMode.Files, "로보토미 코퍼레이션 실행파일 찾기");
        var paths = StandaloneFileBrowser.OpenFilePanel($"{CurGameName} 실행파일 찾기", "", "exe", false);
        SetGamePath(paths);
    }

    public static bool LoadFileWindow(FileBrowser.OnSuccess success, FileBrowser.PickMode pickmode, string desc)
    {
        return FileBrowser.ShowLoadDialog(success, null, pickmode, false, null, desc, "선택");
    }

    static string GetGamePath()
    {
        string steamPath;
        string path = SaveManager.Instance.Load<string>(SavePath);
        if (path != null && File.Exists(path)) return path;
        try
        {
            SteamApps.GetAppInstallDir(CurAppId, out steamPath, 100000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"오류 발생: {ex.Message}");
            return null;
        }
        steamPath = Path.Combine(steamPath, $"{CurGameName}.exe");

        return steamPath;
    }




    public ModListXml ModListFile
    {
        get
        {
            UnityEngine.Debug.Log("ModListFile 1");
            if (modlistfile != null) return modlistfile;
            UnityEngine.Debug.Log("ModListFile 2");
            if (ModListFolderPath == String.Empty) return null;
            UnityEngine.Debug.Log("ModListFile 3");
            string path = Path.Combine(ModListFolderPath, "BaseModList_v2.xml");
            if (!File.Exists(path)) return null;
            UnityEngine.Debug.Log("ModListFile 4");
            return ModListXml.LoadData(path);
        }
        set
        {
            modlistfile = value;
            ModListXml.SerializeData(modlistfile, Path.Combine(ModListFolderPath, "BaseModList_v2.xml"));
        }
    }
    private ModListXml modlistfile;
    public string GameManagedFolderPath
    {
        get
        {
            if (GamePath == String.Empty) return String.Empty;
            string path = Path.Combine(GameFolderPath, $"{CurGameName}_Data", "Managed");
            return path;
        }
    }

    public string ModListFolderPath
    {
        get
        {
            if (GamePath == String.Empty) return String.Empty;
            string path = Path.Combine(GameFolderPath, $"{CurGameName}_Data", "BaseMods");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }
    public string GameFolderPath
    {
        get
        {
            if (GamePath == String.Empty) return String.Empty;
            return new FileInfo(GamePath).Directory.FullName;
        }
    }
    public ModListXml modlistdata;

    public TextMeshProUGUI pathtext;

    public TextMeshProUGUI versiontext;

    public bool steaminit;

    public string GamePath = String.Empty;

    public static AppId_t LobotomyAppId = new AppId_t(568220);

    public static AppId_t CurAppId
    {
        get
        {
            return GlobalManager.Instance.AppIdDic[CurGameName].appid;
        }
    }
    public static string PatchPath
    {
        get
        {
            return GlobalManager.Instance.AppIdDic[CurGameName].patcherpath;
        }
    }
    public static string PatchFilePath
    {
        get
        {
            return GlobalManager.Instance.AppIdDic[CurGameName].patchfilepath;
        }
    }
    public static string SavePath
    {
        get
        {
            if (CurGameName == "LobotomyCorp")
            {
                return "GamePath.json";
            }
            return $"GamePath_{CurGameName}.json";
        }
    }
    public static StartOption GlobalOption_StartOption
    {
        get
        {
            return GlobalOption.startoption;
        }
        set
        {
            var tmp = GlobalOption;
            tmp.startoption = value;
            GlobalOption = tmp;
        }
    }
    public static GlobalOptionData GlobalOption
    {
        get
        {
            if (globaloption == null)
            {
                globaloption = SaveManager.Instance.Load<GlobalOptionData>("GlobalOption");
                if (globaloption == null) globaloption = new GlobalOptionData();
                globaloption.CurGameName = "LobotomyCorp";
            }
            return globaloption;
        }
        set
        {
            globaloption = value;
            SaveManager.Instance.Save("GlobalOption", globaloption);
        }
    }
    private static GlobalOptionData globaloption;
    public static string CurGameName
    {
        get
        {
            return GlobalOption.CurGameName;
        }
        set
        {
            var tmp = GlobalOption;
            tmp.CurGameName = value;
            GlobalOption = tmp;
        }
    }

    public static string curversion = "1.3.9";

    public List<GitHubRelease> test;

    public GameObject LangSelectView;

    public SerializeDictionary<string, GameObject> Views;

    public SerializeDictionary<string, PGameInfo> AppIdDic;
}
[Serializable]
public class PGameInfo
{
    public AppId_t appid;
    public string gamename;
    public string patcherpath;
    public string patchfilepath;
    public List<string> startoptions;

}
[Serializable]
public class GlobalOptionData
{
    public StartOption startoption = StartOption.Patch;
    public string CurGameName = "LobotomyCorp";
}

