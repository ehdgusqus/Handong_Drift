using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class ForzaHUD : MonoBehaviour
{
    [Header("UI 연결")]
    public Image rpmBar;              // 게이지 이미지
    public TextMeshProUGUI speedText; // 속도 텍스트

    [Header("설정")]
    public float maxSpeed = 200f;     // 차량 최고 속도
    
    [Header("색상 설정")]
    public Color normalColor = Color.pink; // 기본 색상 (분홍)
    public Color redLineColor = Color.red; // 최고 속도 색상 (빨강)
    
    [Range(0f, 1f)]
    public float colorChangeStart = 0.5f; // 언제부터 색이 변하기 시작할까요? (0.5 = 50% 속도부터)

    public void UpdateHUD(float currentSpeed)
    {
        // 1. 속도 텍스트 표시
        if (speedText != null)
        {
            speedText.text = Mathf.RoundToInt(currentSpeed).ToString();
        }

        // 2. 게이지 및 색상 처리
        if (rpmBar != null)
        {
            // 비율 계산 (0.0 ~ 1.0)
            float fillRatio = currentSpeed / maxSpeed;
            float clampedRatio = Mathf.Clamp01(fillRatio);

            // 게이지 채우기
            rpmBar.fillAmount = clampedRatio;

            // 3. 색상 부드럽게 변경 (Lerp 사용)
            // 설명: 현재 비율이 '변화 시작점(0.5)'과 '끝(1.0)' 사이 어디쯤인지 0~1로 계산
            float colorT = Mathf.InverseLerp(colorChangeStart, 1f, clampedRatio);
            
            // colorT가 0이면 pink, 1이면 red, 0.5면 반반 섞인 색이 나옴
            rpmBar.color = Color.Lerp(normalColor, redLineColor, colorT);
        }
    }
}