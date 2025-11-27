using UnityEngine;
using TMPro;

public class CarSpeedUI : MonoBehaviour
{
    public Rigidbody carRigidbody;        // 차에 있는 Rigidbody
    public TextMeshProUGUI speedText;     // 속도 표시할 TextMeshPro UI

    void Update()
    {
        if (carRigidbody == null || speedText == null) return;

        float speed = carRigidbody.linearVelocity.magnitude;
        float speedKmh = speed * 3.6f;

        speedText.text = Mathf.RoundToInt(speedKmh) + " km/h";
    }
}