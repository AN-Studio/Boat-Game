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
        public int forcesPerAxis = 5;
        
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

        void OnTriggerStay2D(Collider2D other) 
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

            //Find the highest corner
            int upperCornerIndex = 0;
            for (int i = 0; i < 4; i++)
                if (vertices[i].y > vertices[upperCornerIndex].y) upperCornerIndex = i;

            float volume = size.x * size.y;
            float unsubmergedVolume = 0;

            // Get ready to compute unsubmerged volume
            WaterNode[] closestNodes = FindClosestSegment(vertices[upperCornerIndex]);
            if (vertices[upperCornerIndex].y > closestNodes[0].position.y || vertices[upperCornerIndex].y > closestNodes[1].position.y)
            {
                // Add contact points between water & collider
                Vector2[] intersections = FindIntersectionsOnSurface(vertices, rb.rotation, upperCornerIndex);
                
                // Remove submerged vertices
                foreach (Vector2 vertex in vertices)
                {
                    if (waterBody.OverlapPoint(vertex))
                        vertices.Remove(vertex);
                }
                
                vertices.InsertRange(upperCornerIndex, intersections);

                // Split the unsubmerged volume into triangles
                List<int> triangles = SplitIntoTriangles(vertices);
            }



            // Vector2 step = size / forcesPerAxis;
            // Vector2 origin = center + RotateVector(-size / 2f, rb.rotation);
            // Vector2[] points = new Vector2[forcesPerAxis*forcesPerAxis];
            // for (int j = 0; j < forcesPerAxis; j++)
            // {
            //     for (int i = 0; i < forcesPerAxis; i++)
            //     {
            //         Vector2 point = new Vector2(step.x * i, step.y * j);
            //         point = RotateVector(point, rb.rotation);

            //         points[i + j * forcesPerAxis] = 
            //             origin + point
            //         ;
            //     }
            // }

            float fluidDensity = 1f;

            // float volumePerPoint = volume / points.Length;
            // float torque = 0f;
            // Vector2 buoyancy = Vector2.zero;
            // foreach (Vector2 point in points)
            // {
            //     if (waterBody.OverlapPoint(point)) 
            //     {
            //         Vector2 localForce = -fluidDensity * Physics2D.gravity * volumePerPoint;
            //         Vector2 radius = point - (Vector2) rb.worldCenterOfMass;

            //         buoyancy += localForce;
                    
            //         rb.AddForceAtPosition(-fluidDensity * Physics2D.gravity * volumePerPoint, point);
            //     }
            // }

            float dragCoefficient = .38f;
            float crossSection = rb.velocity.y > 0 ? other.bounds.size.x : other.bounds.size.y; // this one needs a better solution

            Vector2 buoyancy = -fluidDensity * Physics2D.gravity * (volume - unsubmergedVolume);
            float drag = .5f * rb.velocity.sqrMagnitude * dragCoefficient * crossSection;
            
            // Debug.Log($"Size: {size}\nTorque: {torque}");

            rb.AddForce(-drag * rb.velocity.normalized);
            
        }

        void Awake() 
        {
            surface = GetComponent<LineRenderer>();
            nodes = new List<WaterNode>();
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
        if (float.IsNaN(rightIncline)) 
        {
            intersections[0].x = rightCorner.x;
            intersections[0].y = waterIncline * intersections[0].x + waterOffset;
        }
        else 
        {
            intersections[0].x = 
                (rightOffset - waterOffset) /
                (waterIncline - rightIncline);
            intersections[0].y = waterIncline * intersections[0].x + waterOffset;
        }

        intersections[1] = Vector2.zero;
        if (float.IsNaN(leftIncline)) 
        {
            intersections[1].x = leftCorner.x;
            intersections[1].y = waterIncline * intersections[1].x + waterOffset;
        }
        else 
        {
            intersections[1].x = 
                (leftOffset - waterOffset) /
                (waterIncline - leftIncline);
            intersections[1].y = waterIncline * intersections[1].x + waterOffset;
        }

        return intersections;
    }

    List<int> SplitIntoTriangles(List<Vector2> vertices) 
    {
        List<int> triangles = new List<int>();

        // TODO: Split the given polygon into triangles

        return triangles;
    }

    Vector2 GetColliderSize(Collider2D other)
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
                size = other.bounds.size / forcesPerAxis;
                break;
        }
        
        return size * other.transform.localScale;

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

    Vector2 RotateVector(Vector2 vector, float degrees)
    {
        return new Vector2(
            Mathf.Cos(degrees) * vector.x - Mathf.Sin(degrees) * vector.y,
            Mathf.Sin(degrees) * vector.x + Mathf.Cos(degrees) * vector.y
        );
    }

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
        // private void OnDrawGizmos() {
        //     Gizmos.color = waterColor;
        //     Gizmos.DrawLine(transform.position, transform.position + Vector3.right * longitude);
        //     Gizmos.DrawCube(
        //         transform.position + Vector3.right * longitude/2 + Vector3.down * waterDepth/2,
        //         Vector3.right * longitude + Vector3.down * waterDepth    
        //     );
        // }
    #endregion
}
