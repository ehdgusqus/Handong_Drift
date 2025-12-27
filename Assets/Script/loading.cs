using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; 

public class LevelLoader : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject loadingScreen;
    public Slider slider;
    public TextMeshProUGUI progressText;

    [Header("설정")]
    public float minLoadingTime = 2.0f; // 최소 이 시간만큼은 로딩하는 척함 (초)

    public void LoadGame(string sceneName)
    {
        StartCoroutine(LoadAsynchronously(sceneName));
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료!");
        Application.Quit();
    }

    IEnumerator LoadAsynchronously(string sceneName)
    {
        loadingScreen.SetActive(true);
        
        // 1. 비동기 로딩 시작 (화면 전환 막아둠)
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false; // 로딩 다 돼도 바로 넘어가지 마!

        float timer = 0.0f;

        // 2. 로딩이 끝날 때까지 반복 (가짜 시간 or 진짜 로딩 중 하나라도 안 끝났으면 계속)
        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            // [진짜 로딩률] 0.9가 만땅임 -> 0~1로 변환
            float realProgress = operation.progress / 0.9f;
            
            // [가짜 로딩률] 시간이 흐른 비율 (0초 ~ 2초)
            float fakeProgress = timer / minLoadingTime;

            // 둘 중 더 '작은' 값을 사용해서, 로딩이 빨라도 천천히 차오르게 함
            // (만약 로딩이 더 오래 걸리면 진짜 로딩률을 따라감)
            float currentProgress = Mathf.Min(realProgress, fakeProgress);

            // UI 업데이트
            slider.value = currentProgress;
            if(progressText != null)
                progressText.text = (currentProgress * 100f).ToString("F0") + "%";

            // 3. 로딩도 다 됐고(0.9), 가짜 시간(2초)도 지났으면 넘어가기
            if (operation.progress >= 0.9f && timer >= minLoadingTime)
            {
                // 딱 100% 찍어주고 씬 넘김
                slider.value = 1f;
                if(progressText != null) progressText.text = "100%";
                
                operation.allowSceneActivation = true; // 잠금 해제!
            }

            yield return null;
        }
    }
}