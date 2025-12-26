using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.APIUtil;
using TMPro;
using System.Threading;
using System.Threading.Tasks;

public class UIGitHubOwnerModListController : SingletonBehavior<UIGitHubOwnerModListController>
{
    void Start()
    {
        prefab.gameObject.SetActive(false);
    }

    void Update()
    {
        
    }
	public void Init()
	{
        TMP_owner.text = LocalizeManager.Instance.GetText("DevView_AuthOwner") + DeveloperViewController.Instance.ownerinfo.login;
        //UIGitHubModInfoView.Instance.Close();
        Prev.gameObject.SetActive(false);
        Next.gameObject.SetActive(false);
        pagetext.gameObject.SetActive(false);
        //keywordinput.text = string.Empty;
        curkeyword = $"user:{DeveloperViewController.Instance.ownerinfo.login}";
        curpage = 0;
        DelMod = false;
        ListUpdate();
    }
    public void ClicDelMod()
    {
        DelMod = !DelMod;
    }
    /*
    public void ClickSearch()
    {
        if (keywordinput.text == null || keywordinput.text == string.Empty) return;
        curkeyword = keywordinput.text;
        curpage = 0;
        ListUpdate();
    }*/
    /*
    public void ClickGoto()
    {
        if (movepageinput.text == null || movepageinput.text == string.Empty) return;
        int result;
        if (!int.TryParse(movepageinput.text, out result)) return;
        if (result < 1 || result > totalpage + 1) return;
        curpage = result-1;
        ListUpdate();
    }*/
    public void ClickPrev()
    {
        if (curpage > 0) curpage -= 1;
        ListUpdate();
    }
    public void ClickNext()
    {
        if (curpage < totalpage) curpage += 1;
        ListUpdate();
    }
    public void ListUpdate()
    {
        foreach (UIGitHubModContainer panel in panels)
        {
            panel.gameObject.SetActive(false);
        }
        LoadModList(curpage);
    }

    public async Task<int> GetModPageCount()
    {
        var result = await GitHubApiManager.Instance.SearchRepo(curkeyword,new List<string>(),1);
        return result.total_count / 20 + result.total_count % 20 == 0 ? 0 : 1;
    }
    public async void LoadModList(int page, int num = 20)
    {
        totalpage = await GetModPageCount();
        Debug.Log("1");
        SetPageText();
        Debug.Log("2");
        GitHubRepoSearchResult result = await GitHubApiManager.Instance.SearchRepo(curkeyword, new List<string>(), page+1,num);
        var result2 = await GitHubApiManager.Instance.Graphql_GetFileEachRepo(result.items, "Data.txt");

        Debug.Log("3");
        int index = 0;
        for (int i = 0; i < panels.Count; i++)
        {
            if (index < result.items.Count)
            {
                panels[i].SetData(result2[index], result.items[index], panels[i].OnClick_ModUpdate);
                index++;
            }
        }
        Debug.Log("4");
        if (index < result2.Count)
        {
            for (int i = index; i < result2.Count; i++)
            {
                var ui = UnityEngine.Object.Instantiate(prefab, prefab.gameObject.transform.parent);
                ui.SetData(result2[i], result.items[i], ui.OnClick_ModUpdate);
                panels.Add(ui);
            }
        }
        Debug.Log("5");
    }
    public void SetPageText()
    {
        Debug.Log("1_1");
        pagetext.gameObject.SetActive(true);
        Prev.SetActive(true);
        Next.SetActive(true);
        pagetext.text = (curpage + 1).ToString() + "/" + totalpage.ToString();
        if (curpage == 0) Prev.SetActive(false);
        if (curpage+1 >= totalpage) Next.SetActive(false);
        Debug.Log("1_2");
    }

    public string CurKeyword
    {
        get {
            if (curkeyword == null || curkeyword == string.Empty) return null;
            return curkeyword;
        }
    }
    private string curkeyword = null;

    //public TMP_InputField movepageinput;

    //public TMP_InputField keywordinput;

    public int curpage;

	public List<UIGitHubModContainer> panels;
    public int totalpage;
    public int modcount;

    public GameObject Prev;
    public GameObject Next;

    public TextMeshProUGUI pagetext;

    public UIGitHubModContainer prefab;

    public bool DelMod;

    public TextMeshProUGUI TMP_owner;
}
