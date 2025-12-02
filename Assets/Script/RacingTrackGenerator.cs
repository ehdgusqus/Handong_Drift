using PathCreation;
using UnityEngine;

[RequireComponent(typeof(PathCreator))]
public class RacingTrackGenerator : MonoBehaviour
{
    [Header("Track Dimensions")]
    [Tooltip("트랙 폭 (미터)")]
    public float trackWidth = 10f;
    
    [Tooltip("트랙 해상도 (높을수록 부드러움)")]
    [Range(10, 200)]
    public int pathResolution = 50;
    
    [Header("Curbs (커브)")]
    [Tooltip("커브 추가")]
    public bool addCurbs = true;
    
    [Tooltip("커브 폭")]
    public float curbWidth = 0.5f;
    
    [Tooltip("커브 높이")]
    public float curbHeight = 0.15f;
    
    [Header("Barriers (가드레일)")]
    [Tooltip("가드레일 추가")]
    public bool addBarriers = true;
    
    [Tooltip("가드레일 높이")]
    public float barrierHeight = 1.0f;
    
    [Tooltip("가드레일 거리")]
    public float barrierOffset = 0.5f;
    
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
        
        // Path Creator 가져오기
        if (pathCreator == null)
        {
            pathCreator = GetComponent<PathCreator>();
        }
        
        // 유효성 검사
        if (pathCreator == null)
        {
            Debug.LogError("이 GameObject에 Path Creator가 없습니다!");
            return;
        }
        
        if (pathCreator.path == null)
        {
            Debug.LogError("경로가 생성되지 않았습니다! Scene에서 포인트를 추가하세요.");
            return;
        }

        // 트랙 생성 시작
        trackObject = new GameObject("Generated_Track");
        trackObject.transform.parent = transform;
        trackObject.transform.localPosition = Vector3.zero;

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

        Debug.Log("✅ 트랙 생성 완료!");
    }

    void ClearOldTrack()
    {
        Transform oldTrack = transform.Find("Generated_Track");
        if (oldTrack != null)
        {
            DestroyImmediate(oldTrack.gameObject);
        }
    }

    void CreateTrackSurface()
    {
        GameObject surfaceObj = new GameObject("Track_Surface");
        surfaceObj.transform.parent = trackObject.transform;

        MeshFilter meshFilter = surfaceObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = surfaceObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = surfaceObj.AddComponent<MeshCollider>();

        // 머티리얼 설정
        meshRenderer.material = trackMaterial != null ? trackMaterial : CreateDefaultMaterial(new Color(0.15f, 0.15f, 0.15f));

        VertexPath path = pathCreator.path;
        int pointCount = pathResolution;

        Vector3[] vertices = new Vector3[(pointCount + 1) * 2];
        int[] triangles = new int[pointCount * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i <= pointCount; i++)
        {
            float t = i / (float)pointCount;
            float distance = t * path.length;

            Vector3 point = path.GetPointAtDistance(distance);
            Vector3 forward = path.GetDirectionAtDistance(distance);
            // 수정: Right 벡터를 직접 계산
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            vertices[i * 2] = point - right * (trackWidth / 2f);
            vertices[i * 2 + 1] = point + right * (trackWidth / 2f);

            uvs[i * 2] = new Vector2(0, distance / trackWidth);
            uvs[i * 2 + 1] = new Vector2(1, distance / trackWidth);
        }

        for (int i = 0; i < pointCount; i++)
        {
            int vertIndex = i * 2;
            int triIndex = i * 6;

            triangles[triIndex] = vertIndex;
            triangles[triIndex + 1] = vertIndex + 2;
            triangles[triIndex + 2] = vertIndex + 1;

            triangles[triIndex + 3] = vertIndex + 1;
            triangles[triIndex + 4] = vertIndex + 2;
            triangles[triIndex + 5] = vertIndex + 3;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Track_Surface";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    void CreateCurbsSide(int side, Material curbMaterial)
    {
        string sideName = side > 0 ? "Right" : "Left";
        GameObject curbObj = new GameObject($"Curb_{sideName}");
        curbObj.transform.parent = trackObject.transform;

        MeshFilter meshFilter = curbObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = curbObj.AddComponent<MeshRenderer>();
        
        meshRenderer.material = curbMaterial != null ? curbMaterial : CreateDefaultMaterial(side > 0 ? Color.white : Color.red);

        VertexPath path = pathCreator.path;
        int pointCount = pathResolution;

        Vector3[] vertices = new Vector3[(pointCount + 1) * 4];
        int[] triangles = new int[pointCount * 12];

        for (int i = 0; i <= pointCount; i++)
        {
            float t = i / (float)pointCount;
            float distance = t * path.length;

            Vector3 point = path.GetPointAtDistance(distance);
            Quaternion rotation = path.GetRotationAtDistance(distance);
            Vector3 right = rotation * Vector3.right;

            Vector3 innerPoint = point + right * (trackWidth / 2f * side);
            Vector3 outerPoint = innerPoint + right * (curbWidth * side);

            vertices[i * 4] = innerPoint;
            vertices[i * 4 + 1] = outerPoint;
            vertices[i * 4 + 2] = innerPoint + Vector3.up * curbHeight;
            vertices[i * 4 + 3] = outerPoint + Vector3.up * curbHeight;
        }

        for (int i = 0; i < pointCount; i++)
        {
            int vertIndex = i * 4;
            int triIndex = i * 12;

            triangles[triIndex] = vertIndex + 2;
            triangles[triIndex + 1] = vertIndex + 6;
            triangles[triIndex + 2] = vertIndex + 3;
            triangles[triIndex + 3] = vertIndex + 3;
            triangles[triIndex + 4] = vertIndex + 6;
            triangles[triIndex + 5] = vertIndex + 7;

            triangles[triIndex + 6] = vertIndex + 1;
            triangles[triIndex + 7] = vertIndex + 5;
            triangles[triIndex + 8] = vertIndex + 3;
            triangles[triIndex + 9] = vertIndex + 3;
            triangles[triIndex + 10] = vertIndex + 5;
            triangles[triIndex + 11] = vertIndex + 7;
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Curb_{sideName}";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void CreateBarrierSide(int side)
    {
        string sideName = side > 0 ? "Right" : "Left";
        GameObject barrierObj = new GameObject($"Barrier_{sideName}");
        barrierObj.transform.parent = trackObject.transform;

        MeshFilter meshFilter = barrierObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = barrierObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = barrierObj.AddComponent<MeshCollider>();

        meshRenderer.material = barrierMaterial != null ? barrierMaterial : CreateDefaultMaterial(new Color(0.7f, 0.7f, 0.7f));

        VertexPath path = pathCreator.path;
        int pointCount = pathResolution;

        Vector3[] vertices = new Vector3[(pointCount + 1) * 2];
        int[] triangles = new int[pointCount * 6];

        float offset = (trackWidth / 2f + curbWidth + barrierOffset) * side;

        for (int i = 0; i <= pointCount; i++)
        {
            float t = i / (float)pointCount;
            float distance = t * path.length;

            Vector3 point = path.GetPointAtDistance(distance);
            Vector3 forward = path.GetDirectionAtDistance(distance);
            // 수정: Right 벡터를 직접 계산
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 basePoint = point + right * offset;

            vertices[i * 2] = basePoint;
            vertices[i * 2 + 1] = basePoint + Vector3.up * barrierHeight;
        }

        for (int i = 0; i < pointCount; i++)
        {
            int vertIndex = i * 2;
            int triIndex = i * 6;

            triangles[triIndex] = vertIndex;
            triangles[triIndex + 1] = vertIndex + 2;
            triangles[triIndex + 2] = vertIndex + 1;

            triangles[triIndex + 3] = vertIndex + 1;
            triangles[triIndex + 4] = vertIndex + 2;
            triangles[triIndex + 5] = vertIndex + 3;
        }

        Mesh mesh = new Mesh();
        mesh.name = $"Barrier_{sideName}";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    Material CreateDefaultMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Glossiness", 0.3f);
        return mat;
    }
}