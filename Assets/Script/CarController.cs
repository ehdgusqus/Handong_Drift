using UnityEngine;
using TMPro;

public class CarController : MonoBehaviour
{
    [Header("Arduino Connection")]
    // ★ 1. 아두이노 스크립트를 연결할 변수 추가
    public ArduinoInput arduinoInput; 

    [Header("References")]
    public Rigidbody rb;
    public WheelCollider wheelFL, wheelFR, wheelRL, wheelRR;
    public Transform meshFL, meshFR, meshRL, meshRR;
    public TextMeshProUGUI speedText;

    [Header("Engine & Transmission")]
    public float motorForce = 500f;
    public AnimationCurve torqueCurve = new AnimationCurve(
        new Keyframe(1000f, 200f),
        new Keyframe(3000f, 380f),
        new Keyframe(6000f, 350f),
        new Keyframe(7500f, 280f)
    );
    public float maxRPM = 7500f;

    public float[] gearRatios = { 3.3f, 2.1f, 1.5f, 1.2f, 1.0f, 0.85f };
    public float finalDrive = 3.6f;
    private int currentGear = 0;

    [Header("Steering & Handling")]
    public float maxSteerAngle = 28f;
    public float downforce = 0.05f;

    [Header("Friction Settings")]
    public float forwardStiffness = 2.0f;
    public float sidewaysStiffness = 1.8f;

    [Header("Brakes")]
    public float brakeForce = 5000f;
    public float rearBrakeRatio = 0.7f;

    private float throttleInput; // -1 ~ 1
    private float steerInput;
    private bool braking;

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        ApplyFriction(forwardStiffness, sidewaysStiffness);
    }

    void Update()
    {
        // ★ 2. 입력 방식 변경 (아두이노 우선, 없으면 키보드)
        if (arduinoInput != null)
        {
            // 아두이노 값 받아오기
            // steerValue: -1(좌) ~ 1(우)
            // accelValue: -1(아래) ~ 1(위) -> 조이스틱 방향에 따라 부호를 바꿔야 할 수도 있음
            
            steerInput = arduinoInput.steerValue;
            
            // 조이스틱 Y축을 악셀로 사용 (-1:후진, 1:전진)
            // 만약 위로 밀었는데 후진하면 부호(-)를 붙이세요: -arduinoInput.accelValue
            throttleInput = arduinoInput.accelValue; 

            // 조이스틱 버튼(꾹 누름)을 브레이크로 사용
            braking = arduinoInput.isBtnPressed;
        }
        else
        {
            // 아두이노 연결 안 했을 때 비상용 (키보드)
            throttleInput = 0f;
            if (Input.GetKey(KeyCode.W)) throttleInput = 1f;
            if (Input.GetKey(KeyCode.S)) throttleInput = -1f;
            braking = Input.GetKey(KeyCode.Space);
            steerInput = Input.GetAxis("Horizontal");
        }

        // 속도 UI (유니티 버전에 따라 rb.linearVelocity 혹은 rb.velocity 사용)
        // 작성해주신 코드 원본인 linearVelocity 유지
        float spd = rb.linearVelocity.magnitude * 3.6f; 
        if (speedText != null)
            speedText.text = Mathf.RoundToInt(spd) + " km/h";
    }

    void FixedUpdate()
    {
        // Steering
        float steerAngle = steerInput * maxSteerAngle;
        wheelFL.steerAngle = steerAngle;
        wheelFR.steerAngle = steerAngle;

        // Wheel RPM & Engine RPM
        float wheelRpm = (wheelRL.rpm + wheelRR.rpm) * 0.5f;
        float engineRpm = Mathf.Clamp(Mathf.Abs(wheelRpm) * finalDrive * gearRatios[currentGear], 1000f, maxRPM);

        // Auto gear shifting
        if (engineRpm > maxRPM * 0.95f && currentGear < gearRatios.Length - 1) currentGear++;
        if (engineRpm < 2000f && currentGear > 0) currentGear--;

        // Engine Torque
        float engineTorque = torqueCurve.Evaluate(engineRpm);
        float wheelTorque = engineTorque * motorForce * gearRatios[currentGear] * finalDrive * throttleInput;

        // 후륜 구동
        wheelRL.motorTorque = wheelTorque * 0.5f;
        wheelRR.motorTorque = wheelTorque * 0.5f;
        wheelFL.motorTorque = 0f;
        wheelFR.motorTorque = 0f;

        // 브레이크
        if (braking)
        {
            wheelFL.brakeTorque = brakeForce;
            wheelFR.brakeTorque = brakeForce;
            wheelRL.brakeTorque = brakeForce * rearBrakeRatio;
            wheelRR.brakeTorque = brakeForce * rearBrakeRatio;
        }
        else
        {
            wheelFL.brakeTorque = 0f;
            wheelFR.brakeTorque = 0f;
            wheelRL.brakeTorque = 0f;
            wheelRR.brakeTorque = 0f;
        }

        // 다운포스
        rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);

        // 휠 메쉬 업데이트
        UpdateWheelPose(wheelFL, meshFL);
        UpdateWheelPose(wheelFR, meshFR);
        UpdateWheelPose(wheelRL, meshRL);
        UpdateWheelPose(wheelRR, meshRR);
    }

    void UpdateWheelPose(WheelCollider col, Transform mesh)
    {
        Vector3 pos; Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    void ApplyFriction(float forward, float sideways)
    {
        WheelFrictionCurve f;

        f = wheelFL.forwardFriction; f.stiffness = forward; wheelFL.forwardFriction = f;
        f = wheelFR.forwardFriction; f.stiffness = forward; wheelFR.forwardFriction = f;
        f = wheelRL.forwardFriction; f.stiffness = forward; wheelRL.forwardFriction = f;
        f = wheelRR.forwardFriction; f.stiffness = forward; wheelRR.forwardFriction = f;

        f = wheelFL.sidewaysFriction; f.stiffness = sideways; wheelFL.sidewaysFriction = f;
        f = wheelFR.sidewaysFriction; f.stiffness = sideways; wheelFR.sidewaysFriction = f;
        f = wheelRL.sidewaysFriction; f.stiffness = sideways; wheelRL.sidewaysFriction = f;
        f = wheelRR.sidewaysFriction; f.stiffness = sideways; wheelRR.sidewaysFriction = f;
    }
}