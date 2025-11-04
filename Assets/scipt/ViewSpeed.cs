using UnityEngine;
using TMPro;

public class CarSpeedUI : MonoBehaviour
{
    public Rigidbody rb;        // 차에 있는 Rigidbody
    public TextMeshProUGUI speedText;     // 속도 표시할 TextMeshPro UI

    void Update()
    {
        if (rb == null || speedText == null) return;

        float speed = rb.linearVelocity.magnitude;
        float speedKmh = speed * 3.6f;

        speedText.text = Mathf.RoundToInt(speedKmh) + " km/h";
    }
}