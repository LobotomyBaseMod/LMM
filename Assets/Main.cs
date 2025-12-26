using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


	public class Singleton<T> where T : class, new()
	{
		public static T Instance
		{
			get
			{
				bool flag = Singleton<T>._instance == null;
				if (flag)
				{
					Singleton<T>._instance = Activator.CreateInstance<T>();
				}
				return Singleton<T>._instance;
			}
		}


		protected Singleton()
		{
		}


		private static T _instance;
	}
public class SingletonBehavior<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance
	{
		get
		{
			if (SingletonBehavior<T>._instance == null)
			{
				SingletonBehavior<T>._instance = UnityEngine.Object.FindObjectOfType<T>(true);
			}
			return SingletonBehavior<T>._instance;
		}
		set
		{
			_instance = value;
		}
	}

	public virtual void OnApplicationQuit()
	{
		SingletonBehavior<T>._isQuit = true;
		base.StopAllCoroutines();
	}

	private static T _instance;

	private static bool _isQuit;
}
