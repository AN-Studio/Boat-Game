using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : Singleton<GameManager>
{
    #region Structures
        public enum GameState {None, Calm, Hostile, GameEnded}
        
        [System.Serializable]
        public struct RNGCell
        {
            public Cell prefab;
            public int weight; 
        }

        [System.Serializable]
        public struct Range {
            public float min;
            public float max;
        }
    
        #region Formula Parameter Structures
            [System.Serializable]
            public class WindSpeedParams
            {
                [Range(0,60)]public float baseValue = 24;
                public float maxValue = 60;
                public float increaseRate = 1;
                [Range(1,100) ]public float metersPerIncrease = 1;
            }
            [System.Serializable]
            public class WaveIntensityParams
            {
                [Range(.1f,10)] public float baseValue = 2.6f;
                public float maxValue = 10;
                public float increaseRate = 1;
                [Range(1,100) ]public float metersPerIncrease = 1;
            }
        #endregion

    #endregion

    #region Game State

        #region Game State
            [Header("Game State")]
            public bool gameStarted = false;
            public bool gameEnded = false;
            public GameState gameState = GameState.Calm;
        #endregion
        
        #region Weather Parameters
            [Header("Weather Parameters")]
            public WindSpeedParams windSpeed;
        #endregion

        #region Wave Parameters
            [Header("Wave Parameters")]
            public WaveIntensityParams waveIntensity;
            public Range wavePeriodRange;
            [FormerlySerializedAs("baseWavePeriod")] [Range(.95f,1.1f)] float wavePeriod = 1.04f;
            public Range waveNoiseFactorRange;
            [FormerlySerializedAs("baseWaveNoiseFactor")] [Range(.5f,2)] float waveNoiseFactor = .6f;
            public float maxWaveNoiseFactor = 2f;
        #endregion

        #region RNG Cell Settings
            [Header("RNG Cell Settings")]
            [SerializeField] int maxCellCount;
            private int cellCount = 1;
        #endregion 
    #endregion

    #region References
        public Transform lastEndpoint;
        public List<RNGCell> cells;
        private Transform gameWorld;
    #endregion

    #region Private Variables
        private int coinCombo = 0;
        private Coroutine comboTimer;
        private WaitForSeconds waitFor2Seconds = new WaitForSeconds(2f);
    #endregion

    #region Properties & Formulas
        public float WindSpeed {
            get => Mathf.Min(
                windSpeed.maxValue,
                windSpeed.baseValue
            );
        }
        public float WaveIntensity {
            get => Mathf.Min(
                waveIntensity.maxValue,
                waveIntensity.baseValue
            );
        }
        public float WavePeriod {
            get => wavePeriod;
        }
        public float WaveNoiseFactor {
            get => waveNoiseFactor;
        }
    #endregion

    protected override void Awake() 
    {
        base.Awake();
        gameWorld = GameObject.Find("Game World").transform;
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

            Cell instance  = Instantiate(cell, lastEndpoint.position, Quaternion.identity, gameWorld);
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
