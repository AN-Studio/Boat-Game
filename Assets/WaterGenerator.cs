using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(MeshFilter), typeof(PolygonCollider2D))]
public partial class WaterGenerator : MonoBehaviour
{
    #region Settings
        [Header("Settings")]
        public Color waterColor;
        public float longitude;
        public int nodesPerUnit = 5;
        public float waterDepth;
        
        [Header("Physics")]
        [Range(0, 0.1f)] public float springConstant;
        [Range(0, 0.1f)] public float damping;
        [Range(0.0f, 0.5f)] public float spread;
    #endregion

    #region References
        [Header("References")]
        public LineRenderer surface;
        public Mesh mesh;
    #endregion

    #region Private Variables
        private List<WaterNode> nodes;
        private float positionDelta;
        private float massPerNode;
    #endregion

    #region MonoBehaviour Functions
        void Awake() 
        {
            surface = GetComponent<LineRenderer>();

            nodes = new List<WaterNode>();
            // nodes = new List<Vector2>();
            // velocities = new List<Vector2>();
            // accelerations = new List<Vector2>();
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
            DetectCollisions();
        }

        void FixedUpdate() 
        {
            ApplySpringForces();
            PropagateWaves();
            DrawBody();
        }
    #endregion

    void ComputeCoeficients()
    {
        positionDelta = 1f / nodesPerUnit;
        massPerNode = (1f / nodesPerUnit) * waterDepth;
    }

    void ApplySpringForces()
    {
        for (int i = 0; i < nodes.Count ; i++)
        {
            nodes[i].Update(springConstant, damping, massPerNode);
            surface.SetPosition(i, nodes[i].position);
        } 
    }

    void PropagateWaves()
    {
        Vector2[] leftDeltas = new Vector2[nodes.Count];
        Vector2[] rightDeltas = new Vector2[nodes.Count];
                    
        // do some passes where nodes pull on their neighbours
        for (int j = 0; j < 8; j++)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0)
                {
                    leftDeltas[i] = spread * (nodes[i].position - nodes[i - 1].position - Vector2.right * positionDelta);
                    nodes[i - 1].velocity.y += leftDeltas[i].y;
                }
                if (i < nodes.Count - 1)
                {
                    rightDeltas[i] = spread * (nodes[i].position - nodes[i + 1].position + Vector2.right * positionDelta);
                    nodes[i + 1].velocity.y += rightDeltas[i].y;
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0)
                    nodes[i - 1].position.y += leftDeltas[i].y * Time.fixedDeltaTime;
                if (i < nodes.Count - 1)
                    nodes[i + 1].position.y += rightDeltas[i].y * Time.fixedDeltaTime;
            }
        }
    }

    public void DetectCollisions()
    {
        LayerMask mask = LayerMask.GetMask("Default");
        foreach (WaterNode node in nodes)
        {
            Collider2D splasher = Physics2D.OverlapCircle(
                node.position + Vector2.down * positionDelta, 
                positionDelta, 
                mask
            );
            
            if (splasher != null)
            {
                float mass = splasher.attachedRigidbody.mass;
                Vector2 velocity = splasher.attachedRigidbody.velocity;

                node.Splash(mass * velocity, massPerNode);
            }
        }
    }

    #region Draw Functions
        void DrawSurfaceLine() 
        {
            int nodeAmount = ((int)(longitude * nodesPerUnit));
            positionDelta = 1f / nodesPerUnit;
            surface.positionCount = nodeAmount + 1;

            for (int count = 0; count <= nodeAmount; count++)
            {
                Vector2 position = (Vector2) transform.position + Vector2.right * (positionDelta * count);
                
                nodes.Add(new WaterNode(position));
                surface.SetPosition(count, position);
                
                // nodes.Add(transform.position + Vector2.right * (positionDelta * count));
                // velocities.Add(Vector2.zero);
                // accelerations.Add(Vector2.zero);
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
                    (Vector3) nodes[i].position - transform.position,
                    (Vector3) nodes[i].position - transform.position + Vector3.down * waterDepth,
                });

                // Add each node's position, relative to the gameObject position
                colliderPath.Add(nodes[i].position - (Vector2) transform.position);
                
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
