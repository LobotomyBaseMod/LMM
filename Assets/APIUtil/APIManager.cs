using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Net.Http;
using Newtonsoft.Json;
using System.Web;
using System.Net;
using UnityEngine.Networking;
namespace Assets.APIUtil
{
    public class APIManager
    {
        public static async Task<NexusModDownloadInfo> GetModDownloadLink(int modid)
        {
            string result = await GetHttp(moddownload(modid));
            return JsonUtility.FromJson<NexusModDownloadInfo>(result);
        }
        public static async Task<int> GetModPageCountAsync()
        {
            try
            {
                string result = await GetHttp(modpagecount());
                return int.Parse(result);
            }
            catch (HttpRequestException ex)
            {
                Debug.Log($"HttpRequestException: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.Log($"InnerException: {ex.InnerException.Message}");
                }
                throw ex;
            }
        }
        public static async Task<NexusModInfo[]> GetModPageAsync(int page, int count)
        {
            string result = await GetHttp(modpage(page, count));
            Debug.Log(result);
            return JsonConvert.DeserializeObject<NexusModInfo[]>(result);
        }

        public static async Task<NexusModInfo> GetModInfoAsync(int id)
        {
            string result = await GetHttp(modinfo(id));
            return JsonUtility.FromJson<NexusModInfo>(result);
        }

        private static async Task<string> GetHttp(Uri link)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(link.AbsoluteUri))
            {
                Debug.Log(link.AbsoluteUri);
                // 비동기로 데이터 가져오기
                var asyncOperation = webRequest.SendWebRequest();

                // 비동기 작업 완료까지 대기
                while (!asyncOperation.isDone)
                {
                    await Task.Yield();
                }


                // 에러 체크
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to fetch JSON data. Error: {webRequest.error}");
                    return null;
                }
                else
                {
                    // JSON 데이터 읽기
                    string jsonData = webRequest.downloadHandler.text;
                    Debug.Log($"JSON data: {jsonData}");
                    return jsonData;
                }
            }
        }
        /*public async static Task<string> GetHttp(Uri link)
        {
            using (UnityWebRequest www = new UnityWebRequest())
            using (WebClient client = new WebClient())
            {

                string response = await client.DownloadStringTaskAsync(link);
                return response;
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                else
                {
                    Console.WriteLine("요청이 실패했습니다. 상태 코드: " + response.StatusCode);
                    return string.Empty;
                }
            }
            
        }*/
        public static Uri moddownload(int id)
        {
            return new Uri(baseurl + "/api/mod/"+id.ToString()+"/download");
        }
        public static Uri modpagecount()
        {
            return new Uri(baseurl + "/api/mod/page/count");
        }
        public static Uri modpage(int page, int count)
        {
            return new Uri(baseurl + "/api/mod/page/forward"+count+"/"+page);
        }
        public static Uri modinfo(int id)
        {
            return new Uri(baseurl + "/api/mod/" + id);
        }
        public static string baseurl = "https://lmmapi-jiexf3tvaa-uc.a.run.app";
    }
}
