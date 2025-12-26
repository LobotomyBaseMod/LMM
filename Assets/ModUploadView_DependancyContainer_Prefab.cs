using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModUploadView_DependancyContainer_Prefab : MonoBehaviour
{
	public void ClickDelete()
	{
		parent.DeleteDependancy(this);
	}
	public void Init(ModUploadView_DependancyContainer p)
	{
		parent = p;
	}
	public ModUploadView_DependancyContainer parent;
	public TMP_InputField input;
}