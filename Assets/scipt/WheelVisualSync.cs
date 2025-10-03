using UnityEngine;

public class WheelVisualSync : MonoBehaviour
{
    [System.Serializable]
    public class Wheel
    {
        public WheelCollider collider; // FR/FL/RR/RL의 WheelCollider
        public Transform mesh;         // 해당 바퀴의 메쉬 (Sport_Wheel_1)
        public Vector3 rotationOffset; // 메쉬의 축이 어긋날 때 보정 (deg)
    }

    public Wheel[] wheels;

    void LateUpdate()
    {
        foreach (var w in wheels)
        {
            if (w.collider == null || w.mesh == null) continue;

            w.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);

            // 위치/회전 동기화
            w.mesh.position = pos;
            w.mesh.rotation = rot * Quaternion.Euler(w.rotationOffset);
        }
    }
}