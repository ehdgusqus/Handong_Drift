using UnityEngine;
using TMPro;

public class SimpleCarController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public WheelCollider wheelFL, wheelFR, wheelRL, wheelRR;
    public Transform meshFL, meshFR, meshRL, meshRR; // 선택
    public TextMeshProUGUI speedText;                // 선택
    [SerializeField] private float maxSpeed = 200f;

    [Header("Drive")]
    [Tooltip("W 또는 S를 누르고 있을 때 바퀴에 거는 최대 토크(N·m)")]
    public float maxMotorTorque = 1200f;   // 필요하면 800~2000 사이로 조절
    [Tooltip("키를 떼었을 때 서서히 감속을 위한 약한 브레이크 토크")]
    public float coastBrakeTorque = 150f;

    [Header("Steering")]
    public float maxSteerAngle = 28f;
    [Tooltip("미세 입력 무시")]
    public float steerDeadzone = 0.05f;

    // === 드리프트 옵션 (Reverse + Arrow) ===
    [Header("Drift (Reverse + Arrow)")]
    [Tooltip("드리프트 진입/해제 스무딩 속도(높을수록 빠르게 변함)")]
    public float driftBlendSpeed = 8f;
    [Tooltip("드리프트 중 뒤축 감속 토크")]
    public float driftBrakeTorque = 1800f;
    [Tooltip("드리프트 시 뒤타이어 측면 그립(낮을수록 더 미끄러짐)")]
    [Range(0.2f, 2f)] public float rearLatStiffnessInDrift = 0.6f;
    [Tooltip("드리프트 시 앞타이어 측면 그립(조향 안정성)")]
    [Range(0.5f, 2f)] public float frontLatStiffnessInDrift = 1.0f;
    [Tooltip("바깥쪽 뒤타이어 그립 스케일(0.5~1.0, 낮을수록 누른 방향으로 더 잘 말림)")]
    [Range(0.5f, 1.0f)] public float outsideRearGripScale = 0.8f;
    [Tooltip("가벼운 회전 보조(요 토크). 0이면 꺼짐")]
    public float yawAssist = 0.0f; // 0.5~1.2 권장 (원하면 사용)

    // 내부 상태
    float throttle;   // -1(S) ~ 0 ~ +1(W)
    float steer;      // -1(A) ~ 0 ~ +1(D)
    bool driftHeld;   // S + 좌/우 방향키 조건
    float driftT;     // 0~1 블렌드

    // 원본 마찰 저장
    WheelFrictionCurve flFwd0, flLat0, frFwd0, frLat0, rlFwd0, rlLat0, rrFwd0, rrLat0;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        // 물리 안정성 추천값(원하면 인스펙터에서 조절)
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // 너무 오래 활주하지 않게 약간의 드래그 권장 (Unity 6 기준)
        if (rb.linearDamping < 0.02f) rb.linearDamping = 0.02f;

        // 원본 마찰 커브 캐싱
        if (wheelFL) { flFwd0 = wheelFL.forwardFriction; flLat0 = wheelFL.sidewaysFriction; }
        if (wheelFR) { frFwd0 = wheelFR.forwardFriction; frLat0 = wheelFR.sidewaysFriction; }
        if (wheelRL) { rlFwd0 = wheelRL.forwardFriction; rlLat0 = wheelRL.sidewaysFriction; }
        if (wheelRR) { rrFwd0 = wheelRR.forwardFriction; rrLat0 = wheelRR.sidewaysFriction; }
    }

    void Update()
    {
        // 1) 입력 처리 — "누르고 있을 때만" 동작
        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);

        // 둘 다 누르거나 둘 다 안 누르면 0 (상충 방지)
        if (w ^ s) throttle = w ? 1f : -1f;
        else       throttle = 0f;

        // A/D 방향전환 (Raw + 데드존)
        steer = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(steer) < steerDeadzone) steer = 0f;

        // S(후진) + 좌/우 => 드리프트 트리거
        driftHeld = (throttle < 0f) && (Mathf.Abs(steer) > 0f);

        // 속도 UI (선택)
        if (speedText)
        {
            float kmh = rb ? rb.linearVelocity.magnitude * 3.6f : 0f;
            speedText.text = Mathf.RoundToInt(kmh) + " km/h";
        }
    }

    void FixedUpdate()
    {
        if (!wheelFL || !wheelFR || !wheelRL || !wheelRR) return;

        // 2) 조향: 앞바퀴만 (고속 감쇠 필요하면 여기서 보간 가능)
        float steerAngle = steer * maxSteerAngle;
        wheelFL.steerAngle = steerAngle;
        wheelFR.steerAngle = steerAngle;

        // 3) 구동: 후륜 구동
        float wheelTorque = throttle * maxMotorTorque;
        wheelRL.motorTorque = wheelTorque * 0.5f;
        wheelRR.motorTorque = wheelTorque * 0.5f;
        wheelFL.motorTorque = 0f;
        wheelFR.motorTorque = 0f;

        // 4) 브레이크 로직
        if (driftHeld)
        {
            // 드리프트 중: 뒤축 감속, 앞축은 굴리게 둬서 조향 유지
            wheelRL.brakeTorque = driftBrakeTorque;
            wheelRR.brakeTorque = driftBrakeTorque;
            wheelFL.brakeTorque = 0f;
            wheelFR.brakeTorque = 0f;
        }
        else
        {
            // 떼면 서서히 감속
            ApplyBrake(Mathf.Approximately(throttle, 0f) ? coastBrakeTorque : 0f);
        }

        // 5) 드리프트 마찰 블렌드(방향성 포함)
        DriftFrictionUpdate(driftHeld, steer);

        // 6) 요 토크 보조(선택)
        if (yawAssist > 0f && driftHeld && rb && rb.linearVelocity.sqrMagnitude > 1f)
        {
            float sign = Mathf.Sign(steer); // 좌(-1) / 우(+1)
            rb.AddTorque(transform.up * sign * yawAssist * 1000f, ForceMode.Force);
        }

        // 7) 휠 메쉬(선택) — 콜라이더 포즈를 메쉬에 반영
        UpdateWheelPose(wheelFL, meshFL);
        UpdateWheelPose(wheelFR, meshFR);
        UpdateWheelPose(wheelRL, meshRL);
        UpdateWheelPose(wheelRR, meshRR);
    }

    void ApplyBrake(float torque)
    {
        wheelFL.brakeTorque = torque;
        wheelFR.brakeTorque = torque;
        wheelRL.brakeTorque = torque;
        wheelRR.brakeTorque = torque;
    }

    // 방향성을 반영한 마찰 보간
    void DriftFrictionUpdate(bool drifting, float steerInput)
    {
        float target = drifting ? 1f : 0f;
        driftT = Mathf.MoveTowards(driftT, target, driftBlendSpeed * Time.fixedDeltaTime);

        // 기본 목표: 앞은 비교적 안정, 뒤는 낮은 그립
        float frontLat = Mathf.Lerp(flLat0.stiffness, frontLatStiffnessInDrift, driftT);
        float rearLat  = Mathf.Lerp(rlLat0.stiffness, rearLatStiffnessInDrift, driftT);

        // 방향성: 우(+)면 바깥쪽은 왼쪽(RL), 좌(-)면 바깥쪽은 오른쪽(RR)
        bool rightTurn = steerInput > 0f;
        float rlStiff = rearLat * (rightTurn ? outsideRearGripScale : 1f);
        float rrStiff = rearLat * (rightTurn ? 1f : outsideRearGripScale);

        // 앞바퀴 동일 적용
        SetLatStiffness(wheelFL, flLat0, frontLat);
        SetLatStiffness(wheelFR, frLat0, frontLat);
        // 뒤바퀴 비대칭 적용
        SetLatStiffness(wheelRL, rlLat0, rlStiff);
        SetLatStiffness(wheelRR, rrLat0, rrStiff);

        // 진입 완화를 위해 뒤축 슬립 임계 살짝 증가
        float extMul = Mathf.Lerp(1f, 1.4f, driftT);
        float asymMul = Mathf.Lerp(1f, 1.4f, driftT);
        SetLatSlip(wheelRL, rlLat0, rlLat0.extremumSlip * extMul, rlLat0.asymptoteSlip * asymMul);
        SetLatSlip(wheelRR, rrLat0, rrLat0.extremumSlip * extMul, rrLat0.asymptoteSlip * asymMul);
    }

    void SetLatStiffness(WheelCollider wc, WheelFrictionCurve baseCurve, float stiffness)
    {
        var lat = baseCurve;
        lat.stiffness = stiffness;
        wc.sidewaysFriction = lat;
    }

    void SetLatSlip(WheelCollider wc, WheelFrictionCurve /*baseCurve*/ _, float extremumSlip, float asymptoteSlip)
    {
        var lat = wc.sidewaysFriction;
        lat.extremumSlip = extremumSlip;
        lat.asymptoteSlip = asymptoteSlip;
        wc.sidewaysFriction = lat;
    }

    void UpdateWheelPose(WheelCollider col, Transform mesh)
    {
        if (!col || !mesh) return;
        Vector3 pos; Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}