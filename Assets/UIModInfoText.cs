using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIModInfoText : SingletonBehavior<UIModInfoText>
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetText(string desc)
    {
        text.text = desc;
    }
    public TextMeshProUGUI text;
}
