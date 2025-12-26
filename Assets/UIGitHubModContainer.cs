using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using ImageMagick;
using System.Net;
using System.IO;
using System;
using UnityEngine.Networking;
public delegate void ModContainerClickCallback();
[Serializable]
public class UIGitHubModContainer : MonoBehaviour
{
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (delimg != null)
        {
            delimg.enabled = UIGitHubOwnerModListController.Instance.DelMod;
        }
    }
    public void SetData(GitHubModInfo_Upload info, GitHubRepos repos, ModContainerClickCallback cb)
    {
        if (delimg != null)
        {
            delimg.enabled = false;
        }
        this.gameObject.SetActive(true);

        modinfo = info;
        repoinfo = repos;

        Init();

        callback = cb;
    }
    public void OnClick()
    {
        callback();
    }
    public void OnClick_SearchAndDown()
    {
        if (UIGitHubModInfoView.Instance.gameObject.activeSelf) return;
        UIGitHubModInfoView.Instance.Open(modinfo);
    }
    public void OnClick_ModUpdate()
    {
        if (UIGitHubOwnerModListController.Instance.DelMod)
        {
            OnClickDelete();
            return;
        }
        DeveloperViewController.Instance.OpenUploadView(modinfo);
    }
    public void OnClickDelete()
    {
        PopupViewController.Instance.PopupChoice(ChoicePopupPair.ModDeleteChoice, DeleteMod);
    }
    public bool DeleteMod(bool isyes)
    {
        if (!isyes) return false;

        DeleteMod();
        return false;
    }
    public async void DeleteMod()
    {
        var result = await GitHubApiManager.Instance.DeleteMod(modinfo.owner, modinfo.modname, DeveloperViewController.Instance.Auth);

        if (result)
        {
            UIGitHubOwnerModListController.Instance.Init();
            PopupViewController.Instance.PopupNormal(NormalPopupPair.ModDeleteSuccess);
        }
        else
        {
            PopupViewController.Instance.PopupNormal(NormalPopupPair.ModDeleteFail);
        }
    }
    public async void Init()
    {
        title.text = modinfo.modname;
        img.enabled = false;
        StarText.text = repoinfo.stargazers_count.ToString();
        //this.StartCoroutine(LoadImageFromURL(modinfo.picture_url));
        await LoadImageFromData(modinfo.thumbdata);
    }
    public async Task<int> LoadImageFromData(string data)
    {
        var sp = ExtensionUtil.TryCreateSprite(ExtensionUtil.ImageDecode(data));

        img.sprite = sp;
        img.enabled = true;
        return 0;
    }
    //[HideInInspector]
    public GitHubRepos repoinfo;

    public GitHubModInfo_Upload modinfo;

    public TextMeshProUGUI title;

    public UnityEngine.UI.Image img;

    public Texture2D tex;

    public int fileid;

    public ModContainerClickCallback callback;

    public Image delimg;

    public TextMeshProUGUI StarText;
}
