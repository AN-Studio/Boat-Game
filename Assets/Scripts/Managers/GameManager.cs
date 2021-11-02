using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public struct RNGCell
    {
        public GameObject prefab;
        public int weight; 
    }

    #region Singleton
        static GameManager instance;
        public static GameManager Instance {get => instance;}
    #endregion

    #region Switches
        public bool gameStarted = false;
        public bool gameEnded = false;
    #endregion

    #region Prefabs
        public List<RNGCell> cells;
    #endregion

    void Awake() 
    {
        #region Singleton
            if (instance == null)
            {
                instance = this;
            } 
            else
            {
                Debug.LogWarning("Tried to create another instance of GameManager");
                Destroy(this.gameObject);
            }
        #endregion
    
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        // GameObject prefab = GetRandomCell();
        // Instantiate(prefab,new Vector3(120,0,0), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region Public Functions
        public GameObject GetRandomCell() 
        {
            int totalWeight = 0;
            foreach (var cell in cells) 
                totalWeight += cell.weight;

            float random = Random.value * totalWeight;

            int index = 0;
            int w = cells[index].weight;
            while (w < random)
            {
                index++;
                w += cells[index].weight;
            }

            return cells[index].prefab;
        }
    #endregion
}
