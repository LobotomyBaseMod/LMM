using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.EventSystems;
using System.ComponentModel;
using System.Net;
using Assets.APIUtil;
using System.Collections.Generic;
using System.IO.Compression;

public class UIModPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum FileState
    {
        None,
        Empty,
        Lastest,
        Update,
        Downloading,
        UpdateCheck,
        CantCheck
    }
    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        progresstext.enabled = false;
        if (CantDown)
        {
            curstate = FileState.CantCheck;
            goto Skip;
        }
        if (!this.xml.IsNexus && !this.xml.IsGitHub)
        {
            curstate = FileState.None;
            goto Skip;
        }
        if (IsDownloading)
        {
            curstate = FileState.Downloading;
            progresstext.enabled = true;
            goto Skip;
        }
        if (this.xml.IsNexus && xml.fileid == -1)
        {
            curstate = FileState.Empty;
            goto Skip;
        }
        if (this.xml.IsGitHub && xml.g_fileid == -1)
        {
            curstate = FileState.Empty;
            goto Skip;
        }
        if (IsChecking)
        {
            curstate = FileState.UpdateCheck;
            goto Skip;
        }
        if (curdowninfo != null && curdowninfo.fileid > xml.fileid)
        {
            curstate = FileState.Update;
            goto Skip;
        }
        if (curdowninfo_github != null && curdowninfo_github.id > xml.g_fileid)
        {
            curstate = FileState.Update;
            goto Skip;
        }
        curstate = FileState.Lastest;


    Skip:
        statetext.text = LocalizeManager.Instance.GetText("UI_Btn_ModState_" + curstate.ToString());

        WarningImage.enabled = !IsContainUpperMod();
        if (UIModPanelController.Instance.curPanel == null) NeedImage.enabled = false;
    }
    public bool IsContainUpperMod()
    {
        var list = GetRequireMods();
        if (list == null) return true;
        foreach (var p in UIModPanelController.Instance.panels)
        {
            if (list.Count == 0) return true;
            if (p == this) return false;
            if (p.info == null) continue;
            if (list.Contains(p.info.modid)) list.Remove(p.info.modid);
        }
        Debug.Log("WHAT? UMP_ICUMError");
        return true;
    }
    public List<string> GetRequireMods()
    {
        if (info == null) return null;
        if (info.requiremods == null || info.requiremods.Count == 0) return null;
        return new List<string>(info.requiremods);
    }
    public string GetModId()
    {
        if (info == null) return null;
        if (info.modid == null || info.modid == string.Empty) return null;
        return info.modid;
    }
    public void OnClickState()
    {
        if (!xml.IsNexus && !xml.IsGitHub) return;
        if (curstate == FileState.Empty || curstate == FileState.Update)
        {
            IsDownloading = true;
            if (xml.IsNexus)
            {
                FileDownStart();
                progresstext.text = "0%";
            }
            if (xml.IsGitHub)
            {
                FileDownStart_GitHub();
                progresstext.text = "0%";
            }
        }
        if (curstate == FileState.CantCheck)
        {
            CantDown = false;
            if (xml.IsNexus)
            {
                CheckRecentFile();
            }
            if (xml.IsGitHub)
            {
                CheckRecentFile_GitHub();
            }
        }
    }
    public async void FileDownStart_GitHub()
    {
        string[] array = xml.g_modid.Split(' ');
        var result = await GitHubApiManager.Instance.GetLastestRelease(array[0], array[1]);
        curdowninfo_github = result.assets[0];
        if (curdowninfo_github != null)
        {
            DownloadManager.Instance.FileDownLoad(curdowninfo_github.browser_download_url, Path.Combine(GlobalManager.Instance.ModListFolderPath, xml.modfoldername + ".zip"), DownloadFileCompleted, DownloadProgressChanged);
        }
        else
        {
            CantDown = true;
            IsDownloading = false;
            curdowninfo = null;
        }
    }
    public async void FileDownStart()
    {
        curdowninfo = await NexusPageManager.Instance.GetDownloadLinkByModid(xml.modid);
        if (curdowninfo != null)
        {
            DownloadManager.Instance.FileDownLoad(curdowninfo.url, Path.Combine(GlobalManager.Instance.ModListFolderPath, xml.modfoldername + ".zip"), DownloadFileCompleted, DownloadProgressChanged);
        }
        else
        {
            CantDown = true;
            IsDownloading = false;
            curdowninfo = null;
        }
    }

    private void DownloadFileCompleted()
    {
        if (xml.IsNexus)
        {
            xml.fileid = curdowninfo.fileid;
        }
        if (xml.IsGitHub)
        {
            xml.g_fileid = curdowninfo_github.id;
        }
        UIModPanelController.Instance.RefreshPanelList();
        string filepath = Path.Combine(GlobalManager.Instance.ModListFolderPath, xml.modfoldername + ".zip");

        GlobalManager.Instance.AddMod(filepath, true);

        ModDescInit();

        IsDownloading = false;
        curdowninfo = null;
    }


    private void DownloadProgressChanged(float e)
    {
        progresstext.text = Mathf.Round(e*100).ToString() + "%";
    }
    public void OnClickActiveToggle(bool dummy)
    {
        xml.Useit = toggle.isOn;
        if (Initing) return;
        UIModPanelController.Instance.RefreshPanelList();
    }
    public void OnClickModPanel()
    {
        UIModPanelController.Instance.SetCurPanel(this);
        
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        starpos = rectTransform.anchoredPosition;
    }
    public void OnDrag(PointerEventData eventData)
    {
        // 드래그 중에 RectTransform의 위치를 업데이트합니다.
        rectTransform.anchoredPosition += eventData.delta / GetComponentInParent<Canvas>().scaleFactor;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        int currentIndex = transform.GetSiblingIndex();
        Debug.Log(currentIndex);
        
        RectTransform dropRectTransform = GetDropRectTransform(eventData, rectTransform);

        if (dropRectTransform != null)
        {
            int dropIndex = dropRectTransform.GetSiblingIndex();
            transform.SetSiblingIndex(dropIndex);
            Debug.Log(dropIndex);
            if (currentIndex == dropIndex)
            {
                rectTransform.anchoredPosition = starpos;
            }
        }
        else
        {
            rectTransform.anchoredPosition = starpos;
        }
        UIModPanelController.Instance.RefreshPanelList();
    }
    private RectTransform GetDropRectTransform(PointerEventData eventData, RectTransform cur)
    {
        // 드롭된 위치의 Collider 또는 RectTransform을 찾습니다.
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (var result in raycastResults)
        {
            RectTransform rectTransform = result.gameObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                if (rectTransform == cur) continue;
                if (rectTransform.gameObject.GetComponent<UIModPanel>() != null) return rectTransform;
            }
        }
        return null;
    }
    public void SetData(ModInfoXml modinfoxml)
    {
        Initing = true;
        this.gameObject.SetActive(true);
        xml = modinfoxml;
        ModDescInit();
        
        toggle.isOn = modinfoxml.Useit;
     
        curdowninfo = null;
        curdowninfo_github = null;

        if (info.IsNexus && xml.fileid != -1) CheckRecentFile();
        if (info.IsGitHub && xml.g_fileid != -1) CheckRecentFile_GitHub();

        Initing = false;
    }
    bool Initing;
    public void ModDescInit()
    {
        info = new ModInfo();
        info.Init(new DirectoryInfo(Path.Combine(GlobalManager.Instance.ModListFolderPath, xml.modfoldername)), xml.IsNexus,false, xml.IsGitHub);
        title.text = info.modname;
        this.gameObject.name = title.text;
    }
    public bool HighLight(bool ison, string keyword = null)
    {
        if (!ison)
        {
            title.text = title.text.Replace("<color=yellow>", "");
            title.text = title.text.Replace("</color=yellow>", "");
            return false;
        }
        else
        {
            Debug.Log("HighLight : "+ keyword);
            if (keyword == null || keyword == string.Empty || !title.text.Contains(keyword)) return false;

            title.text = title.text.Replace(keyword, $"<color=yellow>{keyword}</color=yellow>");
            return true;
        }
    }
    public async void CheckRecentFile()
    {
        IsChecking = true;
        curdowninfo = await NexusPageManager.Instance.GetDownloadLinkByModid(xml.modid);
        if (curdowninfo == null) CantDown = true;
        IsChecking = false;
    }
    public async void CheckRecentFile_GitHub()
    {
        IsChecking = true;
        string[] array = xml.g_modid.Split(' ');
        var result = await GitHubApiManager.Instance.GetLastestRelease(array[0], array[1]);
        curdowninfo_github = result.assets[0];
        if (curdowninfo_github == null) CantDown = true;
        IsChecking = false;
    }
    private RectTransform rectTransform;

    public bool IsChecking = false;

    public bool IsDownloading = false;

    public bool CantDown = false;

    public TextMeshProUGUI progresstext;

    public TextMeshProUGUI statetext;

    public FileState curstate;

    public ModInfo info;
    public ModInfoXml xml;
    public Toggle toggle;
    public TextMeshProUGUI title;

    public Vector2 starpos;

    public Image WarningImage;

    public Image NeedImage;

    public NexusModDownloadInfo curdowninfo;

    public GitHubAsset curdowninfo_github;
}
