using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : PersistentSingleton<GameManager>
{
    [System.Serializable]
    public struct RNGCell
    {
        public Cell prefab;
        public int weight; 
    }

    #region Game State

        #region Game State
            [Header("Game State")]
            public bool gameStarted = false;
            public bool gameEnded = false;
            [Range(0,60f)] public float windSpeed = 0;
        #endregion
        
        #region Wave Parameters
            [Header("Wave Parameters")]
            [Range(.1f,10f)] public float waveIntensity = 1;
            [Range(.95f,1.1f)] public float wavePeriod = 1;
            [Range(.5f,2)] public float waveNoiseFactor = .5f;
        #endregion

        [Header("Cell Settings")]
        [SerializeField] int maxCellCount;
        private int cellCount = 1;
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

    protected override void Awake() 
    {
        base.Awake();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
