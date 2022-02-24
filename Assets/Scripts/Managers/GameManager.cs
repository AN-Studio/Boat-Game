using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [Range(.95f,1.1f)] public float wavePeriod = 1;
        [Range(0,60f)] public float windSpeed = 0;
    #endregion

    #region References
        public Transform lastEndpoint;
        public List<RNGCell> cells;
    #endregion

    #region Private Variables
        private int coinCombo = 0;
        private Coroutine comboTimer;
        private WaitForSeconds waitFor2Seconds = new WaitForSeconds(2f);
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
    
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
        while (cellCount < maxCellCount) SpawnCell();
    }

    #region Public Functions
        public void ResetGame() 
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

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

        public void IncreaseCoinCombo() 
        {
            // Set FMOD global parameter 'Coin Combo'
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Coin Combo", coinCombo);
            coinCombo = coinCombo <= 15? coinCombo+1 : 0;
            BeginComboTimer(); 
        }

        public void BeginComboTimer()
        {
            if (comboTimer != null) StopCoroutine(comboTimer);
            comboTimer = StartCoroutine(CoinComboTimer());
        }

        private IEnumerator CoinComboTimer()
        {
            yield return waitFor2Seconds;
            
            // Set FMOD global parameter 'Coin Combo'
            coinCombo = 0;
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Coin Combo", coinCombo);
        }
    #endregion
}
