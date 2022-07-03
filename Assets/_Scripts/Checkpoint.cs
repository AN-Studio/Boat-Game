using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    protected virtual void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.CompareTag("Player"))
        {
            WorldGenerator.Instance.TickCheckpoint();    
            print("Passed a Checkpoint!");
            gameObject.SetActive(false);
        }
    }
}