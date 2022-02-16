using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    #region References
        [Header("References")]
        public ParticleSystem particles;
        public GameObject sprite;
        private Coroutine breakAnimation;
    #endregion

    #region Settings
        [Header("Settings")]
        public int value = 1;
    #endregion

    private void OnTriggerEnter2D(Collider2D other) 
    {
        // print("COLLISION!");
        if (other.gameObject.CompareTag("Player"))
        {
            DataManager.Instance.money += value;
            GameManager.Instance.IncreaseCoinCombo();
            
            sprite.SetActive(false);
            
            breakAnimation = StartCoroutine(BreakContainer());
        }
    }

    private void OnDestroy() {
        if (breakAnimation != null) StopCoroutine(breakAnimation);
    }

    private IEnumerator BreakContainer()
    {
        particles.Play();
        while (particles.isPlaying)
        {
            yield return null;
        }

        Destroy(transform.parent.gameObject);
    }

}
