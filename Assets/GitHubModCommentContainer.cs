using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GitHubModCommentContainer : MonoBehaviour
{
	public void SetData(GitHubComment data)
	{
		curdata = data;
		ProfileDown();
		nameText.text = curdata.user.login;
		descText.text = curdata.body;

		editBtn.gameObject.SetActive(false);
		delBtn.gameObject.SetActive(false);

		var user = DeveloperViewController.Instance.ownerinfo;
		if (user == null)
		{
			goto btnSkip;
		}
		if (user.login == GitHubModCommentView.Instance.curmodinfo.owner)
		{
			delBtn.gameObject.SetActive(true);
		}
		if (user.id == data.user.id)
		{
			delBtn.gameObject.SetActive(true);
			editBtn.gameObject.SetActive(true);
		}

	btnSkip:
		return;
	}
	public void ProfileDown()
	{
		DownloadManager.Instance.FileDownLoad_NoFile<byte[]>(curdata.user.avatar_url, ProfileDownCompelete, null);
	}
	public void ProfileDownCompelete(byte[] data)
	{
		Sprite s = ExtensionUtil.TryCreateSprite(data);
		profile.sprite = s;
	}

	public async void ClickDel()
	{
		var auth = DeveloperViewController.Instance.Auth;
		if (auth == string.Empty)
		{
			return;
		}
		var modinfo = GitHubModCommentView.Instance.curmodinfo;
		var issueinfo = GitHubModCommentView.Instance.curissueinfo;


		var result = await GitHubApiManager.Instance.DeleteComment(modinfo.owner, modinfo.modname, issueinfo.number, curdata.id, auth);

		GitHubModCommentView.Instance.UpdateComment();
	}
	public void ClickEdit()
	{
		GitHubModCommentView.Instance.OpenCommentEdit(this);
	}
	public GitHubComment curdata;

	public TextMeshProUGUI nameText;
	public TextMeshProUGUI descText;
	public Button editBtn;
	public Button delBtn;
	public Image profile;
}