using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public struct RNGCell
    {
        public Cell prefab;
        public int weight; 
    }

    #region Singleton
        public static GameManager Instance {get; private set;}
    #endregion

    #region Game State
        [SerializeField] int maxCellCount;
        private int cellCount = 1;
        public bool gameStarted = false;
        public bool gameEnded = false;
        [Range(.1f,10f)] public float waveIntensity = 1;
        [Range(0,60f)] public float windSpeed = 0;
    #endregion

    #region References
        [SerializeField] GlobalEvents globalEvents;
        public Transform lastEndpoint;
        public List<RNGCell> cells;
    #endregion

    #region Private Variables
        FMOD.Studio.EventInstance gustSE;
    #endregion

    void Awake() 
    {
        #region Singleton
            if (Instance == null)
            {
                Instance = this;
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
        gustSE = FMODUnity.RuntimeManager.CreateInstance(globalEvents.gustingWind.fmodEvent);
    }

    // Update is called once per frame
    void Update()
    {
        while (cellCount < maxCellCount) SpawnCell();
        
        List<FMODParameter> list = globalEvents.gustingWind.parameters;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].name == "Wind Speed")
                list[i].value = windSpeed;
        }
    }

    #region Public Functions
        public Cell GetRandomCell() 
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
        public void SpawnCell()
        {
            Cell cell = GetRandomCell();

            Cell instance  = Instantiate(cell, lastEndpoint.position, Quaternion.identity);
            lastEndpoint = instance.EndPoint;
            
            cellCount++;
        }
        public void DecreaseCellCount() => cellCount--;
    #endregion
}
