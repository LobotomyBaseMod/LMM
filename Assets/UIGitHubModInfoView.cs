using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class UIGitHubModInfoView : SingletonBehavior<UIGitHubModInfoView>
{
	public Sprite TryCreateSprite(byte[] data)
	{
		Texture2D texture = new Texture2D(2, 2);

		texture.LoadImage(data);
		Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
		return sprite;
	}
	public void Open(GitHubModInfo_Upload modinfo)
	{
		this.gameObject.SetActive(true);
		curinfo = modinfo;
		var spname = curinfo.owner + "_" + curinfo.modname + "_Thumb";

		var	sp = TryCreateSprite(ExtensionUtil.ImageDecode(curinfo.thumbdata));

		mainimage.sprite = sp;
		titletext.text = curinfo.modname;
		summarytext.text = curinfo.desc;
		authortext.text = LocalizeManager.Instance.GetText("UI_GitHubView_AuthorName") + modinfo.owner;

		var curlang = LocalizeManager.Instance.GetCurLang();
		if (curinfo.DescDic.ContainsKey(curlang))
		{
			summarytext.text = curinfo.DescDic[curlang];
		}

		ModInfoXml xmlinfo = GlobalManager.Instance.ModListFile.list.Find(x => x.IsGitHub && x.g_modid == curinfo.ModId);
		if (xmlinfo != null)
		{
			curxml = xmlinfo;
			subscribetext.text =  LocalizeManager.Instance.GetText("UI_Btn_Subscribe_yes");
		}
		else
		{
			curxml = null;
			subscribetext.text = LocalizeManager.Instance.GetText("UI_Btn_Subscribe_do");
		}

		GitHubModCommentView.Instance.Active(curinfo);
	}
	public void Close()
	{
		this.gameObject.SetActive(false);
	}
	public void ClickAuthorSearch()
	{
		UIGitHubModListController.Instance.keywordinput.text = $"user:{curinfo.owner}";
		UIGitHubModListController.Instance.ClickSearch();
		Close();
	}
	public void OpenModPage()
	{
		string path = $"https://github.com/{curinfo.owner}/{curinfo.modname}";
		Application.OpenURL(path);
	}
	public bool IsExistGitHubMod(string modid)
	{
		var id = modid.Replace('/', ' ');
		var value = GlobalManager.Instance.ModListFile.list.Find(x => x.IsGitHub && x.g_modid == id);
		return value != null;
	}
	public async void ClickSubscribe()
	{
		if (curxml == null)
		{
			if (curinfo.dependancy != null && curinfo.dependancy.Count > 0)
			{
				var list = new List<GitHubModInfo_Upload>();
				var str = string.Empty;
				foreach (var modid in curinfo.dependancy)
				{
					if (IsExistGitHubMod(modid)) continue;
					var data = await GitHubApiManager.Instance.ModVaild(modid);
					if (data == null) continue;
					list.Add(data);
					str += modid + Environment.NewLine;
				}
				if (list.Count == 0)
				{
					SubscribeMod();
					return;
				}
				PopupViewController.Instance.PopupChoice(ChoicePopupPair.ModDependancyDetect, delegate (bool isyes) { SubscribeAllMod(list); return false; }, str);
				return;
			}
			SubscribeMod();
		}
		else
		{
			UnsubscribeMod();
		}
	}
	public void SubscribeAllMod(List<GitHubModInfo_Upload> list)
	{
		foreach (var mod in list)
		{
			ModInfoXml xml = new ModInfoXml(mod.ModId, mod.ModId, -1);

			Directory.CreateDirectory(Path.Combine(GlobalManager.Instance.ModListFolderPath, mod.ModId));

			UIModPanelController.Instance.AddMod(xml);
		}
		SubscribeMod();
	}
	public void SubscribeMod()
	{
		ModInfoXml xml = new ModInfoXml(curinfo.ModId, curinfo.ModId,-1);

		Directory.CreateDirectory(Path.Combine(GlobalManager.Instance.ModListFolderPath, curinfo.ModId));

		UIModPanelController.Instance.AddMod(xml);

		curxml = xml;
		subscribetext.text = LocalizeManager.Instance.GetText("UI_Btn_Subscribe_yes");
	}
	public void UnsubscribeMod()
	{
		ModInfoXml xmlinfo = GlobalManager.Instance.ModListFile.list.Find(x => x.IsGitHub && x.g_modid == curinfo.ModId);
		if (xmlinfo != null)
		{
			UIModPanelController.Instance.RemoveMod(xmlinfo);
			
			
		}
		curxml = null;
		subscribetext.text = LocalizeManager.Instance.GetText("UI_Btn_Subscribe_do");
	}
	public TextMeshProUGUI authortext;

	public ModInfoXml curxml;

	public TextMeshProUGUI subscribetext;

	public TextMeshProUGUI summarytext;

	public TextMeshProUGUI titletext;

	public Image mainimage;

	public GitHubModInfo_Upload curinfo;
}
