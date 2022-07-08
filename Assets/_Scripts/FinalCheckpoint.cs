using UnityEngine;

public class FinalCheckpoint : Checkpoint 
{
    protected override void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.CompareTag("Player"))
        {
            // base.OnTriggerEnter2D(other);
            GameManager.Instance.EndGame();
        }
    }    
}