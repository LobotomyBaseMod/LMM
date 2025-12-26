using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ModUploadView_DependancyContainer : MonoBehaviour
{
	public void Init()
	{
		foreach (var depend in dependancyList.ToArray())
		{
			DeleteDependancy(depend);
		}
		dependancyList.Clear();
	}
	public void ClickAddDependancy()
	{
		AddDependancy(string.Empty);
	}
	public void AddDependancy(string data)
	{
		var p = UnityEngine.Object.Instantiate(prefab);
		p.transform.SetParent(root);
		p.Init(this);
		p.input.text = data;
		p.gameObject.SetActive(true);
		dependancyList.Add(p);
	}
	public void DeleteDependancy(ModUploadView_DependancyContainer_Prefab target)
	{
		if (dependancyList.Contains(target))
		{
			dependancyList.Remove(target);
			UnityEngine.Object.Destroy(target.gameObject);
		}
	}
	public List<string> GetDependancy()
	{
		var list = new List<string>();
		foreach (var p in dependancyList)
		{
			string s = p.input.text;
			if (s == null || s == string.Empty || list.Contains(s)) continue;
			list.Add(s);
		}
		return list;
	}
	public List<ModUploadView_DependancyContainer_Prefab> dependancyList = new List<ModUploadView_DependancyContainer_Prefab>();
	public Transform root;
	public ModUploadView_DependancyContainer_Prefab prefab;
}