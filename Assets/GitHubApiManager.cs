using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net;
public class GitHubApiManager : Singleton<GitHubApiManager>
{
	private string KEY = string.Empty;
    // public static string defaultTopic = "lmm-lobotomymod-tag-by-abcdcode topic:lmm-lobotomymod-tag-by-abcdcode2";
    public static string V1Topic = "lmm-lobotomymod-tag-by-abcdcode";
    public static string V2Topic = "lmm-lobotomymod-tag-by-abcdcodev2";
    public static string CurTopic = "lmm-lobotomymod-tag-by-abcdcodev2";
    public static string SearchTopic = "lmm-lobotomymod-tag-by-abcdcode OR lmm-lobotomymod-tag-by-abcdcodev2";
    public static GitHubRelease recent;
    public async Task<GitHubAuthor> GetOwnerInfo(string key)
    {
        var url = "https://api.github.com/user";
        var result = await Request(url, key, null, "GET");
        if (result == null) return null;

        var r = JsonConvert.DeserializeObject<GitHubAuthor>(result.key);
        return r;
    }
    public async Task<string> GetAuthKey()
    {
        string clientId = "b4b76ce9e57fbeee2163";
        string clientSecret = "5abcf67f89bfdfd45564599b724234d7e31ccfd3";
        string redirectURL = "http://localhost:5500";
        var url = $"https://github.com/login/oauth/authorize" + $"?client_id={clientId}&client_secret={clientSecret}&scope=user,repo,administration:write,delete_repo,issues:write,issues:read&redirect_uri={redirectURL}";
        Application.OpenURL(url);

        var token = await StartLocalServer(redirectURL);
        if (token == null) return null;

        return token;
    }
    public async Task<GitHubRelease> GetLastestLMMRelease()
    {
        if (recent != null) return recent;
        var data = await GetLMMRelease();
        recent = data;
        return recent;
    }
	public async Task<GitHubRelease> GetLMMRelease()
	{
        var result = await GetLastestRelease("LobotomyBaseMod", "LMM");
        return result;
    }
    public async Task<bool> DeleteMod(string owner, string repo, string key)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}";
        var result = await Request(url, key, null, "DELETE");

        if (result == null) return false;

        return true;
    }
    public async Task<bool> UploadRelease(string owner, string repo, string key, string filepath)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases";

        var pre = await Request(url, key, null, "GET");

        var list = JsonConvert.DeserializeObject<List<GitHubRelease>>(pre.key);

        string id = "version-1";

        if (list.Count != 0)
        {
            try
            {
                int i = int.Parse(list[0].tag_name.Split('-')[1]);
                id = $"version-{i + 1}";
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        var info = new
        {
            tag_name = id,
            name = id,
            body = "Uploaded by LMM"
        };

        var result = await Request(url, key, info, "POST");

        GitHubRelease re = JsonConvert.DeserializeObject<GitHubRelease>(result.key);

        string url2 = $"https://uploads.github.com/repos/{owner}/{repo}/releases/{re.id}/assets?name=Mod.zip";

        byte[] fileBytes = File.ReadAllBytes(filepath);

        Dictionary<string, string> h = new Dictionary<string, string>();
        h["Content-Type"] = "application/zip";

        var r = await Request(url2, key, fileBytes, "POST", h);

        return true;
    }
    public async Task<string> Graphql_GetFileEachRepo_string(List<GitHubRepos> repos, string filepath)
    {
        string query = @"query {";

        foreach (var repoName in repos)
        {
            query += $"{repoName.name}: repository(owner: \"{repoName.owner.login}\", name: \"{repoName.name}\") {{ object(expression: \"main:{filepath}\") {{ ... on Blob {{ text }} }} }}";
        }

        query += @"}";

        string jsonQuery = "{\"query\": \"" + query.Replace("\"", "\\\"") + "\"}";

        var url = "https://api.github.com/graphql";
        var result = await Request(url, KEY, jsonQuery, "POST");
        return result.key;
    }
    public async Task<List<GitHubModInfo_Upload>> Graphql_GetFileEachRepo(List<GitHubRepos> repos, string filepath)
    {
        /*
        var result = await Graphql_GetFileEachRepo_string(repos,filepath);
        SaveManager.Instance.Save("GraphQLTest.txt", result);
        GitHubSearchDetail_Graphql d = JsonConvert.DeserializeObject<GitHubSearchDetail_Graphql>(result);
        var r = new List<GitHubModInfo_Upload>();
        foreach (var pair in d.data)
        {
            SaveManager.Instance.Save($"QLTest_{pair.Key}.txt", pair.Value._object.text);
            try
            {
                r.Add(JsonConvert.DeserializeObject<GitHubModInfo_Upload>(pair.Value._object.text));
            }
            catch (Exception e)
            {
                continue;
            }
        }*/

        var r = await new GitHubFileDownManager().GetList(repos, filepath);
        return r;
    }
    public async Task<GitHubRepoSearchResult> SearchRepo(string keyword, List<string> topics, int page, int perpage = 20)
    {
        var url = $"https://api.github.com/search/repositories?page={page}&per_page={perpage}&q=";
        url += keyword;
        url += keyword == string.Empty ? $"{SearchTopic}" : $" {SearchTopic}" ;
        if (topics != null)
        {
            foreach (var k in topics)
            {
                url += " " + k;
            }
        }
        if (!keyword.Contains("sort:"))
        {
            url += "&sort=updated";
        }
        /*var hdic = new Dictionary<string, string>();
        hdic.Add("per_page", perpage.ToString());
        hdic.Add("page", page.ToString());*/
        var result = await Request(url, KEY, null, "GET", null);
        if (result == null) return null;
        var list = JsonConvert.DeserializeObject<GitHubRepoSearchResult>(result.key);
        list.items.RemoveAll(x => !x.topics.Contains(V1Topic) && !x.topics.Contains(V2Topic));
        return list;
    }
    public async Task<GitHubRelease> GetLastestRelease(string owner, string repo, string key = null)
    {
        key = key == null ? KEY : key;
        string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        var result = await Request(url, key, null);
        if (result == null) return null;
        return JsonConvert.DeserializeObject<GitHubRelease>(result.key);
    }
    public async Task<List<GitHubRelease>> GetRelease(string owner, string repo, string key = null)
    {
        key = key == null ? KEY : key;
        string url = $"https://api.github.com/repos/{owner}/{repo}/releases";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("User-Agent", "C# HttpClient");
            webRequest.SetRequestHeader("Authorization", $"token {key}");


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
            string jsonData = webRequest.downloadHandler.text;
            return JsonConvert.DeserializeObject<List<GitHubRelease>>(jsonData);
        }
    }

    public async Task<bool> UploadMod_AuthTest(GitHubModInfo_Upload upload,string token)
    {
        return await UploadMod(upload, token);
    }
    public async Task<string> StartLocalServer(string redirectURL)
    {
        string clientId = "b4b76ce9e57fbeee2163";
        string clientSecret = "7a4406bd25c1639d96b2036c347c796d9c57ab35";
        using (var listener = new HttpListener())
        {
            listener.Prefixes.Add(redirectURL + "/");
            listener.Start();
            Debug.Log("Waiting for GitHub authorization...");

            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            // 깃허브로부터 받은 인증 코드 읽기
            string code = request.QueryString["code"];

            // 깃허브로부터 받은 인증 코드 출력
            Debug.Log($"Received authorization code: {code}");

            // 수신된 인증 코드에 대한 응답 보내기
            string responseString = "<html><head><title>GitHub Authorization</title></head><body><h1>Authorization Received</h1><p>Thank you for authorizing the application. You can now close this window.</p></body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();

            // 서버 종료
            listener.Stop();

            var tokenurl = "https://github.com/login/oauth/access_token";

            var Hdic = new Dictionary<string, string>();
            Hdic["Content-Type"] = "application/x-www-form-urlencoded";
            var result = await Request(tokenurl, KEY, $"client_id={clientId}&client_secret={clientSecret}&code={code}","POST", Hdic);
            if (result == null) return null;

            Debug.Log($"Response from server: {result.key}");
            string accessToken = result.key.Split('&')[0].Split('=')[1];
            Debug.Log($"accessToken: {accessToken}");
            return accessToken;
        }
    }
    public async Task<bool> Starred(string owner, string repo, string key, bool Starred)
    {
        var url = $"https://api.github.com/user/starred/{owner}/{repo}";
        if (Starred)
        {
            var result = await Request(url, key, null, "PUT");
            return result != null;
        }
        else
        {
            var result = await Request(url, key, null, "DELETE");
            return result != null;
        }
    }
    public async Task<bool> IsStarred(string owner, string repo, string key)
    {
        var url = $"https://api.github.com/user/starred/{owner}/{repo}";
        var result = await Request(url, key, null);

        return result != null;
    }
    public async Task<bool> UpdateComment(string owner, string repo, int issueid, long commentid,string value, string key)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/issues/comments/{commentid}";
        var data = new
        {
            body = value
        };
        var result = await Request(url, key, data, "PATCH");

        return result != null;
    }
    public async Task<bool> DeleteComment(string owner, string repo, int issueid, long commentid, string key)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/issues/comments/{commentid}";

        var result = await Request(url, key, null, "DELETE");

        return result != null;
    }
    public async Task<bool> UploadComment(string owner, string repo, int issueid, string value, string key)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueid}/comments";
        var data = new
        {
            body = value
        };
        var result = await Request(url, key, data, "POST");
        return result != null;
    }
    public async Task<List<GitHubComment>> GetComments(string owner, string repo, int issueid, int page, int perpage = 20)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueid}/comments?page={page}&per_page={perpage}";
        var result = await Request(url, KEY,null);
        Debug.Log(result.key);
        var r = JsonConvert.DeserializeObject<List<GitHubComment>>(result.key);
        return r;

    }
    public async Task<GitHubIssue> GetCommentIssue(string owner, string repo)
    {
        var result = await GetIssueList(owner, repo, KEY);
        var r = result.Find(x => x.title == "LMMCommentIssue_AutoGenerate");

        if (r == null)
        {
           var re = await CreateCommentIssue(owner, repo, KEY);
            return re;
        }
        return r;
    }
    public async Task<GitHubIssue> CreateCommentIssue(string owner, string repo, string key)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/issues";
        var data = new
        {
            title = "LMMCommentIssue_AutoGenerate",
            body = "by abcdcode",
            labels = new string[] { "LMM_Comment" }
        };
        Dictionary<string, string> adddic = new Dictionary<string, string>();
        adddic["Accept"] = "application/vnd.github+json";
        var r = await Request(url, key, data, "POST", adddic);
        Debug.Log(r.key);
        var re = JsonConvert.DeserializeObject<GitHubIssue>(r.key);
        return re;
    }
    public async Task<List<GitHubIssue>> GetIssueList(string owner, string repo, string key)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/issues";
            var result = await Request(url, key, null);
            if (result == null) return null;
            var r = JsonConvert.DeserializeObject<List<GitHubIssue>>(result.key);
            return r;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message + Environment.NewLine + e.StackTrace);
            return null;
        }
    }
    public async Task<bool> UploadMod(GitHubModInfo_Upload upload, string key)
    {
        

        var s1 = await CreateRepos_MOD(upload, key);
        if (!s1) return false;

        return true;
    }
    public async Task<bool> SetRepoTopics(GitHubModInfo_Upload upload, List<string> topics, string key)
    {
        var url = $"https://api.github.com/repos/{upload.owner}/{upload.modname}/topics";

        var list = new List<string>(topics);
        list.Add(CurTopic);
        if (upload.dependancy.Count == 0)
        {
            list.Add(V1Topic);
        }

        var request = new { names = list };

        var result = await Request(url, key, request, "PUT");
        return result != null;
    }
    public async Task<bool> CreateRepos_MOD(GitHubModInfo_Upload upload, string key)
    {
        bool step1 = await CreateRepo(upload.modname,upload.desc,key);

        var step2 = await SetRepoTopics(upload, upload.topics, key);
        if (!step2) goto Fail;

        var s = JsonConvert.SerializeObject(upload);
        var filepath = upload.filepath;
        upload.filepath = string.Empty;
        bool step3 = await SetRepoFile_string(upload.owner, upload.modname, "Data.txt", s, key);
        if (!step3) goto Fail;

        var step4 = await UploadRelease(upload.owner, upload.modname, key, filepath);
        if (!step4) goto Fail;
        return true;

    Fail:
        if (!step1)
        {
            var r1 = await DeleteMod(upload.owner, upload.modname, key);
        }
        return false;
    }
    public async Task<bool> SetRepoFile_byte(string owner, string repo, string path, string filepath, string key)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
       
        byte[] imageBytes = File.ReadAllBytes(filepath);

        string base64Image = Convert.ToBase64String(imageBytes);
        var requestData = new Dictionary<string, string>();
        requestData["message"] = "Add byte";
        requestData["content"] = base64Image;

        var check = await Request(url, key, null, "GET");
        if (check != null)
        {
           var con = JsonConvert.DeserializeObject<GitHubContent>(check.key);
            requestData["sha"] = con.sha;
        }
            var result = await Request(url, key, requestData, "PUT");
        return result != null;
    }
    public async Task<bool> SetRepoFile_string(string owner, string repo, string path, string content, string key)
    {
        var b = Encoding.UTF8.GetBytes(content);
        var c = System.Convert.ToBase64String(b);
        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
        var requestData = new Dictionary<string, string>();
        requestData["message"] = "Add Text";
        requestData["content"] = c;

        var check = await Request(url, key, null, "GET");
        if (check != null)
        {
            var con = JsonConvert.DeserializeObject<GitHubContent>(check.key);
            requestData["sha"] = con.sha;
        }

        var result = await Request(url, key, requestData, "PUT");
        return result != null;
    }
    public async Task<bool> CreateRepo(string name, string desc, string key)
    {
        string url = "https://api.github.com/user/repos";
        var requestData = new
        {
            name = name,
            description = desc
        };
        var result = await Request(url, key, requestData, "POST");
        return result != null;
    }
    public async Task<GitHubModInfo_Upload> ModVaild(string moddependancyStr)
    {
        string[] array = moddependancyStr.Split('/');
        if (array.Length != 2) return null;
        return await ModVaild(array[0], array[1]);
    }
    public async Task<GitHubModInfo_Upload> ModVaild(string owner, string modname)
    {
        var url = $"https://raw.githubusercontent.com/{owner}/{modname}/main/Data.txt";
        var c = new Checker();
        DownloadManager.Instance.FileDownLoad_NoFile(url, delegate (string data) { c.Check = true; c.result = data; }, null);
        while (!c.Check)
        {
            await Task.Delay(10);
        }
        var result = c.result;
        if (result == null) return null;
        return JsonConvert.DeserializeObject<GitHubModInfo_Upload>(result);
    }
    public class Checker
    {

        public bool Check = false;
        public string result;
    }
    public void ModValidComplete(string data)
    {

    }
    public async Task<string> GetApiKey()
    {
        Debug.Log("GetApiKey");
        using (UnityWebRequest webRequest = UnityWebRequest.Get("https://lmmapi.s3.ap-northeast-1.amazonaws.com/index.html"))
        {
            var asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {webRequest.error}");
                Debug.Log($"Error Detail Message : {webRequest.downloadHandler.text}");
                return null;
            }
            Debug.Log("GetApiKey Success!");
            string key = webRequest.downloadHandler.text.TrimEnd();
            key = ExtensionUtil.StringDecode(ExtensionUtil.StringDecode(ExtensionUtil.StringDecode(key)));
            Debug.Log("API key : " + key);
            return key;
        }
    }
    public async Task<SKeyValuePair<string, byte[]>> Request_v2(string url, string key, object send, string method = "GET", Dictionary<string, string> addHeader = null, bool limitdebug = false)
    {
        var requestData = new
        {
            path = url,  
            method = method,                  
            auth = key, 
            data = send
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        Debug.Log($"Send to : {url}");
        using (UnityWebRequest webRequest = UnityWebRequest.Get("https://ujhii8nfyc.execute-api.ap-northeast-1.amazonaws.com/LMMAPI"))
        {
            webRequest.method = "POST";
            webRequest.SetRequestHeader("User-Agent", "C# HttpClient");
            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (addHeader != null)
            {
                foreach (var pair in addHeader)
                {
                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }
            Debug.Log("dend data : " + jsonData);
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);

            var asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {webRequest.error}");
                Debug.Log($"Error Detail Message : {webRequest.downloadHandler.text}");
                return null;
            }
            Debug.Log("Request Success!");
            Debug.Log(webRequest.downloadHandler.text);
            var d = JsonConvert.DeserializeObject<Dictionary<string, object>>(webRequest.downloadHandler.text);
            if (d.ContainsKey("statusCode"))
            {
                Debug.Log("StatusCode : " + d["statusCode"]);

                var sc = d["statusCode"].ToString();
                if (int.Parse(sc) == 404)
                {
                    return null;
                }
            }
            if (d.ContainsKey("body"))
            {
                return new SKeyValuePair<string, byte[]>(JsonConvert.SerializeObject(d["body"]), webRequest.downloadHandler.data);
            }
            //Debug.Log(d["body"]);
            return new SKeyValuePair<string, byte[]>("", webRequest.downloadHandler.data);
        }
    }
    public async Task<SKeyValuePair<string, byte[]>> Request(string url,string key,object send, string method = "GET", Dictionary<string,string> addHeader = null)
    {
        if (url.Contains("https://api.github.com"))
        {
            url = url.Replace("https://api.github.com", "");
            return await Request_v2(url, key, send, method, addHeader);
        }
        /*
        if (KEY == string.Empty)
        {
            KEY = await GetApiKey();
        }
        if (key == string.Empty)
        {
            key = KEY;
        }
        */
       // key = "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";
        Debug.Log($"Send to : {url}");
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.method = method;
            webRequest.SetRequestHeader("User-Agent", "C# HttpClient");
            webRequest.SetRequestHeader("Authorization", $" Bearer {key}");
            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (addHeader != null)
            {
                foreach (var pair in addHeader)
                {
                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            if (send is byte[])
            {
                webRequest.uploadHandler = new UploadHandlerRaw((byte[])send);
            }
            else if (send is string)
            {
                Debug.Log("send : " + send);
                webRequest.uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes((string)send));
            }
            else if (send == null)
            {

            }
            else
            {
                string json = JsonConvert.SerializeObject(send);
                Debug.Log("send : " + json);
                byte[] jsonToSend = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
            }

            var asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {webRequest.error}");
                Debug.Log($"Error Detail Message : {webRequest.downloadHandler.text}");
                return null;
            }
            Debug.Log("Request Success!");
            Debug.Log(webRequest.downloadHandler.text);
            return new SKeyValuePair<string, byte[]>(webRequest.downloadHandler.text, webRequest.downloadHandler.data);
        }
    }
}
public class GitHubFileDownManager
{
    public GitHubFileDownManager()
    {
        cur = null;
    }
   
    public async Task<List<GitHubModInfo_Upload>> GetList(List<GitHubRepos> repos, string filepath)
    {
        curjobindex += 1;
        int job = curjobindex;
        cur = new List<GitHubModInfo_Upload>();
        int i = 0;
        foreach (var repo in repos)
        {
            cur.Add(null);
            var url = $"https://raw.githubusercontent.com/{repo.owner.login}/{repo.name}/main/{filepath}";
            var tmppath = $"{Application.persistentDataPath}/{repo.owner.login}_{repo.name}_P{job}";
            var j = i;
            Debug.Log($"Download Start : {tmppath}");
            DownloadManager.Instance.FileDownLoad_NoFile(url, delegate(string data) { Complete(data,j, job); }, null);
            i++;
        }

        while (!IsAllClear())
        {
            await Task.Delay(10);
        }
        return cur;
    }
    public bool IsAllClear()
    {
        foreach (var c in cur)
        {
            if (c == null) return false;
        }
        return true;
    }
    public List<GitHubModInfo_Upload> cur;
    public static int curjobindex = 0;
    public void Complete(string data,int index, int curjob)
    {
        //Debug.Log($"Download Start : {path}");
        try
        {
            if (curjobindex != curjob)
            {
                goto Skip;
            }

            GitHubModInfo_Upload result = JsonConvert.DeserializeObject<GitHubModInfo_Upload>(data);
            cur[index] = result;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    Skip:
        return;
        //File.Delete(path);
    }
}
[Serializable]
public class GitHubSearchDetail_Graphql
{
    public Dictionary<string, Data> data { get; set; }
    [Serializable]
    public class Data
    {
        [JsonProperty("object")]
        public RealData _object;

        [Serializable]
        public class RealData
        {
            public string text;
        }
    }
}
[Serializable]
public class GitHubRepoSearchResult
{
    public int total_count;
    public bool incomplete_results;
    public List<GitHubRepos> items;
}
[Serializable]
public class GitHubModInfo_Upload
{
    public string ModId {
        get {
            return owner + " " + modname;
        }
    }
    public string owner;
    public string modname;
    public Dictionary<SupportLanguage, string> DescDic = new Dictionary<SupportLanguage, string>();
    public string filepath;
    public string thumbdata;
    public List<string> sideimages = new List<string>();
    public string desc;
    public List<string> dependancy;
    public List<string> topics = new List<string>();
}
[Serializable]
public class GitHubComment
{
    public string author_association;
    public string body;
    public string created_at;
    public string html_url;
    public long id;
    public string issue_url;
    public string node_id;
    public string updated_at;
    public string url;
    public GitHubAuthor user;
}
[Serializable]
public class GitHubIssueLabel
{
    public string color;
    [JsonProperty("default")]
    public bool _default;
    public string description;
    public long id;
    public string name;
    public string node_id;
    public string url;
}
[Serializable]
public class GitHubIssue
{
    public string author_association;
    public string body;
    public string closed_at;
    public int comments;
    public string comments_url;
    public string created_at;
    public string events_url;
    public string html_url;
    public long id;
    public List<GitHubIssueLabel> labels;
    public string labels_url;
    public bool locked;
    public string node_id;
    public int number;
    public string timeline_url;
    public string title;
    public string updated_at;
    public string url;
    public GitHubAuthor user;
}
[Serializable]
public class GitHubContent
{
    public string content;
    public string download_url;
    public string encoding;
    public string git_url;
    public string html_url;
    public string name;
    public string path;
    public string sha;
    public long size;
    public string type;
    public string url;
}
[Serializable]
public class GitHubRelease
{
    public List<GitHubAsset> assets;
    public string assets_url;
    public GitHubAuthor author;
    public string body;
    public string created_at;
    public bool draft;
    public string html_url;
    public long id;
    public string name;
    public string node_id;
    public bool prerelease;
    public string published_at;
    public string tag_name;
    public string tarball_url;
    public string target_commitish;
    public string upload_url;
    public string url;
}
[Serializable]
public class GitHubAuthor
{
    public string avatar_url;
    public string events_url;
    public string followers_url;
    public string following_url;
    public string gists_url;
    public string gravatar_id;
    public string html_url;
    public long id;
    public string login;
    public string node_id;
    public string organizations_url;
    public string received_events_url;
    public string repos_url;
    public string site_admin;
    public string starred_url;
    public string subscriptions_url;
    public string type;
    public string url;
}
[Serializable]
public class GitHubAsset
{
	public string browser_download_url;
    public string content_type;
    public string created_at;
    public int download_count;
    public long id;
    public string name;
    public string node_id;
    public long size;
    public string state;
    public string updated_at;
    public GitHubAuthor uploader;
    public string url;
}
[Serializable]
public class GitHubRepos
{
    public bool allow_forking;
    public bool archived;
    public string archive_url;
    public string assignees_url;
    public string blobs_url;
    public string branches_url;
    public string clone_url;
    public string collaborators_url;
    public string comments_url;
    public string commits_url;
    public string compare_url;
    public string contents_url;
    public string contributors_url;
    public string created_at;
    public string default_branch;
    public string deployments_url;
    public string description; //
    public bool disabled;
    public string downloads_url;
    public string events_url;
    public bool fork;
    public int forks;
    public int forks_count;
    public string forks_url;
    public string full_name;
    public string git_commits_url;
    public string git_refs_url;
    public string git_tags_url;
    public string git_url;
    public bool has_discussions;
    public bool has_downloads;
    public bool has_issues;
    public bool has_pages;
    public bool has_projects;
    public bool has_wiki;
    public string homepage; //
    public string hooks_url;
    public string html_url;
    public long id;
    public string issues_url;
    public string issue_comment_url;
    public string issue_events_url;
    public bool is_template;
    public string keys_url;
    public string labels_url;
    public string language; //
    public string languages_url;
    public string license; //
    public string merges_url;
    public string milestones_url;
    public string mirror_url; //
    public string name;
    public string node_id;
    public string notifications_url;
    public int open_issues;
    public int open_issues_count;
    public GitHubAuthor owner;
    [JsonProperty("private")]
    public bool _private;
    public string pulls_url;
    public string pushed_at;
    public string releases_url;
    public long size;
    public string ssh_url;
    public int stargazers_count;
    public string stargazers_url;
    public string statuses_url;
    public string subscribers_url;
    public string subscription_url;
    public string svn_url;
    public string tags_url;
    public string teams_url;
    public List<string> topics;
    public string trees_url;
    public string updated_at;
    public string url;
    public string visibility;
    public long watchers;
    public long watchers_count;
    public bool web_commit_signoff_required;
}