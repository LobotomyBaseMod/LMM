using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.APIUtil;
using TMPro;
using System.Threading;
using System.Threading.Tasks;

public class UINexusModListController : SingletonBehavior<UINexusModListController>
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
        UINexusModInfoView.Instance.Close();
        Prev.gameObject.SetActive(false);
        Next.gameObject.SetActive(false);
        pagetext.gameObject.SetActive(false);
        keywordinput.text = string.Empty;
        curkeyword = string.Empty;
        curpage = 0;
        ListUpdate();
    }
    public void ClickSearch()
    {
        curkeyword = keywordinput.text == null || keywordinput.text == string.Empty ? string.Empty : keywordinput.text;
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
        foreach (UINexusModPanel panel in panels)
        {
            panel.gameObject.SetActive(false);
        }
        LoadModList(curpage);
    }
    public async Task<int> GetModPageCount()
    {
        int count = await NexusPageManager.Instance.ModPageCount(curkeyword);
        return count;
    }
    public async void LoadModList(int page, int num = 20)
    {
        totalpage = await GetModPageCount();
        Debug.Log("1");
        SetPageText();
        Debug.Log("2");
        List<NexusModInfoV2> mod = await NexusPageManager.Instance.LoadModInfoPage(page+1, curkeyword);
        Debug.Log("3");
        int index = 0;
        for (int i = 0; i < panels.Count; i++)
        {
            if (index < mod.Count)
            {
                panels[i].SetData(mod[index]);
                index++;
            }
        }
        Debug.Log("4");
        if (index < mod.Count)
        {
            for (int i = index; i < mod.Count; i++)
            {
                UINexusModPanel ui = UnityEngine.Object.Instantiate(prefab, prefab.gameObject.transform.parent);
                ui.SetData(mod[i]);
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

	public List<UINexusModPanel> panels;
    public int totalpage;
    public int modcount;

    public GameObject Prev;
    public GameObject Next;

    public TextMeshProUGUI pagetext;

    public UINexusModPanel prefab;
}
