using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextSizeFitter : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        text.rectTransform.sizeDelta = new Vector2(text.rectTransform.sizeDelta.x, text.preferredHeight);
    }
    public TextMeshProUGUI text;
}
