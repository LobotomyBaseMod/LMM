using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GitHubModCommentView : SingletonBehavior<GitHubModCommentView>
{
	public void Active(GitHubModInfo_Upload info)
	{
		this.gameObject.SetActive(true);
		Init(info);
	}
	public async void Init(GitHubModInfo_Upload info)
	{
		CloseCommentEdit();
		ClearCommentList();
		CommentScrollBar.value = 1;
		StartState = await GitHubApiManager.Instance.IsStarred(info.owner, info.modname, DeveloperViewController.Instance.Auth);
		Debug.Log($"Starred State : {StartState}");
		if (DeveloperViewController.Instance.ownerinfo == null)
		{
			nicknameText.text = "NULL";
			CommentWriteBlind.SetActive(true);
			StarToggle.gameObject.SetActive(false);
		}
		else
		{
			nicknameText.text = DeveloperViewController.Instance.ownerinfo.login;
			CommentWriteBlind.SetActive(false);
			StarToggle.gameObject.SetActive(true);
			StarToggle.image.sprite = StartState ? star_fill : star;
		}

		
		curmodinfo = info;
		curissueinfo = await GitHubApiManager.Instance.GetCommentIssue(info.owner, info.modname);
		curpage = 1;
		maxpage = curissueinfo.comments < 20 ? 1 : curissueinfo.comments % 20 == 0 ? curissueinfo.comments / 20 : curissueinfo.comments / 20 + 1;
		UpdateComment();

	}
	public void ClearCommentList()
	{
		foreach (var cc in commentList)
		{
			UnityEngine.Object.Destroy(cc.gameObject);
		}
		commentList.Clear();
	}
	public void ClickPrev()
	{
		if (curpage > 1) curpage -= 1;
		UpdateComment();
	}
	public void ClickNext()
	{
		if (curpage < maxpage) curpage += 1;
		UpdateComment();
	}
	public async void UpdateComment()
	{
		GitHubModCommentView.Instance.CloseCommentEdit();
		ClearCommentList();

		Prev.gameObject.SetActive(true);
		Next.gameObject.SetActive(true);
		PageText.text = curpage + " / " + maxpage;
		if (curpage == 1)
		{
			Prev.gameObject.SetActive(false);
		}
		if (curpage >= maxpage)
		{
			Next.gameObject.SetActive(false);
		}
		var list = await GitHubApiManager.Instance.GetComments(curmodinfo.owner, curmodinfo.modname, curissueinfo.number, curpage);
		foreach (var l in list)
		{
			var c = UnityEngine.Object.Instantiate(commentPrefab, commentsRoot);
			c.SetData(l);
			c.gameObject.SetActive(true);
			commentList.Add(c);
		}
	}
	public async void UploadComment()
	{
		var key = DeveloperViewController.Instance.Auth;
		if (key == string.Empty) return;
		var s = commentText.text;
		var result = await GitHubApiManager.Instance.UploadComment(curmodinfo.owner, curmodinfo.modname, curissueinfo.number, s, key);
		commentText.text = string.Empty;
		UpdateComment();
	}
	public async void ClickStar()
	{
		var result = await GitHubApiManager.Instance.Starred(curmodinfo.owner, curmodinfo.modname, DeveloperViewController.Instance.Auth, !StartState);

		StartState = await GitHubApiManager.Instance.IsStarred(curmodinfo.owner, curmodinfo.modname, DeveloperViewController.Instance.Auth);
		Debug.Log($"Starred State : {StartState}");
		StarToggle.image.sprite = StartState ? star_fill : star;
	}
	public void OpenCommentEdit(GitHubModCommentContainer requester)
	{
		EditView.gameObject.SetActive(true);
		EditView.gameObject.transform.SetParent(requester.transform);
		EditView.SetData(requester);
	}
	public void CloseCommentEdit()
	{
		EditView.gameObject.SetActive(false);
		EditView.gameObject.transform.SetParent(this.transform);
	}
	public void Update()
	{
		LayoutRebuilder.MarkLayoutForRebuild(CommentListViewFitter.transform as RectTransform);
	}
	public int maxpage = 1;

	public int curpage = 1;

	public GitHubIssue curissueinfo;
	public GitHubModInfo_Upload curmodinfo;

	public GitHubModCommentContainer commentPrefab;
	public Transform commentsRoot;

	public GitHubModCommentEdit EditView;

	public List<GitHubModCommentContainer> commentList = new List<GitHubModCommentContainer>();

	public TMP_InputField commentText;
	public TextMeshProUGUI nicknameText;

	public GameObject CommentWriteBlind;

	public bool StartState;

	public Button StarToggle;
	public Sprite star;
	public Sprite star_fill;

	public ContentSizeFitter CommentListViewFitter;

	public Button Prev;
	public Button Next;
	public TextMeshProUGUI PageText;

	public Scrollbar CommentScrollBar;
}