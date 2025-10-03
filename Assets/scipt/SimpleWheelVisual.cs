using UnityEngine;

public class SimpleWheelVisual : MonoBehaviour
{
    [System.Serializable]
    public class Axle
    {
        public WheelCollider collider;
        public Transform mesh; // Sport_Wheel_1 트랜스폼
        public bool steer;
        public bool motor;
    }

    public Axle[] axles;
    public float maxSteerAngle = 30f;
    public float motorTorque = 300f;

    void FixedUpdate()
    {
        float steer = Input.GetAxis("Horizontal") * maxSteerAngle;
        float throttle = Input.GetAxis("Vertical") * motorTorque;

        foreach (var axle in axles)
        {
            if (axle.steer) axle.collider.steerAngle = steer;
            if (axle.motor) axle.collider.motorTorque = throttle;

            // 메쉬 동기화
            axle.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            axle.mesh.position = pos;
            axle.mesh.rotation = rot;
        }
    }
}