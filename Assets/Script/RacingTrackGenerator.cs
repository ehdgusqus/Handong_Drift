using PathCreation;
using UnityEngine;

[RequireComponent(typeof(PathCreator))]
public class RacingTrackGenerator : MonoBehaviour
{
    [Header("Track Dimensions")]
    public float trackWidth = 10f;
    
    [Range(10, 200)]
    public int pathResolution = 50;
    
    [Header("Curbs")]
    public bool addCurbs = true;
    public float curbWidth = 0.5f;
    public float curbHeight = 0.1f;
    
    [Header("Barriers")]
    public bool addBarriers = true;
    public float barrierHeight = 1.0f;
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
        
        // 여기가 수정된 부분!
        if (pathCreator == null)
        {
            pathCreator = GetComponent<PathCreator>();
        }
        
        if (pathCreator == null)
        {
            Debug.LogError("이 GameObject에 Path Creator 컴포넌트가 없습니다!");
            return;
        }
        
        if (pathCreator.path == null)
        {
            Debug.LogError("Path Creator에 경로가 없습니다!");
            return;
        }

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

        Debug.Log("트랙 생성 완료!");
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

        meshRenderer.material = trackMaterial != null ? trackMaterial : CreateDefaultMaterial(new Color(0.2f, 0.2f, 0.2f));

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
            Quaternion rotation = path.GetRotationAtDistance(distance);
            Vector3 right = rotation * Vector3.right;

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
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

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

        meshRenderer.material = barrierMaterial != null ? barrierMaterial : CreateDefaultMaterial(Color.gray);

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
            Quaternion rotation = path.GetRotationAtDistance(distance);
            Vector3 right = rotation * Vector3.right;

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