using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    #region Settings
        [Header("Settings")]
        [SerializeField] private Item item;
        [SerializeField] private Vector2Int amountRange;
        private int amount;
    #endregion

    #region References
        [Header("References")]
        public ParticleSystem particles;
        public GameObject sprite;
        private Coroutine breakRoutine;
        private WaitForSeconds waitForSeconds = new WaitForSeconds(.2f);
        private static System.Random random;
    #endregion

    private void Awake() 
    {
        if (random == null) random = new System.Random();
        amount = random.Next(amountRange.x, amountRange.y);

        particles = GetComponentInChildren<ParticleSystem>();
        sprite = GetComponentInChildren<SpriteRenderer>().gameObject;
    }

    private void OnTriggerEnter2D(Collider2D other) 
    {
        // print("COLLISION!");
        if (other.gameObject.CompareTag("Player"))
        {          
            sprite.SetActive(false);
            breakRoutine = StartCoroutine(BreakContainer());
        }
    }

    private void OnDestroy() {
        if (breakRoutine != null) StopCoroutine(breakRoutine);
    }

    private IEnumerator BreakContainer()
    {
        int count = 0;
        Vector3 position = transform.position;
        
        particles.Play();
        
        while (count < amount && particles.IsAlive())
        {
            if (count < amount)
            {
                Instantiate(item, position, Quaternion.identity);
                count++;
            }

            yield return waitForSeconds;
        }

        Destroy(transform.parent.gameObject);
    }

}
