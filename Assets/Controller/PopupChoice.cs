using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public delegate bool Callback_Choice(bool isyes);
public class PopupChoice : MonoBehaviour
{
	public void SetData(string d, Callback_Choice c, params string[] p)
	{
		desc.text = string.Format(d,p);
		callback = c;
	}
	public TextMeshProUGUI desc;
	public Callback_Choice callback;
}