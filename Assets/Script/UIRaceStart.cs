using UnityEngine;
using UnityEngine.UI; 
using System.Collections;

public class UIRaceStart : MonoBehaviour
{
    [Header("오디오 설정")]
    // 여기에 BGM이나 자동차 엔진 소리가 담긴 오브젝트를 드래그해서 넣으세요
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

    public static bool isRaceStarted = false; 

    void Start()
    {
        isRaceStarted = false;
        if (carController != null) carController.enabled = false;
        
        // [추가됨] 시작하자마자 등록된 소리들 전부 음소거(Mute)
        foreach (var audio in soundsToMute)
        {
            if (audio != null) audio.mute = true;
        }

        signalPanel.SetActive(true);
        foreach (var img in signalImages) img.color = offColor;

        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(1.0f); 

        foreach (var img in signalImages)
        {
            img.color = redColor;
            yield return new WaitForSeconds(lightInterval);
        }

        float Delay = 1.0f;
        yield return new WaitForSeconds(Delay);

        foreach (var img in signalImages) img.color = greenColor;

        StartGame();

        yield return new WaitForSeconds(2.0f);
        signalPanel.SetActive(false); 
    }

    void StartGame()
    {
        isRaceStarted = true;
        if (carController != null) carController.enabled = true;

        foreach (var audio in soundsToMute)
        {
            if (audio != null) audio.mute = false;
        }

        Debug.Log("GO! Sound On!");
    }
}