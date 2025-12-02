using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("카메라 연결")]
    public GameObject mainCamera;   // 기존 외부 카메라 (3인칭)
    public GameObject driverCamera; // 방금 만든 내부 카메라 (1인칭)

    void Start()
    {
        // 게임 시작 시 외부 카메라는 켜고, 내부 카메라는 끄기
        if (mainCamera != null) mainCamera.SetActive(true);
        if (driverCamera != null) driverCamera.SetActive(false);
    }

    void Update()
    {
        // 탭(Tab) 키를 누르면 전환
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchCamera();
        }
    }

    void SwitchCamera()
    {
        // 현재 외부 카메라가 켜져 있는지 확인
        bool isMainActive = mainCamera.activeSelf;

        // 상태 반전 (켜져 있으면 끄고, 꺼져 있으면 켜고)
        mainCamera.SetActive(!isMainActive);
        driverCamera.SetActive(isMainActive);
    }
}