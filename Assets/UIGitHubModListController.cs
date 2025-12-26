using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.APIUtil;
using TMPro;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

public class UIGitHubModListController : SingletonBehavior<UIGitHubModListController>
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
        UIGitHubModInfoView.Instance.Close();
        Prev.gameObject.SetActive(false);
        Next.gameObject.SetActive(false);
        pagetext.gameObject.SetActive(false);
        keywordinput.text = string.Empty;
        curkeyword = string.Empty;
        curpage = 0;
        ListUpdate();
        Toggle_update.isOn = true;
    }
    public void ClickModUpload()
    {
        DeveloperViewController.Instance.Open();
    }
    public void ClickSearch()
    {
        curkeyword = keywordinput.text == null || keywordinput.text == string.Empty ? string.Empty : keywordinput.text;
        curkeyword = keywordinput.text;
        curpage = 0;
        ListUpdate();
    }
    public void ClickGoto()
    {
        if (movepageinput.text == null || movepageinput.text == string.Empty) return;
        int result;
        if (!int.TryParse(movepageinput.text, out result)) return;
        if (result < 1 || result > totalpage + 1) return;
        curpage = result-1;
        ListUpdate();
    }
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
    public void OnToggleSort_Update(bool ison)
    {
        if (!Toggle_update.isOn)
        {
            if (!Toggle_starred.isOn)
            {
                Toggle_update.isOn = true;
                return;
            }
            return;
        }
        var prevstate = state;
        Toggle_starred.isOn = false;
        if (UpDown_update_State)
        {
            state = SortState.Update;
        } else {
            state = SortState.Update_Asc;
        }
        if (prevstate != state)
        {
            ListUpdate();
        }
    }
    public void OnToggleSort_Starred(bool ison)
    {
        
        if (!Toggle_starred.isOn)
            {
            if (!Toggle_update.isOn)
            {
                Toggle_starred.isOn = true;
                return;
            }
            return;
        }
        var prevstate = state;
        Toggle_update.isOn = false;
        if (UpDown_starred_State)
        {
            state = SortState.Starred;
        }
        else
        {
            state = SortState.Starred_Asc;
        }
        if (prevstate != state)
        {
            ListUpdate();
        }
    }
    public void Click_UpDown_Update()
    {
        if (UpDown_update_State)
        {
            UpDown_update_State = false;
            (UpDown_update.transform as RectTransform).rotation = Quaternion.Euler(0, 0, 90);
        }
        else
        {
            UpDown_update_State = true;
            (UpDown_update.transform as RectTransform).rotation = Quaternion.Euler(0, 0, -90);
        }
        OnToggleSort_Update(Toggle_update.isOn);
    }
    public void Click_UpDown_Starred()
    {
        if (UpDown_starred_State)
        {
            UpDown_starred_State = false;
            (UpDown_starred.transform as RectTransform).rotation = Quaternion.Euler(0, 0, 90);
        }
        else
        {
            UpDown_starred_State = true;
            (UpDown_starred.transform as RectTransform).rotation = Quaternion.Euler(0, 0, -90);
        }
        OnToggleSort_Starred(Toggle_starred.isOn);
    }

    public string GetSortKeyword()
    {
        var k = $"{curkeyword} ";
        switch (state)
        {
            case SortState.Update:
                k += "sort:updated";
                break;
            case SortState.Update_Asc:
                k += "sort:updated-asc";
                break;
            case SortState.Starred:
                k += "sort:stars";
                break;
            case SortState.Starred_Asc:
                k += "sort:stars-asc";
                break;
            default:
                break;
        }
        return k;
    }
    public async Task<int> GetModPageCount()
    {
        var result = await GitHubApiManager.Instance.SearchRepo(curkeyword,new List<string>(),1);
        Debug.Log($"total count : {result.total_count}");
        Debug.Log($"page calc : {result.total_count / 20} + {(result.total_count % 20 == 0 ? 0 : 1)}");
        return result.total_count / 20 + (result.total_count % 20 == 0 ? 0 : 1);
    }
    public async void LoadModList(int page, int num = 20)
    {
        totalpage = await GetModPageCount();
        Debug.Log("1");
        SetPageText();
        Debug.Log("2");
        var k = GetSortKeyword();
        GitHubRepoSearchResult result = await GitHubApiManager.Instance.SearchRepo(k, new List<string>(), page+1,num);
        var result2 = await GitHubApiManager.Instance.Graphql_GetFileEachRepo(result.items, "Data.txt");

        Debug.Log("3");
        int index = 0;
        for (int i = 0; i < panels.Count; i++)
        {
            if (index < result.items.Count)
            {
                panels[i].SetData(result2[index], result.items[index], panels[i].OnClick_SearchAndDown);
                index++;
            }
        }
        Debug.Log("4");
        if (index < result2.Count)
        {
            for (int i = index; i < result2.Count; i++)
            {
                var ui = UnityEngine.Object.Instantiate(prefab, prefab.gameObject.transform.parent);
                ui.SetData(result2[i], result.items[i], ui.OnClick_SearchAndDown);
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

    public TMP_InputField movepageinput;

    public TMP_InputField keywordinput;

    public int curpage;

	public List<UIGitHubModContainer> panels;
    public int totalpage;
    public int modcount;

    public GameObject Prev;
    public GameObject Next;

    public TextMeshProUGUI pagetext;

    public Toggle Toggle_update;
    public Button UpDown_update;
    public bool UpDown_update_State = true;
    public Toggle Toggle_starred;
    public Button UpDown_starred;
    public bool UpDown_starred_State = true;

    public SortState state = SortState.Update;

    public UIGitHubModContainer prefab;
}
public enum SortState
{
    Update,
    Update_Asc,
    Starred,
    Starred_Asc
}
