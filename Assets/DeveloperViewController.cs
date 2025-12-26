using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DeveloperViewController : SingletonBehavior<DeveloperViewController>
{
	public enum Views
	{
		AuthView,
		UploadView,
		OwnerView,
		CheckView
	}
	public void Start()
	{
		Close();
	}
	public void ClickLogOut()
	{
		Auth = string.Empty;
		ownerinfo = null;
		SaveManager.Instance.Delete(AuthKey);
		SetView(Views.AuthView);
	}
	public void ClickNewModUpload()
	{
		OpenUploadView();
	}

	public async void OnClickAuth()
	{
		var result = await GitHubApiManager.Instance.GetAuthKey();
		if (result != null)
		{
			SaveManager.Instance.Save(AuthKey, result);
			var r = await CheckAuth();
			if (r)
			{
				OpenOwnerView();
			}
		}
	}
	public async Task<bool> CheckAuth()
	{
		Auth = SaveManager.Instance.Load<string>(AuthKey);
		if (Auth == null) Auth = string.Empty;
		if (Auth == string.Empty) return false;

		var result = await GitHubApiManager.Instance.GetOwnerInfo(Auth);

		if (result == null) return false;

		ownerinfo = result;
		return true;
	}
	public void OpenOwnerView()
	{
		SetView(Views.OwnerView);
		UIGitHubOwnerModListController.Instance.Init();
	}
	public void OpenUploadView(GitHubModInfo_Upload info = null)
	{
		SetView(Views.UploadView);
		uploadview.Init(info);
	}
	public void Open()
	{
		this.gameObject.SetActive(true);
		SetView(Views.CheckView);

		CheckingOwner();
	}
	public void Close()
	{
		if (curview == Views.UploadView)
		{
			OpenOwnerView();
			return;
		}
		DisableView();
		this.gameObject.SetActive(false);
	}
	public async void CheckingOwner()
	{
		var result = await CheckAuth();
		if (!this.gameObject.activeSelf) return;
		if (result)
		{
			OpenOwnerView();
		}
		else
		{
			SetView(Views.AuthView);
		}
	}
	public void SetView(Views view)
	{
		foreach (var pair in ViewDic.dic)
		{
			pair.value.SetActive(pair.key == view);
		}
		curview = view;
	}
	public void DisableView()
	{
		foreach (var pair in ViewDic.dic)
		{
			pair.value.SetActive(false);
		}
		curview = (Views)int.MaxValue;
	}
	public Views curview;

	public GitHubAuthor ownerinfo;
	public string Auth = string.Empty;
	public static string AuthKey = "GitHubAuthKey.txt";

	public SerializeDictionary<Views, GameObject> ViewDic;

	public ModUploadView uploadview;
}