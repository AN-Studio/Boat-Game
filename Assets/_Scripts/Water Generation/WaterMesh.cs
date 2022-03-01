using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LineRenderer), typeof(MeshFilter), typeof(PolygonCollider2D))]
public partial class WaterMesh : MonoBehaviour
{
    #region Settings
        [Header("Settings")]
        [FormerlySerializedAs("waterColor")] public Color upperWaterColor;
        public Color lowerWaterColor;
        public float longitude;
        public int nodesPerUnit = 5;
        public float waterDepth;
        public int despawnDistance = 5;
        [Min(1)] public int performanceFactor = 2;
        
        [Header("Physics")]
        [Range(0, 0.1f)] public float springConstant;
        [Range(0, 0.1f)] public float damping;
        [Range(0.0f, .5f)] public float spreadRatio;
        [Range(1,10)] public int spreadSpeed;
    #endregion

    #region References
        // [Header("References")]
        protected LineRenderer surface;
        protected Mesh mesh;
        protected ParticleSystem particles;
        protected AreaEffector2D effector;
    #endregion

    #region Private Variables
        protected List<WaterNode> nodes;
        
        protected float[] leftDeltas;
        protected float[] rightDeltas;

        protected Vector3[] meshVertices;
        protected Vector2[] colliderPath;
        protected int[] meshTriangles;
        protected Color[] meshColors;

        protected float positionDelta;
        protected float massPerNode;
        protected Queue<Collider2D> interactionQueue;
        protected float time = 0;

        protected Camera cam;

        protected const float standardDrag = 1.05f;
    #endregion

    #region MonoBehaviour Functions

        #region Collision Management
            private void OnTriggerEnter2D(Collider2D other) 
            {
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.position = other.transform.position - transform.position;

                // particles.Play();
                
                ReactToCollision(other);

                // Compute drag according to cross surface
                Vector2 normal = other.attachedRigidbody.velocity.normalized;
                float crossArea = (normal * other.bounds.size).magnitude;
                other.attachedRigidbody.drag = standardDrag * crossArea ;
            }

            void OnTriggerStay2D(Collider2D other) 
            {
                if (!interactionQueue.Contains(other) && other.gameObject.GetComponent<Joint2D>() == null)
                    interactionQueue.Enqueue(other);

                if (Mathf.Abs(other.attachedRigidbody.velocity.x) >= 1)
                {
                    ParticleSystem.ShapeModule shape = particles.shape;
                    shape.position = other.transform.position - transform.position;

                    // particles.Play();
                }

                Vector2 normal = other.attachedRigidbody.velocity.normalized;
                float crossArea = (normal * other.bounds.size).magnitude;
                other.attachedRigidbody.drag = standardDrag * crossArea ;
            }
            
            void OnTriggerExit2D(Collider2D other) 
            {
                // Reduce drag to air density levels, unless object is the player.
                if (!other.gameObject.CompareTag("Player"))
                {
                    Vector2 normal = other.attachedRigidbody.velocity.normalized;
                    float crossArea = (normal * other.bounds.size).magnitude;
                    other.attachedRigidbody.drag = .001f * standardDrag * crossArea;
                }    
            }
        #endregion

        void Awake() 
        {
            effector = GetComponent<AreaEffector2D>();
            particles = GetComponent<ParticleSystem>();
            surface = GetComponent<LineRenderer>();
            
            nodes = new List<WaterNode>();
            interactionQueue = new Queue<Collider2D>();
            cam = Camera.main;
        }

        // Start is called before the first frame update
        void Start()
        {
            ComputeCoeficients();
            InitializeMeshStructures();

            DrawBody();
        }   

        protected virtual void FixedUpdate() 
        {  
            ProcessInteractionQueue();
            
            ApplySpringForces();
            PropagateWaves();
            
            DrawBody();
        }
    #endregion

    #region Buoyancy Forces Computations
        void ComputeCoeficients()
        {
            positionDelta = 1f / nodesPerUnit;
            massPerNode = (1f / nodesPerUnit) * waterDepth;
        }
        void ProcessInteractionQueue()
        {
            while (interactionQueue.Count > 0)
            {
                Collider2D obj = interactionQueue.Dequeue();
                
                if (obj != null && !obj.gameObject.CompareTag("IgnoreWater"))
                {
                    SimulateBuoyancy(obj);
                }
            }
        }
        void SimulateBuoyancy(Collider2D other) 
        {
            Rigidbody2D rb = other.attachedRigidbody;
            PolygonCollider2D waterBody = GetComponent<PolygonCollider2D>();

            Vector2 center = rb.worldCenterOfMass;
            Vector2 size = Geometry.GetColliderSize(other);
            
            // Vertices are sorted clockwise
            List<Vector2> vertices = new List<Vector2>() {
                center + (size * (Vector2.up + Vector2.left) / 2).Rotate(rb.rotation),
                center + (size * (Vector2.up + Vector2.right) / 2).Rotate(rb.rotation),
                center + (size * (Vector2.down + Vector2.right) / 2).Rotate(rb.rotation),
                center + (size * (Vector2.down + Vector2.left) / 2).Rotate(rb.rotation)
            };

            // Find the corner with the highest Y value
            int upperCornerIndex = 0;
            for (int i = 0; i < 4; i++)
                if (vertices[i].y > vertices[upperCornerIndex].y) upperCornerIndex = i;

            // string debug = "Collider Vertices:\n";
            // foreach (var vertex in vertices)
            //     debug += $"{vertex}\n";
            // Debug.Log(debug);

            // Get ready to compute submerged volume
            float normalForceAngle = 90;
            float volume = 0;
            Vector2 centroid = rb.centerOfMass;
            var (leftNode,rightNode) = Geometry.FindClosestSegment(vertices[upperCornerIndex], nodes);

            var (nodeLL, _) = Geometry.FindClosestSegment(vertices[(upperCornerIndex+3)%4], nodes);
            var (_, nodeRR) = Geometry.FindClosestSegment(vertices[(upperCornerIndex+1)%4], nodes);

            float surfaceSlope = (nodeRR.position.y - nodeLL.position.y) / (nodeRR.position.x - nodeLL.position.x);
            float surfaceOffset = nodeRR.position.y - surfaceSlope * nodeRR.position.x;

            if (vertices.Any(vertex => vertex.y < surfaceSlope * vertex.x + surfaceOffset))
            {
                if (!vertices.All(vertex => vertex.y < surfaceSlope * vertex.x + surfaceOffset))
                {
                    // Compute the angle of the water's surface normal
                    normalForceAngle = (Mathf.Rad2Deg * Mathf.Atan(-1 / surfaceSlope)) % 360 ;
                    normalForceAngle = normalForceAngle < 0 ? 180 + normalForceAngle : normalForceAngle;
                    
                    // Add contact points between water & collider
                    var (p1,p2) = Geometry.FindIntersectionsOnSurface(vertices, rb.rotation, upperCornerIndex,nodes);

                    // Remove unsubmerged vertices
                    vertices.RemoveAll(vertex => vertex.y > surfaceSlope * vertex.x + surfaceOffset);

                    vertices.Insert(0, p1);
                    vertices.Insert(1, p2);
                }

                // Split the unsubmerged volume into triangles
                List<int> triangles = Geometry.SplitIntoTriangles(vertices);

                // Compute the submerged volume & its centroid
                centroid = Geometry.ComputeCentroid(vertices, triangles, out volume);
            }

            // debug = "Submerged Volume Vertices:\n";
            // foreach (var vertex in vertices)
            //     debug += $"{vertex}\n";
            // Debug.Log(debug);

            // Debug.Log($"Buoyancy Centroid: {centroid}\nSubmerged Volume: {volume}");

            float fluidDensity = 1f;
            Vector2 buoyancy = (-fluidDensity * Physics2D.gravity * volume).Rotate(normalForceAngle - 90);
            
            if (volume != 0 && !float.IsNaN(centroid.x) && !float.IsNaN(centroid.y))
                rb.AddForceAtPosition(buoyancy, centroid);

            // print($"SurfaceNormal: {normalForceAngle}");
            // print($"Buoyancy: {buoyancy}\nWeight: {Physics2D.gravity * rb.mass}");
            // print($"1/2 A Triangle Area: {ComputeTriangleArea(new Vector2(0,0),new Vector2(0,1),new Vector2(1,0))}");
        }
    #endregion

    #region Surface Control
        
        void ApplySpringForces()
        {
            for (int i = 0; i < nodes.Count ; i++)
            {
                // if (i < nodes.Count-1)
                    nodes[i].Update(springConstant, damping, massPerNode);
                surface.SetPosition(i, nodes[i].position);
            } 
        }
        void PropagateWaves()
        {
            // do some passes where nodes pull on their neighbours
            Vector2 position;
            for (int j = 0; j < spreadSpeed; j++)
            {
                for (int i = nodes.Count-1; i >= 0; i--)
                {
                    if (i > 0)
                    {
                        leftDeltas[i] = spreadRatio * (nodes[i].position.y - nodes[i - 1].position.y);
                        nodes[i - 1].velocity += leftDeltas[i];
                    }
                    if (i < nodes.Count - 1)
                    {
                        rightDeltas[i] = spreadRatio * (nodes[i].position.y - nodes[i + 1].position.y);
                        nodes[i + 1].velocity += rightDeltas[i];
                    }
                } 

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (i > 0)
                    {
                        position = nodes[i - 1].position;
                        position.y += leftDeltas[i] * Time.fixedDeltaTime;
                        nodes[i - 1].position = position;
                    }
                    if (i < nodes.Count - 1)
                    {
                        position = nodes[i + 1].position;
                        position.y += rightDeltas[i] * Time.fixedDeltaTime;
                        nodes[i + 1].position = position;
                    }
                }
            }
        }
        void ReactToCollision(Collider2D splasher)
        {
            int start = Mathf.FloorToInt((splasher.bounds.center.x - splasher.bounds.extents.x) - nodes[0].position.x) * nodesPerUnit;
            int end = Mathf.CeilToInt((splasher.bounds.center.x + splasher.bounds.extents.x) - nodes[0].position.x) * nodesPerUnit;

            start = start >= 0 ? start : 0;
            end = end < nodes.Count ? end : nodes.Count-1;

            LayerMask mask;
            if (gameObject.layer == LayerMask.NameToLayer("Back Water"))
                mask = LayerMask.GetMask("Back Entities");
            else 
                mask = LayerMask.GetMask("Default");

            float splasherMass = splasher.attachedRigidbody.mass;
            float massPerSplash = splasherMass / (end-start);
            Vector2 velocity = splasher.attachedRigidbody.velocity;

            for (int i = start; i <= end; i++)
            {
                bool splashed = Physics2D.OverlapCircle(
                    nodes[i].position + Vector2.down * positionDelta, 
                    positionDelta, 
                    mask
                );

                if (splashed) 
                    velocity.y += nodes[i].Splash(massPerSplash, velocity.y, massPerNode) * massPerSplash / splasher.attachedRigidbody.mass;
            }

            if (!float.IsNaN(velocity.x) && !float.IsNaN(velocity.y))
                splasher.attachedRigidbody.velocity = velocity;
        }
    #endregion

    #region Draw Functions
        
        void InitializeMeshStructures()
        {
            int nodeAmount = ((int)(longitude * nodesPerUnit)) + 1;

            leftDeltas = new float[nodeAmount];
            rightDeltas = new float[nodeAmount];

            mesh = new Mesh();

            meshVertices = new Vector3[2*nodeAmount];
            colliderPath = new Vector2[nodeAmount/performanceFactor+3];

            meshTriangles = new int[6*(nodeAmount)];
            for (int i=1; i < nodeAmount; i++)
            {
                meshTriangles[6*(i-1)]   = 0 + (i-1)*2;
                meshTriangles[6*(i-1)+1] = 2 + (i-1)*2;
                meshTriangles[6*(i-1)+2] = 1 + (i-1)*2;

                meshTriangles[6*(i-1)+3] = 2 + (i-1)*2;
                meshTriangles[6*(i-1)+4] = 3 + (i-1)*2;
                meshTriangles[6*(i-1)+5] = 1 + (i-1)*2;
            }

            meshColors = new Color[2*nodeAmount];
            for (int i=0; i<meshColors.Length; i++) 
                meshColors[i] = i % 2 == 0 ? upperWaterColor : lowerWaterColor;

            InitializeSurface();
        }
        void InitializeSurface() 
        {
            int nodeAmount = ((int)(longitude * nodesPerUnit));

            positionDelta = 1f / nodesPerUnit;
            surface.positionCount = nodeAmount + 1;

            List<Vector3> positions = new List<Vector3>();
            for (int count = 0; count <= nodeAmount / 2; count++)
            {
                Vector2 rightPosition = (Vector2) transform.position + Vector2.right * (positionDelta * count);
                Vector2 leftPosition = (Vector2) transform.position + Vector2.left * (positionDelta * count);
                
                nodes.Add(new WaterNode(rightPosition));
                positions.Add(rightPosition);

                if (count > 0)
                {
                    nodes.Insert(0, new WaterNode(leftPosition));
                    positions.Insert(0, rightPosition);
                }
            }
                surface.SetPositions(positions.ToArray());
        }
        void DrawBody()
        {
            Vector3 node;
            for (int i = 0; i < nodes.Count; i++)
            {
                // Weave the mesh by adding the nodes in pairs from left to right
                // First the upper node
                node = (Vector3) nodes[i].position - transform.position;
                meshVertices[2*i] = node;
                if (i % performanceFactor == 0) colliderPath[i / performanceFactor] = node;

                // Then the lower node
                node.y = transform.position.y - waterDepth;
                meshVertices[2*i+1] = node;
            }

            #if UNITY_EDITOR
                for (int i=0; i<meshColors.Length; i++) 
                    meshColors[i] = i % 2 == 0 ? upperWaterColor : lowerWaterColor;
            #endif

            // Add the two last nodes that close the polygon properly, and that give it depth.
            colliderPath[nodes.Count/performanceFactor] = meshVertices[2*nodes.Count-2];
            colliderPath[nodes.Count/performanceFactor+1] = meshVertices[2*nodes.Count-1];
            colliderPath[nodes.Count/performanceFactor+2] = meshVertices[1];

            mesh.Clear();
            mesh.vertices = meshVertices;
            mesh.triangles = meshTriangles;
            mesh.colors = meshColors;

            mesh.RecalculateNormals();
            
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 1; 
            
            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<PolygonCollider2D>().SetPath(0, colliderPath);
            
        }
    #endregion

    #region Gizmos
        private void OnDrawGizmosSelected() {
            #if UNITY_EDITOR
                Gizmos.color = upperWaterColor;
                Gizmos.DrawLine(
                    transform.position - Vector3.right * longitude/2, 
                    transform.position + Vector3.right * longitude/2);
                Gizmos.DrawCube(
                    transform.position + Vector3.down * waterDepth/2,
                    Vector3.right * longitude + Vector3.down * waterDepth    
                );
            #endif
        }
    #endregion
}
