using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1;

    private void OnTriggerEnter2D(Collider2D other) 
    {
        // print("COLLISION!");
        if (other.gameObject.CompareTag("Player"))
        {
            DataManager.Instance.money += value;
            Destroy(transform.parent.gameObject);
        }
    }
}
