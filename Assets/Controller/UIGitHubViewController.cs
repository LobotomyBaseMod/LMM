using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGitHubViewController : SingletonBehavior<UIGitHubViewController>
{
    // Start is called before the first frame update
    void Start()
    {
        Close();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Init()
    {
        UIGitHubModListController.Instance.Init();
    }
    public void Open()
    {
        this.gameObject.SetActive(true);
        Init();
    }
    public void Close()
    {
        this.gameObject.SetActive(false);
    }
}
