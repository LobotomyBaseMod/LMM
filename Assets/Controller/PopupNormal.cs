using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PopupNormal : MonoBehaviour
{
	public void SetData(string t, string d, Callback_Normal c, params string[] p)
	{
		title.text = t;
		desc.text = string.Format(d, p);
		callback = c;
	}
	public TextMeshProUGUI title;
	public TextMeshProUGUI desc;
	public Callback_Normal callback;
}