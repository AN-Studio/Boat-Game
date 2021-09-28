using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(MeshFilter), typeof(PolygonCollider2D))]
public class WaterGenerator : MonoBehaviour
{
    #region Settings
        [Header("Settings")]
        public Color waterColor;
        public float longitude;
        public int nodesPerUnit = 5;
        public float waterDepth;
        
        [Header("Physics")]
        public float springConstant;
        public float damping;
        [Range(0.0f, 0.5f)] public float spread;
    #endregion

    #region References
        [Header("References")]
        public LineRenderer surface;
        public Mesh mesh;
    #endregion

    #region Private Variables
        private List<WaterNode> nodes;
        // private List<Vector3> nodes;
        // private List<Vector3> velocities;
        // private List<Vector3> accelerations;
        private float positionDelta;
    #endregion

    #region MonoBehaviour Functions
        void Awake() 
        {
            surface = GetComponent<LineRenderer>();

            nodes = new List<WaterNode>();
            // nodes = new List<Vector3>();
            // velocities = new List<Vector3>();
            // accelerations = new List<Vector3>();
        }

        // Start is called before the first frame update
        void Start()
        {
            ComputeCoeficients();
            DrawSurfaceLine();
            DrawBody();
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void FixedUpdate() 
        {
            ApplySpringForces();
        }
    #endregion

    void ComputeCoeficients()
    {
        positionDelta = 1f / nodesPerUnit;
    }

    void ApplySpringForces()
    {
        for (int i = 0; i < nodes.Count ; i++)
        {
            nodes[i].Update(springConstant, damping);
            surface.SetPosition(i, nodes[i].position);
        } 

        // for (int i = 0; i < nodes.Count ; i++)
        // {
        //     Vector3 displacement = (nodes[i] - transform.position);
        //     accelerations[i] = -springConstant * displacement + velocities[i]*damping ;   
            
        //     nodes[i] += velocities[i];
        //     velocities[i] += accelerations[i];
        // }
        // surface.SetPositions(nodes.ToArray());

        DrawBody();
    }

    void PropagateWaves()
    {
        Vector3[] leftDeltas = new Vector3[nodes.Count];
        Vector3[] rightDeltas = new Vector3[nodes.Count];
                    
        // do some passes where nodes pull on their neighbours
        // for (int j = 0; j < 8; j++)
        // {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0)
                {
                    leftDeltas[i] = spread * (nodes[i].position - nodes[i - 1].position - Vector3.right * positionDelta);
                    nodes[i - 1].velocity += leftDeltas[i];
                }
                if (i < nodes.Count - 1)
                {
                    rightDeltas[i] = spread * (nodes[i].position - nodes[i + 1].position + Vector3.right * positionDelta);
                    nodes[i + 1].velocity += rightDeltas[i];
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0)
                    nodes[i - 1].position += leftDeltas[i];
                if (i < nodes.Count - 1)
                    nodes[i + 1].position += rightDeltas[i];
            }
        // }
    }

    public void Splash(int index, float speed)
    {
        if (index >= 0 && index < nodes.Count)
            nodes[index].velocity = Vector3.down * speed;
    }

    #region Draw Functions
        void DrawSurfaceLine() 
        {
            int nodeAmount = ((int)(longitude * nodesPerUnit));
            positionDelta = 1f / nodesPerUnit;
            surface.positionCount = nodeAmount + 1;

            for (int count = 0; count <= nodeAmount; count++)
            {
                Vector3 position = transform.position + Vector3.right * (positionDelta * count);
                
                nodes.Add(new WaterNode(position));
                surface.SetPosition(count, position);
                
                // nodes.Add(transform.position + Vector3.right * (positionDelta * count));
                // velocities.Add(Vector3.zero);
                // accelerations.Add(Vector3.zero);
            }

            // surface.SetPositions(nodes.ToArray());
            // surface.material.renderQueue = 1000;
        }

        void DrawBody()
        {
            mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> colliderPath = new List<Vector2>();
            List<int> triangles = new List<int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                // Weave the mesh by adding the nodes in pairs from left to right
                vertices.AddRange(new Vector3[]
                {
                    nodes[i].position - transform.position,
                    nodes[i].position - transform.position + Vector3.down * waterDepth,
                });

                // Add each node's position, relative to the gameObject position
                colliderPath.Add(nodes[i].position - transform.position);
                
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

            // Add the two last nodes that close the polygon properly, and that give it depth.
            colliderPath.Add(colliderPath[colliderPath.Count-1] + Vector2.down * waterDepth);
            colliderPath.Add(colliderPath[0] + Vector2.down * waterDepth);

            Color[] colors = new Color[vertices.ToArray().Length];
            for (int i = 0; i < vertices.ToArray().Length; i++)
                colors[i] = waterColor;

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors;

            mesh.RecalculateNormals();
            
            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<PolygonCollider2D>().SetPath(0, colliderPath);
            
        }
    #endregion

    #region Gizmos
        private void OnDrawGizmos() {
            Gizmos.color = waterColor;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * longitude);
            Gizmos.DrawCube(
                transform.position + Vector3.right * longitude/2 + Vector3.down * waterDepth/2,
                Vector3.right * longitude + Vector3.down * waterDepth    
            );
        }
    #endregion
}
