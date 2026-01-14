using UnityEngine;
using UnityEngine.UI; 
using System.Collections;

public class UIRaceStart : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Image[] signalImages; 
    public GameObject signalPanel; 

    [Header("색상 설정")]
    public Color offColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 평소 (회색)
    public Color redColor = Color.red;    // 대기 (빨강)
    public Color greenColor = Color.green; // 출발! (초록) - 새로 추가됨

    [Header("게임 설정")]
    public MonoBehaviour carController; 
    public float lightInterval = 1.0f;  

    public static bool isRaceStarted = false; 

    void Start()
    {
        isRaceStarted = false;
        if (carController != null) carController.enabled = false;
        
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

        float randomDelay = Random.Range(0.2f, 1.0f);
        yield return new WaitForSeconds(randomDelay);


        foreach (var img in signalImages)
        {
            img.color = greenColor;
        }

        StartGame();

        yield return new WaitForSeconds(2.0f);
        signalPanel.SetActive(false); 

    }

    void StartGame()
    {
        isRaceStarted = true;
        if (carController != null) carController.enabled = true;
        Debug.Log("GO! Green Light!");
    }
}