using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GitHubModCommentEdit : MonoBehaviour
{
	public void SetData(GitHubModCommentContainer requester)
	{
		curcomment = requester.curdata;
		editText.text = curcomment.body;
		
	}
	public async void ClickEdit()
	{
		var auth = DeveloperViewController.Instance.Auth;
		if (auth == string.Empty)
		{
			return;
		}
		var s = editText.text;
		if (s == string.Empty)
		{
			return;
		}
		var modinfo = GitHubModCommentView.Instance.curmodinfo;
		var issueinfo = GitHubModCommentView.Instance.curissueinfo;

		var result = await GitHubApiManager.Instance.UpdateComment(modinfo.owner, modinfo.modname, issueinfo.number, curcomment.id, s, auth);
		GitHubModCommentView.Instance.UpdateComment();

		return;
	}
	public GitHubComment curcomment;
	public TMP_InputField editText;
}