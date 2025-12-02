using UnityEngine;
using UnityEngine.UI;

public class SpeedometerFactory : MonoBehaviour
{
    // 게임 시작 시 계기판이 없으면 자동으로 만듭니다.
    void Start()
    {
        CreateSpeedometer();
    }

    public void CreateSpeedometer()
    {
        // 1. 캔버스 찾기 (없으면 생성)
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. 계기판 배경 (Dial) 만들기
        GameObject dialObj = new GameObject("Speedometer_Dial");
        dialObj.transform.SetParent(canvas.transform, false);
        
        Image dialImage = dialObj.AddComponent<Image>();
        dialImage.sprite = UISprite.GetKnob(); // 유니티 기본 원형 스프라이트 가져오기
        dialImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // 진한 회색 (반투명)
        
        // 위치 및 크기 설정 (화면 오른쪽 아래)
        RectTransform dialRect = dialObj.GetComponent<RectTransform>();
        dialRect.anchorMin = new Vector2(1, 0);
        dialRect.anchorMax = new Vector2(1, 0);
        dialRect.pivot = new Vector2(1, 0);
        dialRect.anchoredPosition = new Vector2(-20, 20); // 여백
        dialRect.sizeDelta = new Vector2(250, 250); // 크기

        // 3. 바늘 (Needle) 만들기
        GameObject needleObj = new GameObject("Needle");
        needleObj.transform.SetParent(dialObj.transform, false);
        
        Image needleImage = needleObj.AddComponent<Image>();
        needleImage.color = Color.red; // 빨간색 바늘
        
        // 바늘 크기 및 피벗 설정 (핵심!)
        RectTransform needleRect = needleObj.GetComponent<RectTransform>();
        needleRect.anchorMin = new Vector2(0.5f, 0.5f);
        needleRect.anchorMax = new Vector2(0.5f, 0.5f);
        needleRect.pivot = new Vector2(0.5f, 0f); // ★ 회전축을 바늘 아래로 설정
        needleRect.sizeDelta = new Vector2(6, 110); // 얇고 긴 모양
        needleRect.anchoredPosition = new Vector2(0, 0); // 중앙 정렬

        // 4. (선택) 중앙 덮개 (바늘 회전축 가리기용)
        GameObject capObj = new GameObject("Cap");
        capObj.transform.SetParent(dialObj.transform, false);
        Image capImage = capObj.AddComponent<Image>();
        capImage.sprite = UISprite.GetKnob();
        capImage.color = Color.black;
        capObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);

        Debug.Log("계기판 생성 완료! Hierarchy 창에서 'Speedometer_Dial'을 확인하세요.");
        
        // 5. 아까 만든 SpeedometerUI 스크립트가 있다면 자동으로 연결 시도
        // (프로젝트에 SpeedometerUI 스크립트가 있어야 작동합니다)
        var uiScript = dialObj.AddComponent<SpeedometerUI>();
        if(uiScript != null)
        {
            uiScript.needle = needleRect;
            uiScript.maxSpeed = 260f;
            uiScript.minAngle = 135f;
            uiScript.maxAngle = -135f;
        }
    }
}

// 유니티 기본 스프라이트(Knob 등)를 코드로 가져오기 위한 헬퍼 클래스
public static class UISprite 
{
    public static Sprite GetKnob()
    {
        // 유니티 UI 기본 리소스에서 Knob(동그라미) 스프라이트를 찾아서 리턴
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
    }
}