using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(MeshFilter), typeof(MeshCollider))]
public class WaterGenerator : MonoBehaviour
{
    public Color waterColor;

    public float longitude;
    public int nodesPerUnit = 5;

    public LineRenderer surface;
    public Queue<Vector3> nodes;
    public Queue<float> velocities;
    public Queue<float> accelerations;

    public Mesh mesh;
    public float waterDepth;


    void Awake() 
    {
        surface = GetComponent<LineRenderer>();

        nodes = new Queue<Vector3>();
        velocities = new Queue<float>();
        accelerations = new Queue<float>();
    }

    // Start is called before the first frame update
    void Start()
    {
        DrawSurface();
        DrawBody();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void DrawSurface() 
    {
        int nodeAmount = ((int)(longitude * nodesPerUnit));
        float positionDelta = 1f / nodesPerUnit;
        surface.positionCount = nodeAmount + 1;

        for (int count = 0; count <= nodeAmount; count++)
        {
            nodes.Enqueue(transform.position + Vector3.right * (positionDelta * count));
            velocities.Enqueue(0f);
            accelerations.Enqueue(0f);
        }

        surface.SetPositions(nodes.ToArray());
        // surface.material.renderQueue = 1000;
    }

    void DrawBody()
    {
        float positionDelta = 1f / nodesPerUnit;
        Vector3[] nodeArray = nodes.ToArray();
        mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int i = 0; i < nodeArray.Length; i++)
        {
            vertices.AddRange(new Vector3[]
            {
                Vector3.right * (positionDelta * i),
                Vector3.right * (positionDelta * i) + Vector3.down * waterDepth,
            });
            
            if (i > 0)
                triangles.AddRange(new int[] 
                {
                    0 + (i-1)*2,
                    2 + (i-1)*2,
                    1 + (i-1)*2, 
                    
                    2 + (i-1)*2,
                    3 + (i-1)*2,
                    1 + (i-1)*2
                });
        }

        Color[] colors = new Color[vertices.ToArray().Length];
        for (int i = 0; i < vertices.ToArray().Length; i++)
            colors[i] = waterColor;

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors;

        mesh.RecalculateNormals();
        

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        
    }

    private void OnDrawGizmos() {
        Gizmos.color = waterColor;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * longitude);
        Gizmos.DrawCube(
            transform.position + Vector3.right * longitude/2 + Vector3.down * waterDepth/2,
            Vector3.right * longitude + Vector3.down * waterDepth    
        );
    }
}
