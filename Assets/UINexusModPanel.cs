using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using ImageMagick;
using System.Net;
using System.IO;
using System;
using UnityEngine.Networking;

[Serializable]
public class UINexusModPanel : MonoBehaviour
{
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SetData(NexusModInfoV2 info)
    {
        this.gameObject.SetActive(true);

        modinfo = info;
        Init();

    }
    public void OnClick()
    {
        if (UINexusModInfoView.Instance.gameObject.activeSelf) return;
        UINexusModInfoView.Instance.Open(modinfo);
    }
    public async void Init()
    {
        title.text = modinfo.mod_name;
        img.enabled = false;
        await LoadImageFromURL(modinfo.mod_pic);
        //fileid = await NexusPageManager.Instance.GetLastestFileid(modinfo.modid);
        //this.StartCoroutine(LoadImageFromURL(modinfo.picture_url));
       
    }
    public async Task<int> LoadImageFromURL(string url)
    {
       
        string spname = url.GetTextureNameInPicURI();
        Sprite sp = SpriteManager.Instance.GetSprite(spname);
        if (sp != null)
        {
            img.sprite = sp;
            img.enabled = true;
            return 0;
        }
        byte[] bt = await GetImage(url);
        string path = Application.dataPath + "/img/" + spname;
        await Task.Run(() =>
        {
            using (MagickImage image = new MagickImage(bt))
            {
                image.Write(path + ".png", MagickFormat.Png);
            }
        });
        Texture2D texture = new Texture2D(2, 2);
       
        texture.LoadImage(File.ReadAllBytes(path + ".png"));
        tex = texture;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        img.sprite = sprite;
        img.enabled = true;
        SpriteManager.Instance.AddSprite(spname, sprite);
        return 0;
    }
    public async Task<byte[]> GetImage(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
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
                return webRequest.downloadHandler.data;
            }
        }
       /* using (WebClient client = new WebClient())
        {
            byte[] imgArray;
            imgArray = await client.DownloadDataTaskAsync(url);

            return imgArray;
        }*/
    }

    /*
    IEnumerator LoadImageFromURL(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            Debug.Log(www.url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                img.sprite = sprite;
            }
            else
            {
                Debug.LogError("Failed to load image: " + www.error);
                string path = Application.dataPath + "/img/" + modinfo.name + System.IO.Path.GetExtension(url);




                System.IO.File.WriteAllBytes(path, www.downloadHandler.data);

                using (MagickImage image = new MagickImage(path))
                {
                    image.Quality = 50;


                    image.ColorType = ColorType.Palette;

                    image.Write(path.GetPathWithoutExtension() + "_processing.png", MagickFormat.Png);
                }

                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(Application.dataPath + "/img/" + modinfo.name + "_processing.png"));
                tex = texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                img.sprite = sprite;

                Debug.Log(www.downloadHandler.error);
            }
        }

    }*/

    //[HideInInspector]
    public NexusModInfoV2 modinfo;

    public TextMeshProUGUI title;

    public UnityEngine.UI.Image img;

    public Texture2D tex;

    public int fileid;
}
