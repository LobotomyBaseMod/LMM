using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SerializeDictionary<Tkey, TValue>
{
	public SerializeDictionary()
	{
		dic = new List<SKeyValuePair<Tkey, TValue>>();
	}
	public bool ContainsKey(Tkey key)
	{
		var value = dic.Find(x => x.key.Equals(key));
		return value != null;
	}
	public Dictionary<Tkey, TValue> ToDic()
	{
		Dictionary<Tkey, TValue> result = new Dictionary<Tkey, TValue>();
		foreach (var pair in dic)
		{
			result[pair.key] = pair.value;
		}
		return result;
	}
	public TValue this[Tkey key]
		{
		get
		{
			var pair = dic.FindAll(x => x.key.Equals(key));
			if (pair.Count == 0) return default(TValue);
			return pair[0].value;
		}
		set
		{
			var pair = dic.FindAll (x=> x.key.Equals(key));
			if (pair.Count > 0)
			{
				pair[0].value = value;
				return;
			}
			dic.Add(new SKeyValuePair<Tkey, TValue>(key, value));
		}
		}
	public List<SKeyValuePair<Tkey, TValue>> dic; 
}
[Serializable]
public class SKeyValuePair<Tkey, TValue>
{
	public SKeyValuePair(Tkey k, TValue v)
	{
		key = k;
		value = v;
	}
	public Tkey key;

	public TValue value;
}