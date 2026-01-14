using UnityEngine;

public class Mute : MonoBehaviour
{
    // 제어할 오디오 소스를 담을 변수
    AudioSource bgmSource;

    void Start()
    {
        // 이 스크립트가 붙은 객체(bgm)에 있는 AudioSource 컴포넌트를 가져옵니다.
        bgmSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            // .mute 기능을 사용하면 볼륨 크기를 건드리지 않고 소리만 껐다 킬 수 있습니다.
            // ! 기호는 반대를 의미하므로, 현재 상태(켜짐/꺼짐)를 반대로 뒤집습니다.
            bgmSource.mute = !bgmSource.mute;
        }
    }
}