using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrometeoCarController : MonoBehaviour
{
    [Space(20)]
    [Header("EXTERNAL INPUT")]
    public ArduinoInput arduinoInput;

    //CAR SETUP
    [Space(20)]
    [Header("CAR SETUP")]
    [Range(20, 190)]
    public int maxSpeed = 90;
    [Range(10, 120)]
    public int maxReverseSpeed = 45;
    [Range(1, 10)]
    public int accelerationMultiplier = 2;
    [Space(10)]
    [Range(10, 45)]
    public int maxSteeringAngle = 27;
    [Range(0.1f, 1f)]
    public float steeringSpeed = 0.5f;
    [Space(10)]
    [Range(100, 600)]
    public int brakeForce = 350;
    [Range(1, 10)]
    public int decelerationMultiplier = 2;
    [Range(1, 10)]
    public int handbrakeDriftMultiplier = 5;
    [Space(10)]
    public Vector3 bodyMassCenter;

    //WHEELS
    [Header("WHEELS")]
    public GameObject frontLeftMesh;
    public WheelCollider frontLeftCollider;
    public GameObject frontRightMesh;
    public WheelCollider frontRightCollider;
    public GameObject rearLeftMesh;
    public WheelCollider rearLeftCollider;
    public GameObject rearRightMesh;
    public WheelCollider rearRightCollider;

    //EFFECTS
    [Header("EFFECTS")]
    public bool useEffects = false;
    public ParticleSystem RLWParticleSystem;
    public ParticleSystem RRWParticleSystem;
    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;

    //UI
    [Header("UI")]
    public bool useUI = false;
    public ForzaHUD carSpeedText;

    //SOUNDS
    [Header("SOUNDS")]
    public bool useSounds = false;
    public AudioSource carEngineSound;
    public AudioSource tireScreechSound;
    float initialCarEngineSoundPitch;

    //CONTROLS
    [Header("TOUCH CONTROLS")]
    public bool useTouchControls = false;
    public GameObject throttleButton;
    PrometeoTouchInput throttlePTI;
    public GameObject reverseButton;
    PrometeoTouchInput reversePTI;
    public GameObject turnRightButton;
    PrometeoTouchInput turnRightPTI;
    public GameObject turnLeftButton;
    PrometeoTouchInput turnLeftPTI;
    public GameObject handbrakeButton;
    PrometeoTouchInput handbrakePTI;

    //CAR DATA
    [HideInInspector]
    public float carSpeed;
    [HideInInspector]
    public bool isDrifting;
    [HideInInspector]
    public bool isTractionLocked;

    //PRIVATE VARIABLES
    Rigidbody carRigidbody;
    float steeringAxis;
    float throttleAxis;
    float driftingAxis;
    float localVelocityX;
    float localVelocityZ;
    bool deceleratingCar;

    WheelFrictionCurve FLwheelFriction, FRwheelFriction, RLwheelFriction, RRwheelFriction;
    float FLWextremumSlip, FRWextremumSlip, RLWextremumSlip, RRWextremumSlip;

    void Start()
    {
        carRigidbody = gameObject.GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;

        SetupFrictionCurves();

        if (carEngineSound != null)
        {
            initialCarEngineSoundPitch = carEngineSound.pitch;
        }

        if (useUI)
        {
            InvokeRepeating("CarSpeedUI", 0f, 0.1f);
        }
        else if (!useUI)
        {
            if (carSpeedText != null) carSpeedText.UpdateHUD(0);
        }

        if (useSounds)
        {
            InvokeRepeating("CarSounds", 0f, 0.1f);
        }
        else if (!useSounds)
        {
            if (carEngineSound != null) carEngineSound.Stop();
            if (tireScreechSound != null) tireScreechSound.Stop();
        }

        if (!useEffects)
        {
            if (RLWParticleSystem != null) RLWParticleSystem.Stop();
            if (RRWParticleSystem != null) RRWParticleSystem.Stop();
            if (RLWTireSkid != null) RLWTireSkid.emitting = false;
            if (RRWTireSkid != null) RRWTireSkid.emitting = false;
        }

        if (useTouchControls)
        {
            if (throttleButton != null && reverseButton != null && turnRightButton != null && turnLeftButton != null && handbrakeButton != null)
            {
                throttlePTI = throttleButton.GetComponent<PrometeoTouchInput>();
                reversePTI = reverseButton.GetComponent<PrometeoTouchInput>();
                turnLeftPTI = turnLeftButton.GetComponent<PrometeoTouchInput>();
                turnRightPTI = turnRightButton.GetComponent<PrometeoTouchInput>();
                handbrakePTI = handbrakeButton.GetComponent<PrometeoTouchInput>();
            }
        }
    }

    void SetupFrictionCurves()
    {
        if (frontLeftCollider == null || frontRightCollider == null || rearLeftCollider == null || rearRightCollider == null) return;

        FLwheelFriction = new WheelFrictionCurve();
        FRwheelFriction = new WheelFrictionCurve();
        RLwheelFriction = new WheelFrictionCurve();
        RRwheelFriction = new WheelFrictionCurve();

        CopyFriction(frontLeftCollider, ref FLwheelFriction, out FLWextremumSlip);
        CopyFriction(frontRightCollider, ref FRwheelFriction, out FRWextremumSlip);
        CopyFriction(rearLeftCollider, ref RLwheelFriction, out RLWextremumSlip);
        CopyFriction(rearRightCollider, ref RRwheelFriction, out RRWextremumSlip);
    }

    void CopyFriction(WheelCollider wc, ref WheelFrictionCurve wfc, out float slip)
    {
        wfc.extremumSlip = wc.sidewaysFriction.extremumSlip;
        slip = wc.sidewaysFriction.extremumSlip;
        wfc.extremumValue = wc.sidewaysFriction.extremumValue;
        wfc.asymptoteSlip = wc.sidewaysFriction.asymptoteSlip;
        wfc.asymptoteValue = wc.sidewaysFriction.asymptoteValue;
        wfc.stiffness = wc.sidewaysFriction.stiffness;
    }

    void Update()
    {
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;

#if UNITY_6000_0_OR_NEWER
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;
#else
        localVelocityX = transform.InverseTransformDirection(carRigidbody.velocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.velocity).z;
#endif

        HandleInput();
        AnimateWheelMeshes();
    }

    public void CarSpeedUI()
    {
        if (useUI)
        {
            try
            {
                float absoluteCarSpeed = Mathf.Abs(carSpeed);
                carSpeedText.UpdateHUD(absoluteCarSpeed);
            }
            catch (Exception ex) { }
        }
    }

    public void CarSounds()
    {
        if (useSounds)
        {
            try
            {
                if (carEngineSound != null)
                {
#if UNITY_6000_0_OR_NEWER
                    float currentSpeed = carRigidbody.linearVelocity.magnitude;
#else
                    float currentSpeed = carRigidbody.velocity.magnitude;
#endif
                    float engineSoundPitch = initialCarEngineSoundPitch + (Mathf.Abs(currentSpeed) / 25f);
                    carEngineSound.pitch = engineSoundPitch;
                }

                if ((isDrifting) || (isTractionLocked && Mathf.Abs(carSpeed) > 12f))
                {
                    if (!tireScreechSound.isPlaying) tireScreechSound.Play();
                }
                else if ((!isDrifting) && (!isTractionLocked || Mathf.Abs(carSpeed) < 12f))
                {
                    tireScreechSound.Stop();
                }
            }
            catch (Exception ex) { }
        }
        else if (!useSounds)
        {
            if (carEngineSound != null && carEngineSound.isPlaying) carEngineSound.Stop();
            if (tireScreechSound != null && tireScreechSound.isPlaying) tireScreechSound.Stop();
        }
    }

    void HandleInput()
    {
        if (arduinoInput != null)
        {
            // 아두이노 입력 처리
            steeringAxis = arduinoInput.steerValue;
            ApplySteering(); // 속도별 제한 적용

            float accel = -arduinoInput.accelValue;

            if (accel > 0.1f)
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                GoForward();
            }
            else if (accel < -0.1f)
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                GoReverse();
            }
            else
            {
                ThrottleOff();
            }

            if (Mathf.Abs(accel) <= 0.1f && !arduinoInput.isBtnPressed && !deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }

            if (arduinoInput.isBtnPressed)
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                Handbrake();
            }
            else
            {
                RecoverTraction();
            }
        }
        else // ★★★ 키보드 컨트롤 (여기를 완전히 수정했습니다) ★★★
        {
            // [수정] 목표 방향(-1:좌, 0:중립, 1:우)을 명확하게 설정
            float targetSteer = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) targetSteer = -1f;
            else if (Input.GetKey(KeyCode.RightArrow)) targetSteer = 1f;

            // [핵심] 현재 핸들값에서 목표값(0)으로 부드럽게, 그러나 확실하게 이동
            // 이렇게 하면 키를 떼는 순간 targetSteer가 0이 되므로 핸들이 무조건 중앙으로 돌아옵니다.
            steeringAxis = Mathf.MoveTowards(steeringAxis, targetSteer, Time.deltaTime * 10f * steeringSpeed);
            
            // 바퀴에 각도 적용 (속도 제한 포함)
            ApplySteering();

            // 가속/감속 로직
            if (Input.GetKey(KeyCode.UpArrow))
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                GoForward();
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                GoReverse();
            }
            
            if (Input.GetKey(KeyCode.Space))
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                Handbrake();
            }
            if (Input.GetKeyUp(KeyCode.Space)) RecoverTraction();

            // 엑셀에서 발 뗐을 때
            if ((!Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.UpArrow))) ThrottleOff();

            // 자연스러운 감속
            if ((!Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.UpArrow)) && !Input.GetKey(KeyCode.Space) && !deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }
        }
    }

    // [중요] 조향 처리 함수 (키보드, 아두이노 공통 사용)
    public void ApplySteering()
    {
        // 속도가 빠를수록 핸들 꺾이는 각도를 줄임 (고속 주행 안정성)
        float speedFactor = Mathf.Clamp01(Mathf.Abs(carSpeed) / maxSpeed);
        float currentAngleLimit = Mathf.Lerp(maxSteeringAngle, maxSteeringAngle * 0.1f, speedFactor);

        var steeringAngle = steeringAxis * currentAngleLimit;

        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    void AnimateWheelMeshes()
    {
        if (frontLeftCollider && frontLeftMesh) UpdateWheelPose(frontLeftCollider, frontLeftMesh);
        if (frontRightCollider && frontRightMesh) UpdateWheelPose(frontRightCollider, frontRightMesh);
        if (rearLeftCollider && rearLeftMesh) UpdateWheelPose(rearLeftCollider, rearLeftMesh);
        if (rearRightCollider && rearRightMesh) UpdateWheelPose(rearRightCollider, rearRightMesh);
    }

    void UpdateWheelPose(WheelCollider _collider, GameObject _mesh)
    {
        Vector3 _pos;
        Quaternion _quat;
        _collider.GetWorldPose(out _pos, out _quat);
        _mesh.transform.position = _pos;
        _mesh.transform.rotation = _quat;
    }

    void ApplyMotorTorque(float torque)
    {
        frontLeftCollider.motorTorque = torque;
        frontRightCollider.motorTorque = torque;
        rearLeftCollider.motorTorque = torque;
        rearRightCollider.motorTorque = torque;
    }

    void ApplyBrakeTorque(float torque)
    {
        frontLeftCollider.brakeTorque = torque;
        frontRightCollider.brakeTorque = torque;
        rearLeftCollider.brakeTorque = torque;
        rearRightCollider.brakeTorque = torque;
    }

    public void GoForward()
    {
        CheckDrift();
        throttleAxis = throttleAxis + (Time.deltaTime * 3f);
        
        float maxInput = (arduinoInput != null) ? Mathf.Abs(arduinoInput.accelValue) : 1f;
        if (throttleAxis > maxInput) throttleAxis = maxInput;

        if (localVelocityZ < -1f) Brakes();
        else
        {
            if (Mathf.RoundToInt(carSpeed) < maxSpeed)
            {
                ApplyBrakeTorque(0);
                ApplyMotorTorque((accelerationMultiplier * 50f) * throttleAxis);
            }
            else ApplyMotorTorque(0);
        }
    }

    public void GoReverse()
    {
        CheckDrift();
        throttleAxis = throttleAxis - (Time.deltaTime * 3f);

        float maxInput = (arduinoInput != null) ? -Mathf.Abs(arduinoInput.accelValue) : -1f;
        if (throttleAxis < maxInput) throttleAxis = maxInput;

        if (localVelocityZ > 1f) Brakes();
        else
        {
            if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed)
            {
                ApplyBrakeTorque(0);
                ApplyMotorTorque((accelerationMultiplier * 50f) * throttleAxis);
            }
            else ApplyMotorTorque(0);
        }
    }

    public void ThrottleOff()
    {
        ApplyMotorTorque(0);
    }

    public void DecelerateCar()
    {
        CheckDrift();
        if (throttleAxis != 0f)
        {
            if (throttleAxis > 0f) throttleAxis -= Time.deltaTime * 10f;
            else if (throttleAxis < 0f) throttleAxis += Time.deltaTime * 10f;
            if (Mathf.Abs(throttleAxis) < 0.15f) throttleAxis = 0f;
        }

#if UNITY_6000_0_OR_NEWER
        carRigidbody.linearVelocity = carRigidbody.linearVelocity * (1f / (1f + (0.025f * decelerationMultiplier)));
        ApplyMotorTorque(0);
        if (carRigidbody.linearVelocity.magnitude < 0.25f)
        {
            carRigidbody.linearVelocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
        }
#else
        carRigidbody.velocity = carRigidbody.velocity * (1f / (1f + (0.025f * decelerationMultiplier)));
        ApplyMotorTorque(0);
        if (carRigidbody.velocity.magnitude < 0.25f)
        {
            carRigidbody.velocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
        }
#endif
    }

    public void Brakes()
    {
        ApplyBrakeTorque(brakeForce);
    }

    void CheckDrift()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f) isDrifting = true;
        else isDrifting = false;
        DriftCarPS();
    }

    public void Handbrake()
    {
        CancelInvoke("RecoverTraction");
        driftingAxis = driftingAxis + (Time.deltaTime);
        float secureStartingPoint = driftingAxis * FLWextremumSlip * handbrakeDriftMultiplier;

        if (secureStartingPoint < FLWextremumSlip) driftingAxis = FLWextremumSlip / (FLWextremumSlip * handbrakeDriftMultiplier);
        if (driftingAxis > 1f) driftingAxis = 1f;

        CheckDrift();

        if (driftingAxis < 1f) ApplyDriftFriction(driftingAxis);

        isTractionLocked = true;
        DriftCarPS();
    }

    void ApplyDriftFriction(float axisValue)
    {
        FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * axisValue;
        frontLeftCollider.sidewaysFriction = FLwheelFriction;
        FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * axisValue;
        frontRightCollider.sidewaysFriction = FRwheelFriction;
        RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * axisValue;
        rearLeftCollider.sidewaysFriction = RLwheelFriction;
        RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * axisValue;
        rearRightCollider.sidewaysFriction = RRwheelFriction;
    }

    void ApplyDefaultFriction()
    {
        FLwheelFriction.extremumSlip = FLWextremumSlip;
        frontLeftCollider.sidewaysFriction = FLwheelFriction;
        FRwheelFriction.extremumSlip = FRWextremumSlip;
        frontRightCollider.sidewaysFriction = FRwheelFriction;
        RLwheelFriction.extremumSlip = RLWextremumSlip;
        rearLeftCollider.sidewaysFriction = RLwheelFriction;
        RRwheelFriction.extremumSlip = RRWextremumSlip;
        rearRightCollider.sidewaysFriction = RRwheelFriction;
    }

    public void DriftCarPS()
    {
        if (useEffects)
        {
            if (RLWParticleSystem && RRWParticleSystem)
            {
                if (isDrifting && !RLWParticleSystem.isPlaying)
                {
                    RLWParticleSystem.Play();
                    RRWParticleSystem.Play();
                }
                else if (!isDrifting && RLWParticleSystem.isPlaying)
                {
                    RLWParticleSystem.Stop();
                    RRWParticleSystem.Stop();
                }
            }

            if (RLWTireSkid && RRWTireSkid)
            {
                if ((isTractionLocked || Mathf.Abs(localVelocityX) > 5f) && Mathf.Abs(carSpeed) > 12f)
                {
                    RLWTireSkid.emitting = true;
                    RRWTireSkid.emitting = true;
                }
                else
                {
                    RLWTireSkid.emitting = false;
                    RRWTireSkid.emitting = false;
                }
            }
        }
        else 
        {
            if (RLWParticleSystem) RLWParticleSystem.Stop();
            if (RRWParticleSystem) RRWParticleSystem.Stop();
            if (RLWTireSkid) RLWTireSkid.emitting = false;
            if (RRWTireSkid) RRWTireSkid.emitting = false;
        }
    }

    public void RecoverTraction()
    {
        isTractionLocked = false;
        driftingAxis = driftingAxis - (Time.deltaTime / 1.5f);
        if (driftingAxis < 0f) driftingAxis = 0f;

        if (FLwheelFriction.extremumSlip > FLWextremumSlip)
        {
            ApplyDriftFriction(driftingAxis);
            Invoke("RecoverTraction", Time.deltaTime);
        }
        else if (FLwheelFriction.extremumSlip < FLWextremumSlip)
        {
            ApplyDefaultFriction();
            driftingAxis = 0f;
        }
    }
}