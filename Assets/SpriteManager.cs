using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SpriteManager : Singleton<SpriteManager>
{
	public void Init()
	{
		string path = Application.dataPath + "/img";
		if (!Directory.Exists(path)) Directory.CreateDirectory(path);
		DirectoryInfo dirinfo = new DirectoryInfo(path);
		foreach (FileInfo info in dirinfo.GetFiles())
		{
			if (Path.GetExtension(info.FullName) == ".png")
			{
				Texture2D texture = new Texture2D(2, 2);
				texture.LoadImage(File.ReadAllBytes(info.FullName));
				Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
				AddSprite(Path.GetFileNameWithoutExtension(info.FullName), sprite);
			}
		}
	}
	public void AddSprite(string name, Sprite sp)
	{
		dic[name] = sp;
	}
	public Sprite GetSprite(string name)
	{
		if (dic.ContainsKey(name)) return dic[name];
		return null;
	}
	public Dictionary<string, Sprite> dic = new Dictionary<string, Sprite>();
}