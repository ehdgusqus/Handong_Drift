using UnityEngine;
using UnityEngine.UI;

public class SpeedometerUI : MonoBehaviour
{
    public RectTransform needle; // 바늘 이미지
    public float maxSpeed = 260f; // 최대 속도
    public float minAngle = 135f; // 시작 각도
    public float maxAngle = -135f; // 끝 각도

    public void UpdateSpeed(float speed)
    {
        // 속도 제한 및 비율 계산
        float clampedSpeed = Mathf.Clamp(speed, 0f, maxSpeed);
        float speedRatio = clampedSpeed / maxSpeed;
        
        // 각도 계산 및 회전 적용
        float targetAngle = Mathf.Lerp(minAngle, maxAngle, speedRatio);
        if (needle != null)
        {
            needle.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
        }
    }
}