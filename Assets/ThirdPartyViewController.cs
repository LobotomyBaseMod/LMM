using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

public class ThirdPartyViewController : SingletonBehavior<ThirdPartyViewController>
{
	public void ClickATA()
	{
		GlobalManager.Instance.ChangeCurGame("Alina of the Arena");
		Close();
	}
	public void ClickLMM()
	{
		GlobalManager.Instance.ChangeCurGame("LobotomyCorp");
		Close();
	}
	public void Start()
	{
	}
	public void Open()
	{
		UnityEngine.Debug.Log("OPEN");
		this.gameObject.SetActive(true);
		if (!Init)
		{
			foreach (var tp in ThirdPartys)
			{
				tp.Init();
			}
			Init = true;
		}
	}
	public void Close()
	{
		UnityEngine.Debug.Log("CLOSE");
		this.gameObject.SetActive(false);
	}
	public void Update()
	{
		foreach (var tp in ThirdPartys)
		{
			tp.Update();
		}
	}
	public bool Init = false;
	[SerializeField]
	public List<ThirdPartyInfo> ThirdPartys;

	public static string ThirdPartSaveRoot = "ThirdParty/";
}
public enum TPState
{
	Checking,
	NeedDownload,
	NeedUpdate,
	Lastest,
	Error
}
[Serializable]
public class ThirdPartyInfo
{
	public string TPName;
	public string CreatorName;
	public string ExcutionPath;
	public Button btn;
	public TextMeshProUGUI BtnText;
	public long curversion;
	public TPState curstate;
	public bool Checking;
	public bool Downloading;
	public GitHubRelease TPInfo;
	public void Init()
	{
		curstate = TPState.Checking;
		Checking = true;
		Downloading = false;
		btn.onClick = new Button.ButtonClickedEvent();
		btn.onClick.AddListener(ClickBtn);
		curversion = SaveManager.Instance.Load<long>(GetPath(Indexer) +".txt");
		InitCheck();
	}
	public void Update()
	{
		if (Downloading)
		{
			goto Skip2;
		}
		if (Checking)
		{
			curstate = TPState.Checking;
			goto Skip;
		}
		if (TPInfo == null)
		{
			curstate = TPState.Error;
			goto Skip;
		}
		if (curversion == default(long))
		{
			curstate = TPState.NeedDownload;
			goto Skip;
		}
		if (curversion < TPInfo.id)
		{
			curstate = TPState.NeedUpdate;
			goto Skip;
		}
		curstate = TPState.Lastest;

	Skip:

		switch (curstate)
		{
			case TPState.Checking:
				BtnText.text = LocalizeManager.Instance.GetText("ThirdPartyView_TPState_Checking");
				break;
			case TPState.NeedDownload:
				BtnText.text = LocalizeManager.Instance.GetText("ThirdPartyView_TPState_NeedDownload");
				break;
			case TPState.NeedUpdate:
				BtnText.text = LocalizeManager.Instance.GetText("ThirdPartyView_TPState_NeedUpdate");
				break;
			case TPState.Lastest:
				BtnText.text = LocalizeManager.Instance.GetText("ThirdPartyView_TPState_Lastest");
				break;
			case TPState.Error:
				BtnText.text = LocalizeManager.Instance.GetText("ThirdPartyView_TPState_Error"); 
				break;
			default:
				break;
		}
	Skip2:
		return;
	}
	public void ClickBtn()
	{
		if (curstate == TPState.Error) return;
		if (Checking || Downloading) return;
		if (curstate == TPState.NeedDownload || curstate == TPState.NeedUpdate)
		{ 
			StartDownload(); 
			return; 
		}
		if (curstate == TPState.Lastest)
		{
			LaunchTP();
		}
	}
	public void LaunchTP()
	{
		Process.Start(GetTPExcutionPath);
	}
	public async void InitCheck()
	{
		var result = await GitHubApiManager.Instance.GetLastestRelease(CreatorName, TPName);
		TPInfo = result;
		Checking = false;
	}
	public void StartDownload()
	{
		Downloading = true;
		DownloadManager.Instance.FileDownLoad(TPInfo.assets[0].browser_download_url, GetTPZipPath, DownloadFileCompleted, DownloadProgressChanged);
	}
	private void DownloadFileCompleted()
	{
		var filepath = GetTPZipPath;
		var folderpath = GetTPFolderPath_TMP;
		try
		{
			UnityEngine.Debug.Log("filepath : " + filepath);
			UnityEngine.Debug.Log("folderpath : " + folderpath);
			GlobalManager.ExtractZipFile(filepath, folderpath);
			ExtensionUtil.DirMove(folderpath, GetTPFolderPath);
			//GlobalManager.ExtractZipFile(filepath, folderpath);

			
			DirectoryInfo dir = new DirectoryInfo(GetTPFolderPath);
			
			DirectoryInfo Finaldir = ExtensionUtil.DoubleFolderChecking(dir);
			if (Finaldir.FullName != dir.FullName)
			{
				ExtensionUtil.DirMove(Finaldir.FullName, dir.FullName + "_LMMtmp");
				Directory.Delete(dir.FullName);
				ExtensionUtil.DirMove(dir.FullName + "_LMMtmp", dir.FullName);
			}
			//File.Delete(filepath);
			Downloading = false;
			curversion = TPInfo.id;
			SaveManager.Instance.Save(GetPath(Indexer)+".txt", curversion);
			if (Directory.Exists(LocalSmallPath))
			{
				Directory.Delete(LocalSmallPath, true);
			}
		}
		catch (Exception e)
		{
			UnityEngine.Debug.Log(e.Message + Environment.NewLine+e.StackTrace);
			//File.Delete(filepath);
		}
	}
	private void DownloadProgressChanged(float e)
	{
		BtnText.text = Mathf.Round(e * 100).ToString() + "%";
	}
	public string GetTPExcutionPath
	{
		get
		{
			return Path.Combine(GetTPFolderPath, ExcutionPath);
		}
	}
	public string GetTPFolderPath_TMP
	{
		get
		{
			return Path.Combine(LocalSmallPath, GetPath(Indexer));
		}
	}
	public string GetTPFolderPath
	{
		get
		{
			return Path.Combine(Application.persistentDataPath, GetPath(Indexer));
		}
	}
	public string GetTPZipPath
	{
		get
		{
			return Path.Combine(LocalSmallPath, GetPath(Indexer)+".zip");
		}
	}
	//public string LocalSmallPath = @"C:\LMMT_TP";
	public const string LocalSmallPath = @"C:\LMMT_TP";
	public string Indexer
	{
		get
		{
			return CreatorName + "_" + TPName;
		}
	}
	public static string GetPath(string filename)
	{
		return ThirdPartyViewController.ThirdPartSaveRoot + filename;
	}
}