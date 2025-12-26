using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Assets.APIUtil;

public class TestScript : MonoBehaviour
{
    async void Start()
    {
        text.text = "Non Update";
        //NexusModInfo mod = await APIManager.GetModInfoAsync(1);
        //text.text = mod.name;
    }
    void Update()
    {
        
    }
    public TextMeshProUGUI text;
}
