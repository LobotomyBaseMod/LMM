using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.Networking;

public class NexusPageManager : SingletonBehavior<NexusPageManager>
{
	public void Start()
	{
        CookieDic = new Dictionary<string, string>();
        var dic = SaveManager.Instance.Load<Dictionary<string, string>>("cookies.json");
        if (dic != null) CookieDic = dic;
    }
	public string GetCookie(string key)
    {
        if (CookieDic.ContainsKey(key)) return CookieDic[key];
        return null;

    }
    public void SetCookie(string key, string value)
    {
        CookieDic[key] = value;
        SaveManager.Instance.Save("cookies.json", CookieDic);
    }
    public async Task<NexusModDownloadInfo> GetDownloadLinkByModid(int modid)
    {
        int fileid = await GetLastestFileid(modid);
        return await GetDownloadLinkByFileid(fileid);
    }
    public async Task<NexusModDownloadInfo> GetDownloadLinkByFileid(int fileid,bool IsManager = false)
    {
        try
        {
            Debug.Log("FileId : " + fileid);
            string link = string.Format("https://www.nexusmods.com/Core/Libs/Common/Managers/Downloads?GenerateDownloadUrl&fid={0}&game_id=2861", fileid);
            if (IsManager) link = string.Format("https://www.nexusmods.com/Core/Libs/Common/Managers/Downloads?GenerateDownloadUrl&fid={0}&game_id=2295", fileid);
            HTTPResult result = await SendRequestWithCookie(link, true);
            Debug.Log(result.Content);
            NexusModDownloadInfo info = JsonUtility.FromJson<NexusModDownloadInfo>(result.Content);
            info.fileid = fileid;
            return info;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message + Environment.NewLine + e.StackTrace);
            return null;
        }
    }
    public async Task<int> ModPageCount(string keyword = null)
    {
        string html = await LoadPage(1, keyword);
        return GetModPageCount(html);
    }
    public async Task<List<NexusModInfoV2>> LoadModInfoPage(int page,string keyword = null)
    {
        string html = await LoadPage(page, keyword);
        return LoadModInfosInPage(html);
    }
    public async Task<string> LoadPage(int page, string keyword = "")
    {
        string testlink = "https://api-router.nexusmods.com/graphql";

        string query = GraphQLUtil.GetNexusRequestQuery(keyword, page);
        using (UnityWebRequest webRequest = UnityWebRequest.Post(testlink,query, "application/json"))
        {
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

            var result = webRequest.downloadHandler.text;

            return result;
        }
        return null;
            using (HttpClient c = new HttpClient())
        {
            //c.DefaultRequestHeaders.Add("x-graphql-operationname", "GameModsListing");
            byte[] bytes = new UTF8Encoding().GetBytes(query);
            ByteArrayContent content = new ByteArrayContent(bytes);
            content.Headers.Add("Content-Type", "application/json");
            var r = await c.PostAsync(testlink, content);

            if (r.IsSuccessStatusCode)
            {
                var rr = await r.Content.ReadAsStringAsync();

                UnityEngine.Debug.Log(rr);

                return rr;
            }
            return string.Empty;
        }
        /*
        string link = "https://www.nexusmods.com/games/lobotomycorporation/mods?RH_ModList=nav:true,home:false,advfilt:true,include_adult:true,show_game_filter:false,sort_by:date" + ",page:" + page;
        if (keyword != null)
        {
            link = String.Format("https://www.nexusmods.com/games/lobotomycorporation/mods?RH_ModList=nav:true,home:false,advfilt:true,include_adult:true,show_game_filter:false,sort_by:date,page:{0},search%5Bdescription%5D:{1}", page, keyword);
        }
        HTTPResult result0 = await SendRequestWithCookie(link, true);
        return result0.Content;
        */
    }
    public async Task<int> GetLastestFileid(int modid)
    {
        string link = "https://www.nexusmods.com/lobotomycorporation/mods/" + modid + "?tab=files";
        HTTPResult result = await SendRequestWithCookie(link, true);
        return GetLastestFileidInPage(result.Content,modid);
    }
    public List<NexusModInfoV2> LoadModInfosInPage(string page)
    {
        NexusGraphQLRequestInfo info = JsonConvert.DeserializeObject<NexusGraphQLRequestInfo>(page);
        List<NexusGraphQLRequestInfo.Mods.ModInfo> mods = info.data.mods.nodes;
        var list = new List<NexusModInfoV2>();

        foreach (var modinfo in mods)
        {
            NexusModInfoV2 ninfo = new NexusModInfoV2();
            ninfo.modid = modinfo.modId;
            ninfo.mod_name = modinfo.name;
            ninfo.mod_pic = modinfo.thumbnailUrl;
            ninfo.mod_summary = modinfo.summary;

            list.Add(ninfo);
        }

        /*
        HtmlDocument htmlDocument0 = new HtmlDocument();
        htmlDocument0.LoadHtml(page);
        HtmlNode parent = htmlDocument0.DocumentNode.SelectSingleNode($"//*[contains(@class, 'tiles ')]");
        //HtmlNodeCollection nodes = htmlDocument0.DocumentNode.SelectNodes($"//*[contains(@class, '{targetclass0}')]");
        //Console.WriteLine(parent.WriteTo());
        var list = new List<NexusModInfoV2>();
        foreach (HtmlNode node in parent.SelectNodes("li[@class='mod-tile']"))
        {
            NexusModInfoV2 info = new NexusModInfoV2();
            //Console.WriteLine(node.WriteTo());
            HtmlNode mod_image_node = node.SelectSingleNode("div[@class='mod-tile-left ']").SelectSingleNode("a[@class='mod-image']");
            info.mod_link = mod_image_node.GetAttributeValue("href", "error");

            HtmlNode mod_pic_node = node.SelectSingleNode("div[@class='mod-tile-left ']/a[@class='mod-image']/figure/div[@class='fore_div_mods']/img");
            info.mod_pic = mod_pic_node.GetAttributeValue("src", "error");

            HtmlNode mod_name_node = node.SelectSingleNode("div[@class='mod-tile-right']/div[@class='tile-desc']/div[@class='tile-content']/p[@class='tile-name']/a");
            info.mod_name = mod_name_node.InnerText;

            HtmlNode mod_desc_node = node.SelectSingleNode("div[@class='mod-tile-right']/div[@class='tile-desc']/div[@class='tile-content']/p[@class='desc']");
            info.mod_summary = mod_desc_node.InnerText;

            HtmlNode mod_id_node = node.SelectSingleNode("div[@data-game-id='2861']");
            info.modid = int.Parse(mod_id_node.GetAttributeValue("data-mod-id", "-1"));

            list.Add(info);
        }
        */
        return list;
    }
    public int GetModPageCount(string page)
    {
        NexusGraphQLRequestInfo info = JsonConvert.DeserializeObject<NexusGraphQLRequestInfo>(page);

        int num = info.data.mods.totalCount;
        int pagenum = num / 20 + (num % 20 == 0 ? 0 : 1);

        return pagenum;

        /*
        HtmlDocument htmlDocument0 = new HtmlDocument();
        htmlDocument0.LoadHtml(page);
        File.WriteAllText("C:\\Users\\kj022\\LobotomyModManager\\DebugPage.html", page);
        var parent = htmlDocument0.DocumentNode.SelectSingleNode($"//*[@data-e2eid='result-count']");
        Console.WriteLine(parent.InnerText);
        string[] array = parent.InnerText.Split(" ");
        //int num = int.Parse(parent.InnerText.Replace("Found ", "").Replace(" results.", ""));
        int num = int.Parse(array[0]);
        Console.WriteLine(num);
        int pagenum = num / 20 +( num % 20 == 0 ? 0 : 1);

        */
        /*
        var parent = htmlDocument0.DocumentNode.SelectSingleNode($"//*[contains(@class, 'pagination clearfix')]");
        int last = 0;
        foreach (HtmlNode node in parent.SelectNodes("ul[@class='clearfix']/li[@class='extra']"))
        {
            int num = int.Parse(node.SelectSingleNode("a").InnerText);
            if (num > last) last = num;
        }*/
    }
    public int GetLastestFileidInPage(string page,int modid)
    {
        HtmlDocument htmlDocument0 = new HtmlDocument();
        htmlDocument0.LoadHtml(page);
        var parent = htmlDocument0.DocumentNode.SelectNodes($"//*[contains(@class, 'btn inline-flex')]");
        int last = 0;
        foreach (var node in parent)
        {
            try
            {
                string value = node.GetAttributeValue("href", "error");
                if (value == "error") continue;
                if (value.Contains("Widgets"))
                {
                    value = value.Replace("/Core/Libs/Common/Widgets/ModRequirementsPopUp?id=", "");
                    value = value.Replace("&game_id=2861", "");
                    Debug.Log(value);
                    int id = int.Parse(value);
                    if (id > last) last = id;
                }
                else
                {
                    value = value.Replace("https://www.nexusmods.com/lobotomycorporation/mods/" + modid + "?tab=files&file_id=", "");
                    Debug.Log(value);
                    int id = int.Parse(value);
                    if (id > last) last = id;
                }
            }
            catch (Exception e)
            {
                continue;
            }
        }
        return last;
    }
    public async Task<List<ModFileInfo>> GetFileInfoList(int modid)
    {
        string link = "https://www.nexusmods.com/lobotomycorporation/mods/" + modid + "?tab=files";
        HTTPResult result = await SendRequestWithCookie(link, true);
        return GetFileInfoList(result.Content);
    }
    public async Task<ModFileInfo> GetLastestUpdateInfo()
    {
        try
        {
            string link = "https://www.nexusmods.com/site/mods/765?tab=files";
            HTTPResult result = await SendRequestWithCookie(link, true);
            var list = GetFileInfoList(result.Content);
            list.RemoveAll(x => !x.title.Contains("Update"));
            Comparison<ModFileInfo> c = delegate (ModFileInfo a, ModFileInfo b) { return ExtensionUtil.CompareVersions(a.version, b.version); };
            list.Sort(c);
            return list[list.Count-1];
        }
        catch (Exception e)
        {
            Debug.Log(e.Message + Environment.NewLine + e.StackTrace);
            return null;
        }
    }
    public List<ModFileInfo> GetFileInfoList(string page)
    {
        HtmlDocument htmlDocument0 = new HtmlDocument();
        htmlDocument0.LoadHtml(page);
        var parent = htmlDocument0.DocumentNode.SelectNodes("//dt[@data-size]");
        List<ModFileInfo> list = new List<ModFileInfo>();
        foreach (var nodes in parent)
        {
            ModFileInfo info = new ModFileInfo();
            info.id = int.Parse(nodes.GetAttributeValue("data-id", "-1"));
            var sub = nodes.SelectSingleNode("div");
            info.title = sub.SelectSingleNode("p").InnerText;
            var ssub = sub.SelectSingleNode("div[@class='file-download-stats clearfix']/ul");
            info.version = ssub.SelectSingleNode("li[@class='stat-version']/div/div[@class='stat']").InnerText;
            list.Add(info);
        }
        return list;
    }
    public bool Delay;
    public async Task<HTTPResult> SendRequestWithCookie(string url, bool IsGet, Dictionary<string, string> postdata = null)
    {
        D:
        if (Delay)
        {
           await Task.Delay(100);
            goto D;
        }
        Delay = true;
        await Task.Delay(1000);
        Delay = false;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            string cookie = "";
            foreach (var pair in CookieDic)
            {
                cookie += pair.Key + "=" + pair.Value + ";";
            }
            Debug.Log(cookie);
            UnityWebRequest.ClearCookieCache();
            webRequest.SetRequestHeader("Cookie", cookie);
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
                HTTPResult result = new HTTPResult();
                var headers = webRequest.GetResponseHeaders();
                Debug.Log("Response Headers: " + headers);
                if (headers.ContainsKey("Set-Cookie"))
                {
                    Dictionary<string, string> cookiedic = new Dictionary<string, string>();
                    string cookieresult = headers["Set-Cookie"];
                    string[] parts = cookieresult.Split(';');
                    foreach (string part in parts)
                    {
                        string trimmedPart = part.Trim();
                        // 이름과 값으로 분리
                        int equalIndex = trimmedPart.IndexOf('=');
                        if (equalIndex != -1)
                        {
                            string cookieName = trimmedPart.Substring(0, equalIndex);
                            string cookieValue = trimmedPart.Substring(equalIndex + 1);
                            
                            cookiedic[cookieName] = cookieValue;
                        }
                    }
                    result.Set_Cookies = cookiedic;
                    foreach (var pair in cookiedic)
                    {
                        SetCookie(pair.Key, pair.Value);
                    }
                }
                // JSON 데이터 읽기
                string jsonData = webRequest.downloadHandler.text;
                result.Content = jsonData;
                return result;
            }
        }

        /*
        using (var handler = new HttpClientHandler())
        {
            var cookieContainer = new CookieContainer();
            handler.CookieContainer = cookieContainer;
            if (CookieDic == null) goto skip;
            foreach (KeyValuePair<string, string> pair in CookieDic)
            {
                cookieContainer.Add(new Uri(url), new Cookie(pair.Key, pair.Value));
            }
        skip:
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("User-Agent", "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 108.0.0.0 Safari / 537.36 Edg / 108.0.1462.46");
            HttpResponseMessage response;
            if (IsGet)
            {
                response = await client.GetAsync(url);
            }
            else
            {
                if (postdata == null) return null;
                var content = new FormUrlEncodedContent(postdata);
                response = await client.PostAsync(url, content);
            }

            if (response.IsSuccessStatusCode)
            {
                var cookies = cookieContainer.GetCookies(new Uri(url));
                Dictionary<string, string> cookieresult = new Dictionary<string, string>();
                foreach (Cookie cookie in cookies)
                {
                    cookieresult[cookie.Name] = cookie.Value;
                }
                string responseHeader = response.Headers.ToString();
                string responseBody = await response.Content.ReadAsStringAsync();
                HTTPResult result = new HTTPResult();
                result.Set_Cookies = cookieresult;
                foreach (var cookie in result.Set_Cookies)
                {
                    SetCookie(cookie.Key, cookie.Value);
                }
                result.Content = responseBody;
                return result;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode} , {response.ReasonPhrase} , {response.RequestMessage}");
                return null;
            }
        }*/
    }
    public Dictionary<string, string> CookieDic = new Dictionary<string, string>();
}

public class NexusGraphQLRequestInfo
{
    public Data data;
    public class Data
    {
        public Mods mods;
    }
    public class Mods
    {
        public FacetsData facetsData;
        public List<ModInfo> nodes;
        public int totalCount;
        public class FacetsData
        {
            public Dictionary<string, string> tag;
            public Dictionary<string, string> languageName;
            public Dictionary<string, string> categoryName;
        }
        public class ModInfo
        {
            public bool adultContent;
            public string createdAt;
            public long downloads;
            public long endorsements;
            public long fileSize;
            public GameInfo game;
            public CategoryInfo modCategory;
            public int modId;
            public string name;
            public string status;
            public string summary;
            public string thumbnailUrl;
            public string uid;
            public string updatedAt;
            

            public class GameInfo
            {
                public string domainName;
                public int id;
                public string name;
            }
            public class CategoryInfo
            {
                public int categoryId;
                public string name;
            }
            public class UploaderInfo
            {
                public string avatar;
                public long memberId;
                public string name;
            }
        }
    }
}
