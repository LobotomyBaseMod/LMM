using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Assets.APIUtil;
using System.Web;
using System.Net;
using System.ComponentModel;
using System;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class DownloadManager :Singleton<DownloadManager>
{
    public async void FileDownLoad_NoFile<T>(string url, DownloadComplete_NoFile<T> completeEvent, DownloadProgress progress, bool CheckFail = false)
    {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                // 비동기로 데이터 가져오기
                var asyncOperation = webRequest.SendWebRequest();

                // 비동기 작업 완료까지 대기
                while (!asyncOperation.isDone)
                {
                    if (progress != null)
                    {
                        progress(asyncOperation.progress);
                    }
                    await Task.Delay(100);
                }


                // 에러 체크
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to fetch data. Error: {webRequest.error}");
                if (CheckFail)
                {
                    completeEvent(default(T));
                }
                    return;
                }
                else
                {

                    if (typeof(T) == typeof(byte[]))
                    {

                        var data = webRequest.downloadHandler.data;
                        completeEvent((T)(object)data);
                    }
                    if (typeof(T) == typeof(string))
                    {
                        var data = webRequest.downloadHandler.text;
                        completeEvent((T)(object)data);
                    }

                }
            }

    }
	public async void FileDownLoad(string url, string savepath, DownloadComplete completeEvent, DownloadProgress progress)
	{
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // 비동기로 데이터 가져오기
            var asyncOperation = webRequest.SendWebRequest();

            // 비동기 작업 완료까지 대기
            while (!asyncOperation.isDone)
            {
                if (progress != null)
                {
                    progress(asyncOperation.progress);
                }
                await Task.Delay(100);
            }


            // 에러 체크
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch data. Error: {webRequest.error}");
                return;
            }
            else
            {
                var data = webRequest.downloadHandler.data;
                var f = new FileInfo(savepath);
                if (!Directory.Exists(f.Directory.FullName)) Directory.CreateDirectory(f.Directory.FullName);
                File.WriteAllBytes(savepath, data);

                completeEvent();
            }
        }
        /*using (WebClient webClient = new WebClient())
        {
            webClient.DownloadFileCompleted += completeEvent;
            webClient.DownloadProgressChanged += progressEvent;

            webClient.DownloadFileAsync(new Uri(url), savepath);
        }*/
    }
    public delegate void DownloadComplete_NoFile<T>(T data);
    public delegate void DownloadComplete();
    public delegate void DownloadProgress(float progress);
}
