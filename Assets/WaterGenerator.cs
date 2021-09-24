using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(MeshFilter), typeof(MeshCollider))]
public class WaterGenerator : MonoBehaviour
{
    public float longitude;
    public int nodeCount;

    public LineRenderer surface;
    public Vector2[] points;
    public float[] velocities;
    public float[] accelerations;

    public Mesh mesh;
    public float waterDepth;
    public new MeshCollider collider;


    void Awake() 
    {
        surface = GetComponent<LineRenderer>();
        mesh = GetComponent<MeshFilter>().mesh;
        collider = GetComponent<MeshCollider>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
