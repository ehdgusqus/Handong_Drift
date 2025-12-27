using PathCreation;
using UnityEngine;

[RequireComponent(typeof(PathCreator))]
public class RacingTrackGenerator : MonoBehaviour
{
    [Header("Track Dimensions")]
    [Tooltip("트랙 폭 (미터)")]
    public float trackWidth = 10f;
    
    [Tooltip("트랙 해상도 (높을수록 부드러움, 교차로는 300 이상 추천)")]
    [Range(10, 1000)]
    public int pathResolution = 200; 
    
    [Header("Curbs (커브)")]
    public bool addCurbs = true;
    public float curbWidth = 0.5f;
    public float curbHeight = 0.15f;
    
    [Header("Barriers (가드레일)")]
    public bool addBarriers = true;
    public float barrierHeight = 1.0f;
    public float barrierOffset = 0.5f;

    [Header("Cutout (교차로 진입로 설정)")]
    [Tooltip("체크하면 아래 지정된 거리 구간에는 커브와 가드레일이 생기지 않음.")]
    public bool useCutout = false;
    public float cutoutStartDistance = 0f;
    public float cutoutEndDistance = 10f;
    
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
        
        if (pathCreator == null) pathCreator = GetComponent<PathCreator>();
        if (pathCreator == null || pathCreator.path == null) return;

        trackObject = new GameObject("Generated_Track");
        trackObject.transform.parent = transform;
        trackObject.transform.localPosition = Vector3.zero;
        trackObject.transform.localRotation = Quaternion.identity;

        CreateTrackSurface();

        if (addCurbs)
        {
            CreateCurbsSide(-1, curbMaterialRed);
            CreateCurbsSide(1, curbMaterialWhite);
        }

        if (addBarriers)
        {
            CreateBarrierSide(-1);
            CreateBarrierSide(1);
        }

        Debug.Log("트랙 생성 완료!");
    }

    void ClearOldTrack()
    {
        Transform oldTrack = transform.Find("Generated_Track");
        if (oldTrack != null) DestroyImmediate(oldTrack.gameObject);
    }

    // 부모 오브젝트 기준 로컬 좌표로 변환
    Vector3 GetLocal(Vector3 worldPos) => transform.InverseTransformPoint(worldPos);

    void CreateTrackSurface()
    {
        GameObject obj = new GameObject("Track_Surface");
        obj.transform.parent = trackObject.transform;
        obj.transform.localPosition = Vector3.zero;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        obj.AddComponent<MeshCollider>();

        mr.material = trackMaterial != null ? trackMaterial : CreateDefaultMaterial(new Color(0.15f, 0.15f, 0.15f));

        VertexPath path = pathCreator.path;
        Vector3[] vertices = new Vector3[(pathResolution + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[pathResolution * 6];

        for (int i = 0; i <= pathResolution; i++)
        {
            float t = i / (float)pathResolution;
            float dist = t * path.length;

            Vector3 point = path.GetPointAtDistance(dist);
            Vector3 forward = path.GetDirectionAtDistance(dist);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            vertices[i * 2] = GetLocal(point - right * (trackWidth / 2f));
            vertices[i * 2 + 1] = GetLocal(point + right * (trackWidth / 2f));

            uvs[i * 2] = new Vector2(0, dist / trackWidth);
            uvs[i * 2 + 1] = new Vector2(1, dist / trackWidth);
        }

        for (int i = 0; i < pathResolution; i++)
        {
            int v = i * 2; int tri = i * 6;
            triangles[tri] = v; triangles[tri + 1] = v + 2; triangles[tri + 2] = v + 1;
            triangles[tri + 3] = v + 1; triangles[tri + 4] = v + 2; triangles[tri + 5] = v + 3;
        }

        Mesh mesh = new Mesh { name = "Track_Mesh", vertices = vertices, triangles = triangles, uv = uvs };
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        obj.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void CreateCurbsSide(int side, Material mat)
    {
        GameObject sideObj = new GameObject($"Curb_{(side > 0 ? "Right" : "Left")}");
        sideObj.transform.parent = trackObject.transform;
        sideObj.transform.localPosition = Vector3.zero;

        VertexPath path = pathCreator.path;
        // 개별 세그먼트로 생성하여 컷아웃 구현
        for (int i = 0; i < pathResolution; i++)
        {
            float d1 = (i / (float)pathResolution) * path.length;
            float d2 = ((i + 1) / (float)pathResolution) * path.length;

            if (useCutout && d1 >= cutoutStartDistance && d1 <= cutoutEndDistance) continue;

            CreateSimpleQuad(d1, d2, side, trackWidth / 2f, curbWidth, curbHeight, sideObj.transform, mat, "Curb_Seg");
        }
    }

    void CreateBarrierSide(int side)
    {
        GameObject sideObj = new GameObject($"Barrier_{(side > 0 ? "Right" : "Left")}");
        sideObj.transform.parent = trackObject.transform;
        sideObj.transform.localPosition = Vector3.zero;

        VertexPath path = pathCreator.path;
        float totalOffset = trackWidth / 2f + curbWidth + barrierOffset;

        for (int i = 0; i < pathResolution; i++)
        {
            float d1 = (i / (float)pathResolution) * path.length;
            float d2 = ((i + 1) / (float)pathResolution) * path.length;

            if (useCutout && d1 >= cutoutStartDistance && d1 <= cutoutEndDistance) continue;

            Vector3 p1 = path.GetPointAtDistance(d1);
            Vector3 p2 = path.GetPointAtDistance(d2);
            Vector3 r1 = Vector3.Cross(Vector3.up, path.GetDirectionAtDistance(d1)).normalized;
            Vector3 r2 = Vector3.Cross(Vector3.up, path.GetDirectionAtDistance(d2)).normalized;

            Vector3 b1 = p1 + r1 * totalOffset * side;
            Vector3 b2 = p2 + r2 * totalOffset * side;

            GameObject seg = new GameObject("Barrier_Seg");
            seg.transform.parent = sideObj.transform;
            MeshFilter mf = seg.AddComponent<MeshFilter>();
            MeshRenderer mr = seg.AddComponent<MeshRenderer>();
            seg.AddComponent<MeshCollider>();
            mr.material = barrierMaterial != null ? barrierMaterial : CreateDefaultMaterial(Color.gray);

            Vector3[] v = new Vector3[4];
            v[0] = GetLocal(b1); v[1] = GetLocal(b2);
            v[2] = GetLocal(b1 + Vector3.up * barrierHeight); v[3] = GetLocal(b2 + Vector3.up * barrierHeight);
            
            int[] t = (side < 0) ? new int[] { 0, 1, 2, 2, 1, 3 } : new int[] { 0, 2, 1, 1, 2, 3 };
            Mesh m = new Mesh { vertices = v, triangles = t };
            m.RecalculateNormals();
            mf.mesh = m;
            seg.GetComponent<MeshCollider>().sharedMesh = m;
        }
    }

    // 커브 쿼드 생성을 위한 헬퍼 함수
    void CreateSimpleQuad(float d1, float d2, int side, float startOffset, float width, float height, Transform parent, Material mat, string name)
    {
        VertexPath path = pathCreator.path;
        Vector3 p1 = path.GetPointAtDistance(d1);
        Vector3 p2 = path.GetPointAtDistance(d2);
        Vector3 r1 = Vector3.Cross(Vector3.up, path.GetDirectionAtDistance(d1)).normalized;
        Vector3 r2 = Vector3.Cross(Vector3.up, path.GetDirectionAtDistance(d2)).normalized;

        Vector3 in1 = p1 + r1 * startOffset * side;
        Vector3 out1 = in1 + r1 * width * side;
        Vector3 in2 = p2 + r2 * startOffset * side;
        Vector3 out2 = in2 + r2 * width * side;

        GameObject seg = new GameObject(name);
        seg.transform.parent = parent;
        MeshFilter mf = seg.AddComponent<MeshFilter>();
        seg.AddComponent<MeshRenderer>().material = mat;

        Vector3[] v = new Vector3[4];
        v[0] = GetLocal(in1 + Vector3.up * height); v[1] = GetLocal(out1 + Vector3.up * height);
        v[2] = GetLocal(in2 + Vector3.up * height); v[3] = GetLocal(out2 + Vector3.up * height);

        int[] t = (side < 0) ? new int[] { 0, 1, 2, 2, 1, 3 } : new int[] { 0, 2, 1, 1, 2, 3 };
        Mesh m = new Mesh { vertices = v, triangles = t };
        m.RecalculateNormals();
        mf.mesh = m;
    }

    Material CreateDefaultMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }
}