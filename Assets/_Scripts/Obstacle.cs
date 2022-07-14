using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D other) 
    {
        if (other.collider.CompareTag("Player"))
        {
            ShipController ship = other.gameObject.GetComponent<ShipController>();

            // ship.Crash();

            // I'm temporarily making the ship bounce upwards to get the right balance.
            ship.Bounce(other.GetContact(0).point);
        }
    }
}
