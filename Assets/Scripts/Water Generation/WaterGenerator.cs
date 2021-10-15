using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(MeshFilter), typeof(PolygonCollider2D))]
public partial class WaterGenerator : MonoBehaviour
{
    #region Singleton
        static WaterGenerator instance;
        public static WaterGenerator Instance {
            get => instance;
        }
    #endregion

    #region Settings
        [Header("Settings")]
        public Color waterColor;
        public float longitude;
        public int nodesPerUnit = 5;
        public float waterDepth;
        public int waveIntensity = 10;
        public int despawnDistance = 5;
        
        [Header("Physics")]
        [Range(0, 0.1f)] public float springConstant;
        [Range(0, 0.1f)] public float damping;
        [Range(0.0f, 1f)] public float spread;
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
        private Queue<Collider2D> interactionQueue;
        private System.Random random;
        private float time = 0;
    #endregion

    #region MonoBehaviour Functions

        void OnTriggerStay2D(Collider2D other) 
        {
            if (!interactionQueue.Contains(other))
                interactionQueue.Enqueue(other);
        }

        void Awake() 
        {
            if (instance is null)
                instance = this;

            surface = GetComponent<LineRenderer>();
            nodes = new List<WaterNode>();
            interactionQueue = new Queue<Collider2D>();
            random = new System.Random();
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
            CheckCameraBounds();
            GenerateWaves();
        }

        void FixedUpdate() 
        {
            ProcessInteractionQueue();
            ReactToCollisions();
            ApplySpringForces();
            PropagateWaves();
            DrawBody();
        }
    #endregion

    #region Buoyancy Forces Computations
        void ProcessInteractionQueue()
        {
            while (interactionQueue.Count > 0)
            {
                Collider2D obj = interactionQueue.Dequeue();
                
                if (!obj.attachedRigidbody.freezeRotation)
                {
                    AccuratePhysics(obj);
                }
                else
                {
                    SimplifiedPhysics(obj);
                }
            }
        }
        void SimplifiedPhysics(Collider2D other)
        {
            Rigidbody2D rb = other.attachedRigidbody;
            PolygonCollider2D waterBody = GetComponent<PolygonCollider2D>();

            Vector2 center = rb.worldCenterOfMass;
            Vector2 size = GetColliderSize(other);
            
            List<Vector2> centroids = new List<Vector2>() {
                center,
                center + RotateVector(size * (Vector2.up) / 4, rb.rotation),
                center + RotateVector(size * (Vector2.left) / 4, rb.rotation),
                center + RotateVector(size * (Vector2.right) / 4, rb.rotation),
                center + RotateVector(size * (Vector2.down) / 4, rb.rotation),
                center + RotateVector(size * (Vector2.up + Vector2.left) / 4, rb.rotation),
                center + RotateVector(size * (Vector2.up + Vector2.right) / 4, rb.rotation),
                center + RotateVector(size * (Vector2.down + Vector2.right) / 4, rb.rotation),
                center + RotateVector(size * (Vector2.down + Vector2.left) / 4, rb.rotation)
            };

            float volume = 0;
            float volumePerDivision = size.x * size.y / centroids.Count;
            foreach (Vector2 centroid in centroids)
            {
                if (waterBody.OverlapPoint(centroid))
                    volume += volumePerDivision;
            }

            float fluidDensity = 1f;
            float dragCoefficient = .38f;
            float crossSection = rb.velocity.y > 0 ? other.bounds.size.x : other.bounds.size.y; // this one might need a better solution

            Vector2 buoyancy = -fluidDensity * Physics2D.gravity * volume;
            float drag = .5f * rb.velocity.sqrMagnitude * dragCoefficient * crossSection;

            rb.AddForce(-drag * rb.velocity.normalized);
            rb.AddForce(buoyancy);
        }
        void AccuratePhysics(Collider2D other) 
        {
            Rigidbody2D rb = other.attachedRigidbody;
            PolygonCollider2D waterBody = GetComponent<PolygonCollider2D>();

            Vector2 center = rb.worldCenterOfMass;
            Vector2 size = GetColliderSize(other);
            
            List<Vector2> vertices = new List<Vector2>() {
                center + RotateVector(size * (Vector2.up + Vector2.left) / 2, rb.rotation),
                center + RotateVector(size * (Vector2.up + Vector2.right) / 2, rb.rotation),
                center + RotateVector(size * (Vector2.down + Vector2.right) / 2, rb.rotation),
                center + RotateVector(size * (Vector2.down + Vector2.left) / 2, rb.rotation)
            };

            // Find the highest corner
            int upperCornerIndex = 0;
            for (int i = 0; i < 4; i++)
                if (vertices[i].y > vertices[upperCornerIndex].y) upperCornerIndex = i;

            // Get ready to compute submerged volume
            float volume = 0;
            WaterNode[] closestNodes = FindClosestSegment(vertices[upperCornerIndex]);

            if ((vertices[upperCornerIndex].y > closestNodes[0].position.y || vertices[upperCornerIndex].y > closestNodes[1].position.y)
            && (vertices[(upperCornerIndex+2)%4].y <= closestNodes[0].position.y || vertices[(upperCornerIndex+2)%4].y <= closestNodes[1].position.y))
            {
                // Add contact points between water & collider
                Vector2[] intersections = FindIntersectionsOnSurface(vertices, rb.rotation, upperCornerIndex);

                // Debug.Log($"Intersections: {intersections[0]} {intersections[1]}");
                // Debug.Log($"Submerged Area (approx.): {(intersections[0].y -(center.y - size.y/2)) * size.x}");

                // Remove unsubmerged vertices
                foreach (Vector2 vertex in vertices.ToArray())
                {
                    if (!waterBody.OverlapPoint(vertex))
                        vertices.Remove(vertex);
                }
                
                vertices.InsertRange(0, intersections);
            }

            // Debug.Log("Vertices:");
            // foreach (var vertex in vertices)
            //     Debug.Log(vertex);

            // Split the unsubmerged volume into triangles
            int[] triangles = SplitIntoTriangles(vertices.ToArray());

            // Compute the submerged volume & its centroid
            Vector2 centroid = ComputeCentroid(vertices.ToArray(), triangles, out volume);

            // Debug.Log($"Buoyancy Centroid: {centroid}\nSubmerged Volume: {volume}");

            float fluidDensity = 1f;
            Vector2 buoyancy = -fluidDensity * Physics2D.gravity * volume;
            
            if (volume != 0 && !float.IsNaN(centroid.x) && !float.IsNaN(centroid.y))
                rb.AddForceAtPosition(buoyancy, centroid);   
        }
        Vector2[] FindIntersectionsOnSurface(List<Vector2> vertices, float rotation, int topIndex)
        {
            Vector2[] intersections = new Vector2[2];

            Vector2 upperCorner = vertices[(topIndex) % 4];
            Vector2 leftCorner = vertices[(topIndex + 3) % 4];
            Vector2 lowerCorner = vertices[(topIndex + 2) % 4]; 
            Vector2 rightCorner = vertices[(topIndex + 1) % 4];

            WaterNode leftNode = FindClosestSegment(leftCorner)[0];
            WaterNode rightNode = FindClosestSegment(rightCorner)[1];

            // Compute the line function that approximates the water surface
            float waterIncline = rightNode.position.x - leftNode.position.x != 0 ?
                (rightNode.position.y - leftNode.position.y) /
                (rightNode.position.x - leftNode.position.x) :
                float.NaN;
            float waterOffset = rightNode.position.y - waterIncline * rightNode.position.x;
            
            // Compute the line function that describes the left side of the collider
            float leftIncline;
            float leftOffset;
            if (leftNode.position.y < leftCorner.y)
            {
                leftIncline = lowerCorner.x - leftCorner.x != 0 ?
                    (lowerCorner.y - leftCorner.y) /
                    (lowerCorner.x - leftCorner.x) :
                    float.NaN;
                leftOffset = lowerCorner.y - leftIncline * lowerCorner.x;
            }
            else
            {
                leftIncline = upperCorner.x - leftCorner.x != 0 ?
                    (upperCorner.y - leftCorner.y) /
                    (upperCorner.x - leftCorner.x) :
                    float.NaN;
                leftOffset = upperCorner.y - leftIncline * upperCorner.x;
            }
            
            // Compute the line function that describes the right side of the collider
            float rightIncline;
            float rightOffset;
            if (rightNode.position.y < rightCorner.y)
            {
                rightIncline = lowerCorner.x - rightCorner.x != 0 ?
                    (lowerCorner.y - rightCorner.y) /
                    (lowerCorner.x - rightCorner.x) :
                    float.NaN;
                rightOffset = lowerCorner.y - rightIncline * lowerCorner.x;
            }
            else
            {
                rightIncline = upperCorner.x - rightCorner.x != 0 ?
                    (upperCorner.y - rightCorner.y) /
                    (upperCorner.x - rightCorner.x) :
                    float.NaN;
                rightOffset = upperCorner.y - rightIncline * upperCorner.x;
            }

            // Now compute each intersection
            intersections[0] = Vector2.zero;
            if (float.IsNaN(leftIncline)) 
            {
                intersections[0].x = leftCorner.x;
                intersections[0].y = waterIncline * intersections[0].x + waterOffset;
            }
            else 
            {
                intersections[0].x = 
                    (leftOffset - waterOffset) /
                    (waterIncline - leftIncline);
                intersections[0].y = waterIncline * intersections[0].x + waterOffset;
            }

            intersections[1] = Vector2.zero;
            if (float.IsNaN(rightIncline)) 
            {
                intersections[1].x = rightCorner.x;
                intersections[1].y = waterIncline * intersections[1].x + waterOffset;
            }
            else 
            {
                intersections[1].x = 
                    (rightOffset - waterOffset) /
                    (waterIncline - rightIncline);
                intersections[1].y = waterIncline * intersections[1].x + waterOffset;
            }

            return intersections;
        }
        int[] SplitIntoTriangles(Vector2[] vertices) 
        {
            List<int> triangles = new List<int>();
            int origin = 0;

            for (int i = 1; i < vertices.Length - 1; i++)
            {
                triangles.AddRange(new int[] {
                    origin, i, i+1
                });
            }

            return triangles.ToArray();
        }
        float ComputePolygonArea(Vector2[] vertices, int[] triangles) 
        {
            float area = 0;
            for (int i = 0; i < triangles.Length; i+=3)
                area += ComputeTriangleArea(vertices[i], vertices[i+1], vertices[i+2]);

            return area;
        }
        float ComputeTriangleArea(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float area = 0;
            float[,] matrix = new float[3,3];
            Vector2[] points = new Vector2[] {p1,p2,p3};

            for(int i = 0; i < 3; i++)
            {
                matrix[i,0] = points[i].x;
                matrix[i,1] = points[i].y;
                matrix[i,2] = 1;
            }

            area = Mathf.Abs(Compute3x3Determinant(matrix));

            return area;
        }
        Vector2 ComputeCentroid(Vector2[] vertices, int[] triangles, out float area)
        {
            Vector2 polygonCentroid = Vector2.zero;
            float polygonArea = 0;

            for (int i = 0; i < triangles.Length; i+=3)
            {
                Vector2 tCentroid = ComputeTriangleCentroid(
                    vertices[triangles[i]],
                    vertices[triangles[i+1]],
                    vertices[triangles[i+2]]
                );

                float tArea = ComputeTriangleArea(
                    vertices[triangles[i]],
                    vertices[triangles[i+1]],
                    vertices[triangles[i+2]]
                );

                // Debug.Log($"tArea: {tArea}");
                polygonCentroid += tArea * tCentroid;
                polygonArea += tArea;
            }
            // Debug.Log($"Sum of centroids*area: {polygonCentroid}\nTotal area: {polygonArea}");
            polygonCentroid = polygonCentroid / polygonArea;
            area = polygonArea;

            return polygonCentroid;
        }
        Vector2 ComputeTriangleCentroid(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 centroid = Vector2.zero;

            centroid.x = (p1.x + p2.x + p3.x) / 3;
            centroid.y = (p1.y + p2.y + p3.y) / 3;

            return centroid;
        }
        float Compute3x3Determinant(float[,] matrix)
        {
            if (matrix.Length != 9)
                throw new System.Exception("Matrix is not 3x3");

            float det = 0;
            for(int i=0;i<3;i++)
                det += (matrix[0,i]*(matrix[1,(i+1)%3]*matrix[2,(i+2)%3] - matrix[1,(i+2)%3]*matrix[2,(i+1)%3]));

            return det;
        }
        public static Vector2 GetColliderSize(Collider2D other)
        {
            Vector2 size = Vector2.zero;

            switch(other){
                case BoxCollider2D box:
                    // Debug.Log("It's a box");
                    size = box.size;
                    break;
                case CapsuleCollider2D capsule:
                    // Debug.Log("It's a capsule");
                    size = capsule.size;
                    break;
                case CircleCollider2D circle:
                    // Debug.Log("It's a circle");
                    size = circle.radius * Vector2.one;
                    break;
                default:
                    Debug.LogError("Floating collider fell into generic case");
                    size = other.bounds.size;
                    break;
            }
            
            return size * other.transform.localScale;

        }
        WaterNode[] FindClosestSegment(Vector2 point)
        {
            float minDistance = float.PositiveInfinity;
            float secondToMin = minDistance;
            WaterNode closestNode = nodes[0];
            WaterNode secondToClosest = closestNode;
            
            foreach (WaterNode node in nodes)
            {
                float sqrDistance = (node.position - point).sqrMagnitude;
                if (sqrDistance < minDistance)
                {
                    secondToMin = minDistance;
                    secondToClosest = closestNode;

                    minDistance = sqrDistance;
                    closestNode = node;
                }
                else if (sqrDistance < secondToMin) 
                {
                    secondToMin = sqrDistance;
                    secondToClosest = node;
                }
            }

            if (closestNode.position.x < secondToClosest.position.x) 
                return new WaterNode[] {closestNode, secondToClosest};

            return new WaterNode[] {secondToClosest, closestNode};
        }
        public static Vector2 RotateVector(Vector2 vector, float degrees)
        {
            degrees = degrees % 360;
            float radians = Mathf.Deg2Rad * degrees;

            return new Vector2(
                Mathf.Cos(radians) * vector.x - Mathf.Sin(radians) * vector.y,
                Mathf.Sin(radians) * vector.x + Mathf.Cos(radians) * vector.y
            );
        }
        void ComputeCoeficients()
        {
            positionDelta = 1f / nodesPerUnit;
            massPerNode = (1f / nodesPerUnit) * waterDepth;
        }
    #endregion

    #region Surface Control
        void CheckCameraBounds() 
        {
            Vector2 WorldUnitsInCamera;
            WorldUnitsInCamera.y = Camera.main.orthographicSize * 2;
            WorldUnitsInCamera.x = WorldUnitsInCamera.y * Screen.width / Screen.height;
            
            Vector2 leftMostPos = nodes[0].position;
            float bound = Camera.main.transform.position.x - WorldUnitsInCamera.x / 2 - despawnDistance;

            if (leftMostPos.x < bound) {
                for (int i = 0; i < bound - leftMostPos.x; i++)
                {
                    DestroyChunks();
                    AddChunks();
                }
            }
        }
        public void DestroyChunks(int numberOfChunks = 1)
        {
            if (numberOfChunks < 1) 
                throw new System.Exception("Cannot destroy a negative number of chunks.");
            
            nodes.RemoveRange(0, nodesPerUnit * numberOfChunks);
        }
        public void AddChunks(int numberOfChunks = 1)
        {
            if (numberOfChunks < 1) 
                throw new System.Exception("Cannot add a negative number of chunks.");

            Vector2 anchor;
            Vector2 disturbance;
            for (int i = 1; i <= nodesPerUnit * numberOfChunks; i++)
            {
                anchor = new Vector2(nodes[nodes.Count-1].position.x, transform.position.y);
                disturbance = waveIntensity * Mathf.Sin(time) * Vector2.up;
                
                nodes.Add(new WaterNode(anchor + (positionDelta * i) * Vector2.right, disturbance));
                
                time = (time + Time.fixedDeltaTime) % (2*Mathf.PI); 
            }
            
        }
        void GenerateWaves()
        {
            // Vector2 force = random.Next(-waveIntensity,waveIntensity) * Vector2.up;

            Vector2 disturbance = waveIntensity * Mathf.Sin(time) * Vector2.up;
            time = (time + Time.fixedDeltaTime) % (2*Mathf.PI);

            nodes[nodes.Count-1].Disturb(disturbance);
            // nodes[nodes.Count-2].Disturb(disturbance);
            // nodes[nodes.Count-3].Disturb(disturbance);
            // nodes[nodes.Count-4].Disturb(disturbance);
            // nodes[nodes.Count-5].Disturb(disturbance);
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
                        nodes[i - 1].velocity += leftDeltas[i];
                    }
                    if (i < nodes.Count - 1)
                    {
                        rightDeltas[i] = spread * (nodes[i].position - nodes[i + 1].position + Vector2.right * positionDelta);
                        nodes[i + 1].velocity += rightDeltas[i];
                    }
                }

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (i > 0)
                        nodes[i - 1].position += leftDeltas[i] * Time.fixedDeltaTime;
                    if (i < nodes.Count - 1)
                        nodes[i + 1].position += rightDeltas[i] * Time.fixedDeltaTime;
                }
            }
        }
        void ReactToCollisions()
        {
            Dictionary<Collider2D,List<WaterNode>> splashedNodes = new Dictionary<Collider2D, List<WaterNode>>();

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
                    if (!splashedNodes.ContainsKey(splasher)) 
                        splashedNodes.Add(splasher, new List<WaterNode>());

                    splashedNodes[splasher].Add(node);

                    // float mass = splasher.attachedRigidbody.mass;
                    // Vector2 velocity = splasher.attachedRigidbody.velocity;

                    // node.Splash(mass * velocity, massPerNode);
                }
            }

            foreach (Collider2D splasher in splashedNodes.Keys)
            {
                float massPerSplash = splasher.attachedRigidbody.mass / splashedNodes[splasher].Count;
                Vector2 velocity = splasher.attachedRigidbody.velocity;

                foreach(WaterNode node in splashedNodes[splasher])
                    node.Splash(massPerSplash * velocity, massPerNode);
            } 
        }
    #endregion

    #region Draw Functions
        void DrawSurfaceLine() 
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
                    (nodes[i].position.x - transform.position.x) * Vector3.right + (transform.position.y - waterDepth) * Vector3.up,
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
            #if UNITY_EDITOR
                Gizmos.color = waterColor;
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
