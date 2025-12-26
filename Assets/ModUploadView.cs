using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleFileBrowser;
using System.IO;
using SFB;

public class ModUploadView : MonoBehaviour
{
	public void Init(GitHubModInfo_Upload info = null)
	{
		dependancyContainer.Init();
		var lang = LocalizeManager.Instance.GetCurLang();
		if (info == null)
		{
			owner = string.Empty;
			modname = string.Empty;
			TMP_modname.text = string.Empty;
			thumbdata = null;
			thumbpreview.sprite = null;
			filepath = string.Empty;
			TMP_filepath.text = string.Empty;
			DescDic = new SerializeDictionary<SupportLanguage, string>();

			TMP_langDropDown.value = (int)lang;
			curlang = lang;
			DescDropDown(0);
		}
		else
		{
			owner = info.owner;
			modname = info.modname;
			TMP_modname.text = modname;
			thumbdata = ExtensionUtil.ImageDecode(info.thumbdata);
			thumbpreview.sprite = ExtensionUtil.TryCreateSprite(thumbdata);
			filepath = string.Empty;
			DescDic = new SerializeDictionary<SupportLanguage, string>();
			foreach (var v in info.DescDic)
			{
				DescDic[v.Key] = v.Value;
			}
			TMP_langDropDown.value = (int)lang;
			curlang = lang;
			DescDropDown(0);

			foreach (var d in info.dependancy)
			{
				dependancyContainer.AddDependancy(d);
			}
		}
	}
	public void SetOwner()
	{
		owner = DeveloperViewController.Instance.ownerinfo.login;
		TMP_owner.text = LocalizeManager.Instance.GetText("DevView_AuthOwner") + owner;
	}
	public void DescDropDown(int value)
	{
		Debug.Log("DescDropDown : " + TMP_langDropDown.value);
		SupportLanguage lang = (SupportLanguage)TMP_langDropDown.value;
		curlang = lang;
		if (!DescDic.ContainsKey(lang)) DescDic[lang] = string.Empty;
		TMP_moddesc.text = DescDic[lang];
	}
	public void Update()
	{
		if (DeveloperViewController.Instance.ownerinfo == null) return;
		SetOwner();

		modname = TMP_modname.text;
		DescDic[curlang] = TMP_moddesc.text;
	}
	public async void OnClickUpload()
	{
		if (modname.Contains(" "))
		{
			PopupViewController.Instance.PopupNormal(NormalPopupPair.ModUploadCheck_ModNameContainBlank);
			return;
		}
		if (filepath == string.Empty || !File.Exists(filepath))
		{
			PopupViewController.Instance.PopupNormal(NormalPopupPair.ModUploadCheck_ModFileNull);
			return;
		}
		List<string> dlist = dependancyContainer.GetDependancy();
		foreach (var depend in dlist)
		{
			var cr = await GitHubApiManager.Instance.ModVaild(depend);
			if (cr == null)
			{
				PopupViewController.Instance.PopupNormal(NormalPopupPair.ModUploadCheck_ModDependancy, null, depend);
				return;
			}
		}

		GitHubModInfo_Upload d = new GitHubModInfo_Upload();
		d.owner = owner;
		d.modname = modname;
		d.thumbdata = ExtensionUtil.ImageEncode(thumbdata);
		d.DescDic = DescDic.ToDic();
		d.filepath = filepath;

		var desc = string.Empty;
		foreach (var pair in d.DescDic)
		{
			var s = pair.Value.Replace('\n', ' ');
			 s = s.Replace('\r', ' ');
			desc += s + " , ";
		}

		StringBuilder r= new StringBuilder();

			r.Append(desc);

		const int maxLength = 300;
		if (r.Length > maxLength)
		{
			r.Length = maxLength;
			r.Append("...");
		}

		d.desc = r.ToString();

		d.dependancy = dlist;

		var result = await GitHubApiManager.Instance.UploadMod(d, DeveloperViewController.Instance.Auth);
		if (result)
		{
			PopupViewController.Instance.PopupNormal(NormalPopupPair.ModUploadSuccess);
		}
		else
		{
			PopupViewController.Instance.PopupNormal(NormalPopupPair.ModUploadFail);
		}
		return;
	}
	public void FindThumb()
	{
		//GlobalManager.LoadFileWindow(new FileBrowser.OnSuccess(this.SetThumb), FileBrowser.PickMode.Files, "썸네일 찾기(.png)");
		var paths = StandaloneFileBrowser.OpenFilePanel("썸네일 찾기", "", "png", false);
		SetThumb(paths);
	}
	public void SetThumb(string[] paths)
	{
		var path = paths[0];
		thumbdata = File.ReadAllBytes(path);
		thumbpreview.sprite = ExtensionUtil.TryCreateSprite(thumbdata);
	}
	public void FindPath()
	{
		var paths = StandaloneFileBrowser.OpenFilePanel("모드 파일 찾기", "", "zip", false);
		SetPath(paths);
		//GlobalManager.LoadFileWindow(new FileBrowser.OnSuccess(this.SetPath), FileBrowser.PickMode.Files, "모드 파일 찾기(.zip)");
	}
	public void SetPath(string[] paths)
	{
		var path = paths[0];
		filepath = path;
		TMP_filepath.text = filepath;
	}
	public TMP_InputField TMP_modname;
	public TextMeshProUGUI TMP_owner;
	public TextMeshProUGUI TMP_filepath;
	public TMP_InputField TMP_moddesc;

	public TMP_Dropdown TMP_langDropDown;

	public ModUploadView_DependancyContainer dependancyContainer;

	public SupportLanguage curlang;

	public string owner;
	public string modname;
	public byte[] thumbdata;
	public Image thumbpreview;
	public string filepath;
	public SerializeDictionary<SupportLanguage, string> DescDic;


}