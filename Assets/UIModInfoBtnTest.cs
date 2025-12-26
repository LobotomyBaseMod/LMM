using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.Networking;
using System.IO;
using ImageMagick;
using System.Net;
using System.Threading.Tasks;

public class UIModInfoBtnTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetData(NexusModInfo info)
    {
         this.gameObject.SetActive(true);
        modinfo = info;
        Init();
       
    }
	public async void Init()
	{
        title.text = modinfo.name;

        //this.StartCoroutine(LoadImageFromURL(modinfo.picture_url));
        await LoadImageFromURL(modinfo.picture_url);
    }
    public async Task<int> LoadImageFromURL(string url)
    {
        byte[] bt = await GetImage(url);
        string path = Application.dataPath + "/img/" + modinfo.name + System.IO.Path.GetExtension(url);
        System.IO.File.WriteAllBytes(path, bt);
        await Task.Run(() =>
        {
            using (MagickImage image = new MagickImage(path))
            {
                // 이미지 처리 작업 (옵션 설정 등)

                // PNG로 저장
                image.Write(path.GetPathWithoutExtension() + "_processing.png", MagickFormat.Png);
            }
        });
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(Application.dataPath + "/img/" + modinfo.name + "_processing.png"));
        tex = texture;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        img.sprite = sprite;
        return 0;
    }
    public async Task<byte[]> GetImage(string url)
    {
        using (WebClient client = new WebClient())
        {
            byte[] imgArray;
            imgArray = await client.DownloadDataTaskAsync(url);

            return imgArray;
        }
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
    public NexusModInfo modinfo;

    public TextMeshProUGUI title;

    public UnityEngine.UI.Image img;

    public Texture2D tex;
}
