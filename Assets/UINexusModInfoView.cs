using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class UINexusModInfoView : SingletonBehavior<UINexusModInfoView>
{
	public void Open(NexusModInfoV2 modinfo)
	{
		this.gameObject.SetActive(true);
		curinfo = modinfo;

		mainimage.sprite = SpriteManager.Instance.GetSprite(curinfo.mod_pic.GetTextureNameInPicURI());
		titletext.text = curinfo.mod_name;
		summarytext.text = curinfo.mod_summary;

		ModInfoXml xmlinfo = GlobalManager.Instance.ModListFile.list.Find(x => x.IsNexus && x.modid == curinfo.modid);
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

		if (ConfigViewController.Instance.IsSidValid())
		{
			SubscribeBtn.gameObject.SetActive(true);
		}
		else
		{
			SubscribeBtn.gameObject.SetActive(false);
		}

	}
	public void Close()
	{
		this.gameObject.SetActive(false);
	}
	public void OpenModPage()
	{
		string path = "https://www.nexusmods.com/lobotomycorporation/mods/" + curinfo.modid.ToString();
		Application.OpenURL(path);
	}
	public void ClickSubscribe()
	{
		if (curxml == null)
		{
			SubscribeMod();
		}
		else
		{
			UnsubscribeMod();
		}
	}
	public void SubscribeMod()
	{
		ModInfoXml xml = new ModInfoXml(curinfo.mod_name+"_"+curinfo.modid,curinfo.modid,-1);

		Directory.CreateDirectory(Path.Combine(GlobalManager.Instance.ModListFolderPath, curinfo.mod_name + "_" + curinfo.modid));

		UIModPanelController.Instance.AddMod(xml);
		

		curxml = xml;
		subscribetext.text = LocalizeManager.Instance.GetText("UI_Btn_Subscribe_yes");
	}
	public void UnsubscribeMod()
	{
		ModInfoXml xmlinfo = GlobalManager.Instance.ModListFile.list.Find(x => x.IsNexus && x.modid == curinfo.modid);
		if (xmlinfo != null)
		{
			UIModPanelController.Instance.RemoveMod(xmlinfo);
			
			
		}
		curxml = null;
		subscribetext.text = LocalizeManager.Instance.GetText("UI_Btn_Subscribe_do");
	}
	public ModInfoXml curxml;

	public TextMeshProUGUI subscribetext;

	public TextMeshProUGUI summarytext;

	public TextMeshProUGUI titletext;

	public Image mainimage;

	public NexusModInfoV2 curinfo;

	public Button SubscribeBtn;
}
