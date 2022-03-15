using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{

    public GameObject meteor;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            Rigidbody2D rb = Instantiate(meteor, position, Quaternion.identity).GetComponent<Rigidbody2D>();
            // rb.velocity = Vector2.left;
        }
    }
}