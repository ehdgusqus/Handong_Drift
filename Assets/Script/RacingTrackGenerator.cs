using PathCreation;
using UnityEngine;

[RequireComponent(typeof(PathCreator))]
public class RacingTrackGenerator : MonoBehaviour
{
    [Header("Track Dimensions")]
    public float trackWidth = 10f;
    [Range(10, 500)] public int pathResolution = 100;

    [Header("Curbs (커브)")]
    public bool addCurbs = true;
    public float curbWidth = 0.5f;
    public float curbHeight = 0.1f;

    [Header("Barriers (가드레일)")]
    public bool addBarriers = true;
    public bool leftBarrier = true;   
    public bool rightBarrier = true;  
    public float barrierHeight = 1.0f;
    public float barrierOffset = 0.2f;

    [Header("Materials")]
    public Material trackMaterial;
    public Material curbMaterialRed;
    public Material curbMaterialWhite;
    public Material barrierMaterial;

    private PathCreator pathCreator;
    private GameObject trackObject;

    [ContextMenu("Generate Track")]
    public void GenerateTrack()
    {
        ClearOldTrack();
        pathCreator = GetComponent<PathCreator>();
        if (pathCreator == null || pathCreator.path == null) return;

        // 트랙 부모 생성 및 초기화
        trackObject = new GameObject("Generated_Track");
        trackObject.transform.parent = transform;
        trackObject.transform.localPosition = Vector3.zero;
        trackObject.transform.localRotation = Quaternion.identity;
        trackObject.transform.localScale = Vector3.one;

        CreateTrackSurface();

        if (addCurbs)
        {
            CreateCurbsSide(-1, curbMaterialRed);
            CreateCurbsSide(1, curbMaterialWhite);
        }

        if (addBarriers)
        {
            if (leftBarrier) CreateBarrierSide(-1);
            if (rightBarrier) CreateBarrierSide(1);
        }
        Debug.Log("✅ 트랙 생성 완료!");
    }

    void ClearOldTrack()
    {
        Transform oldTrack = transform.Find("Generated_Track");
        if (oldTrack != null) DestroyImmediate(oldTrack.gameObject);
    }

    // 월드 좌표를 부모 기준 로컬 좌표로 변환 (공중 부양 방지)
    Vector3 WorldToLocal(Vector3 worldPos) => transform.InverseTransformPoint(worldPos);

    void CreateTrackSurface()
    {
        GameObject obj = CreateMeshObject("Track_Surface", trackMaterial, true);
        VertexPath path = pathCreator.path;
        
        Vector3[] verts = new Vector3[(pathResolution + 1) * 2];
        int[] tris = new int[pathResolution * 6];
        Vector2[] uvs = new Vector2[verts.Length];

        for (int i = 0; i <= pathResolution; i++)
        {
            // 거리 기반으로 위치와 방향을 가져옴
            float distance = (i / (float)pathResolution) * path.length;
            
            Vector3 point = path.GetPointAtDistance(distance);
            Vector3 normal = path.GetNormalAtDistance(distance);
            Vector3 forward = path.GetDirectionAtDistance(distance);
            Vector3 right = Vector3.Cross(forward, normal).normalized;

            verts[i * 2] = WorldToLocal(point - right * (trackWidth / 2f));
            verts[i * 2 + 1] = WorldToLocal(point + right * (trackWidth / 2f));

            uvs[i * 2] = new Vector2(0, distance / trackWidth);
            uvs[i * 2 + 1] = new Vector2(1, distance / trackWidth);
        }

        for (int i = 0; i < pathResolution; i++)
        {
            int v = i * 2; int t = i * 6;
            tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
            tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
        }
        FillMesh(obj, verts, tris, uvs);
    }

    void CreateCurbsSide(int side, Material mat)
    {
        GameObject obj = CreateMeshObject($"Curb_{side}", mat, false);
        VertexPath path = pathCreator.path;
        Vector3[] verts = new Vector3[(pathResolution + 1) * 4];
        int[] tris = new int[pathResolution * 18];

        for (int i = 0; i <= pathResolution; i++)
        {
            float distance = (i / (float)pathResolution) * path.length;
            Vector3 point = path.GetPointAtDistance(distance);
            Vector3 normal = path.GetNormalAtDistance(distance);
            Vector3 forward = path.GetDirectionAtDistance(distance);
            Vector3 right = Vector3.Cross(forward, normal).normalized;

            Vector3 innerBase = point + right * (trackWidth / 2f * side);
            Vector3 outerBase = innerBase + right * (curbWidth * side);

            verts[i * 4] = WorldToLocal(innerBase);
            verts[i * 4 + 1] = WorldToLocal(outerBase);
            verts[i * 4 + 2] = WorldToLocal(innerBase + Vector3.up * curbHeight);
            verts[i * 4 + 3] = WorldToLocal(outerBase + Vector3.up * curbHeight);
        }

        for (int i = 0; i < pathResolution; i++)
        {
            int v = i * 4; int t = i * 18;
            tris[t] = v + 2; tris[t + 1] = v + 6; tris[t + 2] = v + 3;
            tris[t + 3] = v + 3; tris[t + 4] = v + 6; tris[t + 5] = v + 7;
            tris[t + 6] = v + 1; tris[t + 7] = v + 3; tris[t + 8] = v + 5;
            tris[t + 9] = v + 5; tris[t + 10] = v + 3; tris[t + 11] = v + 7;
            tris[t + 12] = v; tris[t + 13] = v + 4; tris[t + 14] = v + 2;
            tris[t + 15] = v + 2; tris[t + 16] = v + 4; tris[t + 17] = v + 6;
        }
        FillMesh(obj, verts, tris);
    }

    void CreateBarrierSide(int side)
    {
        GameObject obj = CreateMeshObject($"Barrier_{side}", barrierMaterial, true);
        VertexPath path = pathCreator.path;
        Vector3[] verts = new Vector3[(pathResolution + 1) * 2];
        int[] tris = new int[pathResolution * 6];

        for (int i = 0; i <= pathResolution; i++)
        {
            float distance = (i / (float)pathResolution) * path.length;
            Vector3 point = path.GetPointAtDistance(distance);
            Vector3 normal = path.GetNormalAtDistance(distance);
            Vector3 forward = path.GetDirectionAtDistance(distance);
            Vector3 right = Vector3.Cross(forward, normal).normalized;

            Vector3 basePoint = point + right * ((trackWidth / 2f + curbWidth + barrierOffset) * side);
            verts[i * 2] = WorldToLocal(basePoint);
            verts[i * 2 + 1] = WorldToLocal(basePoint + Vector3.up * barrierHeight);
        }

        for (int i = 0; i < pathResolution; i++)
        {
            int v = i * 2; int t = i * 6;
            if (side < 0) {
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                tris[t + 3] = v + 1; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
            } else {
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
        }
        FillMesh(obj, verts, tris);
    }

    GameObject CreateMeshObject(string name, Material mat, bool addCollider)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = trackObject.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.AddComponent<MeshFilter>();
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.material = mat != null ? mat : new Material(Shader.Find("Standard"));
        if (addCollider) obj.AddComponent<MeshCollider>();
        return obj;
    }

    void FillMesh(GameObject obj, Vector3[] verts, int[] tris, Vector2[] uvs = null)
    {
        Mesh mesh = new Mesh { name = obj.name, vertices = verts, triangles = tris };
        if (uvs != null) mesh.uv = uvs;
        mesh.RecalculateNormals();
        obj.GetComponent<MeshFilter>().mesh = mesh;
        if (obj.TryGetComponent<MeshCollider>(out var col)) col.sharedMesh = mesh;
    }
}