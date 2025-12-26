using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;
using TMPro;
using System.Diagnostics;
public class UIModPanelController : SingletonBehavior<UIModPanelController>, IBeginDragHandler, IDragHandler
{
    void Start()
    {
        panelprefab.gameObject.SetActive(false);
        ModPageBtn.gameObject.SetActive(false);
        ModRemoveBtn.gameObject.SetActive(false);
        ModFolderBtn.gameObject.SetActive(false);
        LocalChangeBtn.gameObject.SetActive(false);
        updatecheckText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if (ConfigViewController.Instance.updateState == true)
        {
            updatecheckText.text = LocalizeManager.Instance.GetText("UI_Text_UpdateCheck_Main");
        }
    }
    public void OnClickRemoveMod()
    {
        if (curPanel == null) return;
        RemoveMod(curPanel.xml);
    }
    public void OnClickAllCheck(bool dummy)
    {
        foreach (var p in panels.ToArray())
        {
            p.toggle.isOn = AllToggle.isOn;
            p.xml.Useit = AllToggle.isOn;
        }
        RefreshPanelList();
    }
    public void OnClickLocalChange()
    {
        if (curPanel == null) return;
        if (!curPanel.info.IsNexus && !curPanel.info.IsGitHub) return;
        curPanel.info.IsGitHub = false;
        curPanel.info.IsNexus = false;
        curPanel.xml.IsNexus = false;
        curPanel.xml.IsGitHub = false;
        RefreshPanelList();
    }
    public void OnClickOpenFolder()
    {
        if (curPanel == null) return;
        Process.Start("explorer.exe", Path.Combine(GlobalManager.Instance.ModListFolderPath, curPanel.info.foldername));
    }
    public void OnClickGoToModPage()
    {
        if (curPanel == null) return;
        if (!curPanel.info.IsNexus && !curPanel.info.IsGitHub) return;
        if (curPanel.info.IsNexus)
        {
            string link = "https://www.nexusmods.com/lobotomycorporation/mods/" + curPanel.xml.modid;
            Application.OpenURL(link);
        }
        if (curPanel.info.IsGitHub)
        {
            string[] array = curPanel.xml.g_modid.Split(' ');
            string link = $"https://github.com/{array[0]}/{array[1]}";
            Application.OpenURL(link);
        }
    }
    public void ClickSearch()
    {
        var keyword = searchTextField.text;
        ClearHighlight();
        var ls = HighLighting(keyword);
        var y = this.layout.transform.localPosition.y;

        foreach (var p in ls)
        {
            int v = panels.IndexOf(p);
            UnityEngine.Debug.Log($"v : {v}");
            UnityEngine.Debug.Log($"v * 50+1: {v * 50 + 1}");
            UnityEngine.Debug.Log($"y: {y}");
            if (y < v * 50)
            {
                this.layout.transform.localPosition = new Vector3(this.layout.transform.localPosition.x, v * 50+1);
                break;
            }
        }

    }
    public void ClearHighlight()
    {
        foreach (var p in panels)
        {
            p.HighLight(false);
        }
    }
    public List<UIModPanel> HighLighting(string keyword)
    {
        List<UIModPanel> result = new List<UIModPanel>();
        foreach (var p in panels)
        {
            if (p.HighLight(true, keyword)) result.Add(p);
        }
        return result;
    }
    public void OnClickDiscord()
    {
        Application.OpenURL("https://discord.gg/5kAdC6H3E3");
    }
    public void SetCurPanel(UIModPanel panel)
    {
        curPanel = panel;
        if (curPanel != null)
        {
            UIModInfoText.Instance.SetText(curPanel.info.modinfo);
            ModPageBtn.gameObject.SetActive(curPanel.xml.IsNexus || curPanel.xml.IsGitHub);
            LocalChangeBtn.gameObject.SetActive(curPanel.xml.IsNexus || curPanel.xml.IsGitHub);
            ModRemoveBtn.gameObject.SetActive(true);
            ModFolderBtn.gameObject.SetActive(true);
            

            var list = curPanel.GetRequireMods();
            foreach (var p in panels)
            {
                p.NeedImage.enabled = false;
                var pl = p.GetRequireMods();
                if (pl != null)
                {
                    if (pl.Contains(curPanel.GetModId()))
                    {
                        p.NeedImage.enabled = true;
                        p.NeedImage.transform.localScale = new Vector3(1, 1);
                    }
                }
            }
            if (list != null)
            {
                foreach (var p in panels)
                {
                    p.NeedImage.enabled = false;
                    if (p == curPanel) continue;
                    if (list.Contains(p.GetModId()))
                    {
                        p.NeedImage.enabled = true;
                        p.NeedImage.transform.localScale = new Vector3(-1, 1);
                        continue;
                    }

                }
            }
        }
        else
        {
            UIModInfoText.Instance.SetText(string.Empty);
            ModPageBtn.gameObject.SetActive(false);
            ModFolderBtn.gameObject.SetActive(false);
            LocalChangeBtn.gameObject.SetActive(false);
            ModRemoveBtn.gameObject.SetActive(false);
        }
    }
    public void LangChange()
    {
        foreach (var panel in panels)
        {
            panel.ModDescInit();
        }
        RefreshPanelList();
    }
	public void Init()
	{
        ResetAllPanel();
        ModListXml xml = GlobalManager.Instance.modlistdata;
        if (xml != null)
        {
            foreach (ModInfoXml info in xml.list)
            {
                if (info.IsWorkShop) continue;
                if (panels.Find(x => x.xml.modfoldername == info.modfoldername) != null) continue;
                UIModPanel panel = UnityEngine.Object.Instantiate(panelprefab, panelprefab.transform.parent);
                panel.SetData(info);
                panels.Add(panel);
            }
        }
	}
    public void RemoveMod(ModInfoXml xml)
    {
        UIModPanel panel = panels.Find(x => x.xml == xml);
        if (panel != null)
        {
            if (Directory.Exists(Path.Combine(GlobalManager.Instance.ModListFolderPath, panel.xml.modfoldername)))
            {
                Directory.Delete(Path.Combine(GlobalManager.Instance.ModListFolderPath, panel.xml.modfoldername), true);
            }
            panel.gameObject.transform.SetParent(null);
            panel.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(panel.gameObject);
            panels.Remove(panel);
        }
        RefreshPanelList();
    }
    public void AddMod(ModInfoXml xml)
    {
        if (xml.IsWorkShop) return;
        UIModPanel panel = UnityEngine.Object.Instantiate(panelprefab, panelprefab.transform.parent);
        panel.SetData(xml);
        panels.Add(panel);
        RefreshPanelList();
    }
    public void RefreshPanelList()
    {
        UnityEngine.Debug.Log($"RefreshPanelList");
        panels.Clear();
        List<ModInfoXml> list = new List<ModInfoXml>();
        for (int i = 0; i < panelprefab.transform.parent.childCount; i++)
        {
            Transform t = panelprefab.transform.parent.GetChild(i);
            UIModPanel ump = t.gameObject.GetComponent<UIModPanel>();
            if (ump == null || ump == panelprefab || !ump.gameObject.activeSelf) continue;
            panels.Add(ump);
            list.Add(ump.xml);
        }
        ModListXml xml = new ModListXml();
        xml.list = list;
        GlobalManager.Instance.SaveNewModListDataList(xml);
        SetCurPanel(null);
    }
    public void ResetAllPanel()
    {
        SetCurPanel(null);
        foreach (var p in panels)
        {
            p.gameObject.transform.SetParent(null);
            p.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(p.gameObject);
        }
        panels.Clear();
    }
	public void OnBeginDrag(PointerEventData eventData)
	{
        eventData.Use();
    }

	public void OnDrag(PointerEventData eventData)
	{
        eventData.Use();
    }

    public UIModPanel curPanel;

    public UIModPanel panelprefab;
    public List<UIModPanel> panels = new List<UIModPanel>();

    public Button ModPageBtn;
    public Button ModRemoveBtn;
    public Button ModFolderBtn;
    public Button LocalChangeBtn;

    public TextMeshProUGUI updatecheckText;
    public TMP_InputField searchTextField;

    public Toggle AllToggle;

    public VerticalLayoutGroup layout;
}
