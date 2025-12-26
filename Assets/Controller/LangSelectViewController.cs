using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LangSelectViewController : SingletonBehavior<LangSelectViewController>
{
	public void SetChinese()
	{
		ChangeLang("cn");
	}
	public void SetKorean()
	{
		ChangeLang("kr");
	}
	public void SetEnglish()
	{
		ChangeLang("en");
	}
	public void SetRussian()
	{
		ChangeLang("ru");
	}
	public void SetJapan()
	{
		ChangeLang("jp");
	}
	public void ChangeLang(string lang)
	{
		LocalizeManager.Instance.Init(lang);
		UITextDataLoader.AllChange();
		UIModPanelController.Instance.LangChange();
		this.gameObject.SetActive(false);
	}
}