using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AICarPainter : MonoBehaviour
{
    public string serverUrl = "http://localhost:8000/generate_skin";
    public Renderer carRenderer;

    public void RequestNewSkin(string prompt)
    {
        StartCoroutine(GenerateSkinRoutine(prompt));
    }

    IEnumerator GenerateSkinRoutine(string prompt)
    {
        WWWForm form = new WWWForm();
        form.AddField("prompt", prompt);
        form.AddField("car_model_id", "sports_car_01");

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            www.timeout = 300; 

            Debug.Log("서버에 요청 중... 잠시만 기다려주세요.");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                Texture2D newSkin = new Texture2D(2, 2);
                newSkin.LoadImage(www.downloadHandler.data); 
                
                carRenderer.material.mainTexture = newSkin; 
                Debug.Log("도색 완료!");
            }
        }
    }
}