using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public delegate void Callback_Normal();

public class PopupViewController : SingletonBehavior<PopupViewController>
{
	public void PopupNormal(NormalPopupPair pair, Callback_Normal callback = null,params string[] p)
	{
		this.gameObject.SetActive(true);

		var title = msgDic[pair].key;
		var desc = msgDic[pair].value;
		PopupNormal(title, desc, callback,p);
	}
	public void PopupNormal(string titleid, string descid, Callback_Normal callback = null, params string[] p)
	{
		this.gameObject.SetActive(true);

		var title = LocalizeManager.Instance.GetText(titleid);
		var desc = LocalizeManager.Instance.GetText(descid);
		PopupNormal_Text(title, desc, callback,p);
	}
	public void PopupNormal_Text(string title, string desc, Callback_Normal callback = null, params string[] p)
	{
		Init();
		this.gameObject.SetActive(true);

		NormalPopup.gameObject.SetActive(true);

		NormalPopup.SetData(title, desc, callback,p);
	}
	public void PopupChoice(ChoicePopupPair pair, Callback_Choice callback = null, params string[] p)
	{
		this.gameObject.SetActive(true);

		var desc = choicemsgDic[pair];
		PopupChoice(desc, callback,p);
	}
	public void PopupChoice(string descid, Callback_Choice callback = null, params string[] p)
	{
		this.gameObject.SetActive(true);
		var desc = LocalizeManager.Instance.GetText(descid);
		PopupChoice_Text(desc, callback,p);
	}
	public void PopupChoice_Text(string desc, Callback_Choice callback = null, params string[] p)
	{
		Init();
		this.gameObject.SetActive(true);

		ChoicePopup.gameObject.SetActive(true);

		ChoicePopup.SetData(desc, callback,p);
	}
	public void Choice_yes()
	{
		if (ChoicePopup.callback != null)
		{
			if (ChoicePopup.callback(true))
			{
				return;
			}
		}
		Close();
	}
	public void Choice_no()
	{
		if (ChoicePopup.callback != null)
		{
			if (ChoicePopup.callback(false))
			{
				return;
			}
		}
		Close();
	}
	public void Close_Normal()
	{
		if (NormalPopup.callback != null) NormalPopup.callback();

		Close();
	}
	public void Init()
	{
		NormalPopup.gameObject.SetActive(false);
		ChoicePopup.gameObject.SetActive(false);
	}
	public void Close()
	{
		NormalPopup.gameObject.SetActive(false);
		ChoicePopup.gameObject.SetActive(false);
		this.gameObject.SetActive(false);
	}
	public PopupNormal NormalPopup;

	public PopupChoice ChoicePopup;

	public SerializeDictionary<NormalPopupPair, SKeyValuePair<string,string>> msgDic;

	public SerializeDictionary<ChoicePopupPair, string> choicemsgDic;
}
public enum NormalPopupPair
{
	GameStartError,
	ModUploadSuccess,
	ModUploadFail,
	ModDeleteSuccess,
	ModDeleteFail,
	ModUploadCheck_ModNameContainBlank,
	ModUploadCheck_ModFileNull,
	ModUploadCheck_ModDependancy
}
public enum ChoicePopupPair
{
	ModDeleteChoice,
	TPNewVersionDetect,
	ModDependancyDetect
}