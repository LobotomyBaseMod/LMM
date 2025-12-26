using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINexusViewController : SingletonBehavior<UINexusViewController>
{
    // Start is called before the first frame update
    void Start()
    {
        Close();
        UINexusViewController.Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Init()
    {
        UINexusModListController.Instance.Init();
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
