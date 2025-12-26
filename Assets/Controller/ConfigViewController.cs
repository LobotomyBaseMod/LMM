using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;

public class ConfigViewController : SingletonBehavior<ConfigViewController>
{
	public void Start()
	{
        Close();
        ConfigViewController.Instance = this;
    }
	public void Awake()
	{
        updateCheck();
	}
	public void Init()
	{
        string sid = NexusPageManager.Instance.GetCookie("nexusmods_session");
        if(sid != null) sid_text.text = sid;
        sidCheck();
        updateCheck();
        this.StartOptionMenu.value = (int)GlobalManager.GlobalOption_StartOption;
    }
    public void ChangeStartOption()
    {
        StartOption cur = (StartOption)this.StartOptionMenu.value;
        GlobalManager.GlobalOption_StartOption = cur;
    }
    public void ClickOpenSessionCheckQuest()
    {
        Process.Start("https://www.nexusmods.com/site/articles/91");
    }
    public void ClickOpenLogPath()
    {
        
        UnityEngine.Debug.Log("Path : " + Application.persistentDataPath);
        var path = "C:/Users/kj022/AppData/LocalLow/DefaultCompany/LobotomyModManager";
        if (path == Application.persistentDataPath) UnityEngine.Debug.Log("true");
        Process.Start("explorer.exe", path.Replace("/",@"\"));
        
    }
    public void ClickLangSelect()
    {
        GlobalManager.Instance.LangSelectView.gameObject.SetActive(true);
    }
    public void Apply()
    {
        string newsid = sid_text.text;
        NexusPageManager.Instance.SetCookie("nexusmods_session", newsid);
        sidCheck();
    }
    public async void UpdateLMM()
    {
        Application.Quit();
        DirectoryInfo dir = new DirectoryInfo(Application.dataPath);
        dir = dir.Parent;
        string args0 = dir.FullName;
        string args1 = Path.Combine(dir.FullName, "LobotomyModManager.exe");
        string args2 = Path.Combine(Application.dataPath, "Updater");
        string args = "\"" + args0 + "\"" + " " + "\"" + args1 + "\"" + " " + "\"" + args2 + "\"";
        Process p = Process.Start(Application.dataPath + "/Updater/LMMUpdater.exe", args);
        Application.Quit();
    }
    public void EndUpdateDownload()
    {
        ZipArchive zipfile = ZipFile.OpenRead(Path.Combine(Application.dataPath,  "LMMupdate.zip"));
        zipfile.ExtractToDirectory(Path.Combine(Application.dataPath, "Updater"), true);
        zipfile.Dispose();
        File.Delete(Path.Combine(Application.dataPath, "LMMupdate.zip"));
        UpdateLMM();
    }
    private void DownloadProgressChanged(float e)
    {
        updateStateText.text = MathF.Round(e*100).ToString() + "%";
    }
    public async void ClickUpdate()
    {
        if (!updateState) return;
        //if (!sidState) return;
       // var info = await NexusPageManager.Instance.GetDownloadLinkByFileid(updateinfo.id, true);
        DownloadManager.Instance.FileDownLoad(updateinfo.assets.Find(x => x.name.Contains("Updater")).browser_download_url, Path.Combine(Application.dataPath, "LMMupdate.zip"), EndUpdateDownload, DownloadProgressChanged);

    }
	public void Open()
    {
        this.gameObject.SetActive(true);
        Init();
    }
    public void Close()
    {
        this.gameObject.SetActive(false);
    }
    public async void sidCheck()
    {
        sidState = false;
        sidStateText.text = LocalizeManager.Instance.GetText("UI_Text_sidCheck_check");
        sidStateText.color = Color.green;
        var result = await NexusPageManager.Instance.GetDownloadLinkByModid(2);
        if (result != null)
        {
            sidStateText.text = LocalizeManager.Instance.GetText("UI_Text_sidCheck_ok");
            sidStateText.color = Color.blue;
            sidState = true;
        }
        else
        {
            sidStateText.text = LocalizeManager.Instance.GetText("UI_Text_sidCheck_error"); 
            sidStateText.color = Color.red;
        }
    }
    public async void updateCheck()
    {
        updateStateText.text = LocalizeManager.Instance.GetText("UI_Text_UpdateCheck_check");
        //updateinfo = await NexusPageManager.Instance.GetLastestUpdateInfo();
        updateinfo = await GitHubApiManager.Instance.GetLastestLMMRelease();
        if (updateinfo == null)
        {
            updateStateText.text = LocalizeManager.Instance.GetText("UI_Text_UpdateCheck_error");
            return;
        }
        int result = ExtensionUtil.CompareVersions(GlobalManager.curversion, updateinfo.name.Trim());
        UnityEngine.Debug.Log("result = " + result);
        if (result >= 0)
        {
            updateStateText.text = LocalizeManager.Instance.GetText("UI_Text_UpdateCheck_recent");
            updateState = false;
            return;
        }
        if (result == -1)
        {
            updateStateText.text = LocalizeManager.Instance.GetText("UI_Text_UpdateCheck_exist");
            updateState = true;
        }
    }
    public bool IsSidValid()
    {
        return sidState;
    }
    public TMP_InputField sid_text;

    public TextMeshProUGUI sidStateText;

    public TextMeshProUGUI updateStateText;

    public bool sidState = false;

    public bool updateState = false;

    //public ModFileInfo updateinfo;

    public GitHubRelease updateinfo;

    public TMP_Dropdown StartOptionMenu;
}
public enum StartOption
{
    Patch,
    Paste
}