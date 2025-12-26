using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UITextDataLoader : MonoBehaviour
{
	public void Start()
	{
		Init();
	}
	public void Init()
	{
		text = this.gameObject.GetComponent<TextMeshProUGUI>();
		if (text == null) return;
		if (instances.Contains(this)) return;
		instances.Add(this);
	}
	public void Awake()
	{
		Init();
		Change();
	}
	public void OnEnable()
	{
		Change();
	}
	public static void AllChange()
	{
		foreach (UITextDataLoader loader in instances)
		{
			loader.Change();
		}
	}
	public void Change()
	{
		if (id == null || id == string.Empty) return;
		string str = LocalizeManager.Instance.GetText(id);
		if (str == null) return;
		text.text = str;
	}
	public void OnDestroy()
	{
		if (instances.Contains(this))
		{
			instances.Remove(this);
		}
	}
	public static List<UITextDataLoader> instances = new List<UITextDataLoader>();
	public string id = string.Empty;
	public TextMeshProUGUI text;
}