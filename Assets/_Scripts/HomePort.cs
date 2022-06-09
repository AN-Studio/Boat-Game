using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HomePort : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) 
    {
        GameManager.Instance.WinGame();
        other.attachedRigidbody.drag = 1000f;
    } 
}
