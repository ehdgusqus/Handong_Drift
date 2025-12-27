using UnityEngine;

public class ShowroomCamera : MonoBehaviour
{
    public Transform targetCar;   // 주인공 자동차
    public float rotationSpeed = 10f; // 회전 속도
    public float height = 3f;     // 카메라 높이
    public float distance = 7f;   // 차와의 거리

    void LateUpdate()
    {
        if (targetCar == null) return;

        // 1. 차 주변을 빙글빙글 돌기
        transform.RotateAround(targetCar.position, Vector3.up, rotationSpeed * Time.deltaTime);

        // 2. 카메라는 항상 차를 쳐다보기
        transform.LookAt(targetCar.position + Vector3.up * 1.0f); // 차의 약간 위쪽을 바라봄
        
        // (선택) 거리가 틀어지지 않게 고정하고 싶다면 아래 코드 추가 가능하지만,
        // RotateAround만 써도 쇼룸 느낌은 충분합니다.
    }
}