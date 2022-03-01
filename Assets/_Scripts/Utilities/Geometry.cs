using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Geometry
{
    public static (Vector2 p1, Vector2 p2) FindIntersectionsOnSurface<T>(List<Vector2> vertices, float rotation, int topIndex, List<T> nodes)
    where T : FunctionNode
    {
        Vector2 upperCorner = vertices[(topIndex) % 4];
        Vector2 leftCorner = vertices[(topIndex + 3) % 4];
        Vector2 lowerCorner = vertices[(topIndex + 2) % 4]; 
        Vector2 rightCorner = vertices[(topIndex + 1) % 4];

        T leftNode = FindClosestSegment(leftCorner, nodes).leftNode;
        T rightNode = FindClosestSegment(rightCorner, nodes).rightNode;

        // Compute the line function that approximates the water surface
        float waterSlope = rightNode.position.x - leftNode.position.x != 0 ?
            (rightNode.position.y - leftNode.position.y) /
            (rightNode.position.x - leftNode.position.x) :
            float.NaN;
        float waterOffset = rightNode.position.y - waterSlope * rightNode.position.x;
        
        // Compute the line function that describes the left side of the collider
        float leftSlope;
        float leftOffset;
        if (leftNode.position.y < leftCorner.y)
        {
            leftSlope = lowerCorner.x - leftCorner.x != 0 ?
                (lowerCorner.y - leftCorner.y) /
                (lowerCorner.x - leftCorner.x) :
                float.NaN;
            leftOffset = lowerCorner.y - leftSlope * lowerCorner.x;
        }
        else
        {
            leftSlope = upperCorner.x - leftCorner.x != 0 ?
                (upperCorner.y - leftCorner.y) /
                (upperCorner.x - leftCorner.x) :
                float.NaN;
            leftOffset = upperCorner.y - leftSlope * upperCorner.x;
        }
        
        // Compute the line function that describes the right side of the collider
        float rightSlope;
        float rightOffset;
        if (rightNode.position.y < rightCorner.y)
        {
            rightSlope = lowerCorner.x - rightCorner.x != 0 ?
                (lowerCorner.y - rightCorner.y) /
                (lowerCorner.x - rightCorner.x) :
                float.NaN;
            rightOffset = lowerCorner.y - rightSlope * lowerCorner.x;
        }
        else
        {
            rightSlope = upperCorner.x - rightCorner.x != 0 ?
                (upperCorner.y - rightCorner.y) /
                (upperCorner.x - rightCorner.x) :
                float.NaN;
            rightOffset = upperCorner.y - rightSlope * upperCorner.x;
        }

        // Now compute each intersection
        Vector2 p1 = Vector2.zero;
        if (float.IsNaN(leftSlope)) 
        {
            p1.x = leftCorner.x;
            p1.y = waterSlope * p1.x + waterOffset;
        }
        else 
        {
            p1.x = 
                (leftOffset - waterOffset) /
                (waterSlope - leftSlope);
            p1.y = waterSlope * p1.x + waterOffset;
        }

        Vector2 p2 = Vector2.zero;
        if (float.IsNaN(rightSlope)) 
        {
            p2.x = rightCorner.x;
            p2.y = waterSlope * p2.x + waterOffset;
        }
        else 
        {
            p2.x = 
                (rightOffset - waterOffset) /
                (waterSlope - rightSlope);
            p2.y = waterSlope * p2.x + waterOffset;
        }

        return (p1,p2);
    }
    public static (T leftNode, T rightNode) FindClosestSegment<T>(Vector2 point, List<T> nodes) where T : FunctionNode
    {
        #region Binary Search
            int i;
            int start = 0;
            int end = nodes.Count-1;
            
            float distance;
            float leftDistance;
            float rightDistance;
            
            while (start <= end) 
            {
                i = (start + end) / 2;

                distance = Mathf.Abs(nodes[i].position.x - point.x);
                leftDistance = 0 <= i-1 ? Mathf.Abs(nodes[i-1].position.x - point.x) : distance;
                rightDistance = i+1 < nodes.Count ? Mathf.Abs(nodes[i+1].position.x - point.x) : distance;
                
                if (leftDistance < distance) 
                    end = i-1;
                else if (rightDistance < distance) 
                    start = i+1;
                else
                {
                    if (0 == i) 
                        return (nodes[i], nodes[i+1]);
                    else if (i == nodes.Count-1)
                        return (nodes[i-1], nodes[i]);
                    if (0 < i-1 && leftDistance < rightDistance)
                        return (nodes[i-1], nodes[i]);
                    else
                        return (nodes[i], nodes[i+1]);
                }
            }

            
            throw new System.Exception("Was unable to find closest segment to the node.");
            // return ( default(T), default(T) );
        #endregion
    }
    public static List<int> SplitIntoTriangles(List<Vector2> vertices) 
    {
        List<int> triangles = new List<int>();
        int origin = 0;

        // print($"Vertex Count: {vertices.Count}");
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.AddRange(new int[] {
                origin, i, i+1
            });
        }

        return triangles;
    }
    public static float ComputeTriangleArea(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float[,] matrix = new float[,] {
            {p1.x, p1.y, 1},
            {p2.x, p2.y, 1},
            {p3.x, p3.y, 1}
        };
        
        return Mathf.Abs(Compute3x3Determinant(matrix)) / 2;
    }
    public static Vector2 ComputeCentroid(List<Vector2> vertices, List<int> triangles, out float area)
    {
        Vector2 centroid = Vector2.zero;
        area = 0;

        // print($"Triangle Count: {triangles.Count}");
        for (int i = 0; i < triangles.Count; i+=3)
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
            centroid += tArea * tCentroid;
            area += tArea;
        }
        // Debug.Log($"Sum of centroids*area: {centroid}\nTotal area: {area}");
        centroid = centroid / area;

        return centroid;
    }
    public static Vector2 ComputeTriangleCentroid(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1 + p2 + p3) / 3;
    }
    public static float Compute3x3Determinant(float[,] matrix)
    {
        if (matrix.Length != 9)
            throw new System.Exception("Matrix is not 3x3");

        float det = 0;
        for(int i=0;i<3;i++)
            det += matrix[0,i] * (matrix[1,(i+1)%3] * matrix[2,(i+2)%3] - matrix[1,(i+2)%3] * matrix[2,(i+1)%3]);

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
        
        return size * other.transform.lossyScale;

    }
}
