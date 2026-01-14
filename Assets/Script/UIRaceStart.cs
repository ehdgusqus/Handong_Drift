using UnityEngine;
using UnityEngine.UI; 
using System.Collections;

public class UIRaceStart : MonoBehaviour
{
    [Header("오디오 소스 연결")]
    public AudioSource startBeepSource; // 일반 삑 (Red Light)
    public AudioSource playBeepSource;  // 높은 삑 (Green Light / Start)
    
    [Header("오디오 설정 (음소거용)")]
    public AudioSource[] soundsToMute; 

    [Header("UI 컴포넌트")]
    public Image[] signalImages; 
    public GameObject signalPanel; 

    [Header("색상 설정")]
    public Color offColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
    public Color redColor = Color.red;    
    public Color greenColor = Color.green; 

    [Header("게임 설정")]
    public MonoBehaviour carController; 
    public float lightInterval = 1.0f;  

    void Start()
    {
        if (carController != null) carController.enabled = false;
        
        // BGM, 엔진 소리 끄기
        foreach (var audio in soundsToMute) if (audio != null) audio.mute = true;

        signalPanel.SetActive(true);
        foreach (var img in signalImages) img.color = offColor;

        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(1.0f); 

        // 1. 빨간불 (startBeepSource 재생)
        foreach (var img in signalImages)
        {
            img.color = redColor;
            
            // 일반 피치 소리 재생
            if (startBeepSource != null) startBeepSource.Play();

            yield return new WaitForSeconds(lightInterval);
        }

        float Delay = 1.3f;
        yield return new WaitForSeconds(Delay);

        // 2. 초록불 (playBeepSource 재생)
        foreach (var img in signalImages) img.color = greenColor;

        // 높은 피치(출발) 소리 재생
        if (playBeepSource != null) playBeepSource.Play();

        StartGame();

        yield return new WaitForSeconds(2.0f);
        signalPanel.SetActive(false); 
    }

    void StartGame()
    {
        if (carController != null) carController.enabled = true;

        // 소리 다시 켜기
        foreach (var audio in soundsToMute) if (audio != null) audio.mute = false;
    }
}