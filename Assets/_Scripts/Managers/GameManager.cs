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
        public class PulsingRandomizer {
            public Vector2 valueRange;
            public PulseTimer timer = new PulseTimer();
            public Vector2 timerRange;
        }
    
        #region Formula Parameter Structures
            [System.Serializable]
            public class WindSpeedParams
            {
                [Range(0,60)] public float simulatedIncrease = 0;

                [Header("Settings")]
                [Range(0,60)]public float baseValue = 24;
                public float maxValue = 60;
                [Range(1,2000)] public float metersPerValueIncrease = 1;
                [Range(0f,.1f)] public float percentValueIncrease = .05f;
            }
            [System.Serializable]
            public class WaveIntensityParams
            {
                [Range(0,10)] public float simulatedIncrease = 0;
                
                [Header("Settings")]
                [Range(.1f,10)] public float baseValue = 2.6f;
                public float maxValue = 10;
                [Range(1,2000) ]public float metersPerValueIncrease = 1;
                [Range(0f,.1f)] public float percentValueIncrease = .05f;
            }
        #endregion

    #endregion

    #region Game State

        #region Game State
            [Header("Game State")]
            public bool gameStarted = false;
            public bool gameEnded = false;
            public GameState gameState = GameState.Calm;
            public Vector2 timeRangeUntilStateChange;
            public Vector2 calmTimeRange;
            public Vector2 hostileTimeRange;
            private PulseTimer stateTimer = new PulseTimer();
        #endregion
        
        #region Weather Parameters
            [Header("Weather Parameters")]
            public WindSpeedParams windSpeed;
        #endregion

        #region Wave Parameters
            [Header("Wave Parameters")]
            public WaveIntensityParams waveIntensity;
            [Range(.95f,1.1f)] public float wavePeriod = 1.04f;
            public PulsingRandomizer wavePeriodRandomizer;
            [Range(.5f,2)] public float waveNoise = .6f;
            public PulsingRandomizer waveNoiseRandomizer;
        #endregion

        #region RNG Cell Settings
            [Header("RNG Cell Settings")]
            [SerializeField] int maxCellCount;
            private int cellCount = 1;
        #endregion 
    #endregion

    #region References
        [Header("References")]
        public GameObject gameUI;
        public GameObject gameOverScreen;

        #region Game World
        [Header("Game World")]
            public Transform lastEndpoint;
            public List<RNGCell> cells;
            private Transform gameWorld;
        #endregion
    #endregion

    #region Private Variables
        private int coinCombo = 0;
        private Coroutine comboTimer;
        private WaitForSeconds waitFor2Seconds = new WaitForSeconds(2f);
        private Coroutine hostilityLerper;
        private float hostilityFactor = 0;
    #endregion

    #region Properties & Formulas
        public float WindSpeed {
            get {
                float result = Mathf.Min(
                    windSpeed.maxValue,
                    windSpeed.baseValue + windSpeed.simulatedIncrease +
                        windSpeed.percentValueIncrease *
                        (windSpeed.maxValue - windSpeed.baseValue) * 
                        (DistanceTravelled / windSpeed.metersPerValueIncrease) 
                );

                return result * (1 + hostilityFactor);
            }
        }
        public float WaveIntensity {
            get {
                float result = Mathf.Min(
                    waveIntensity.maxValue,
                    waveIntensity.baseValue + waveIntensity.simulatedIncrease +
                        waveIntensity.percentValueIncrease *
                        (waveIntensity.maxValue - waveIntensity.baseValue) * 
                        (DistanceTravelled / waveIntensity.metersPerValueIncrease) 
                );

                return result * (1 + hostilityFactor);
            }
        }
        public float WavePeriod {
            get => wavePeriod * (3*WaveIntensity / waveIntensity.baseValue + WindSpeed / windSpeed.baseValue)/4;
        }
        public float WaveNoiseFactor {
            get => waveNoise;
        }
        public float DistanceTravelled {
            // get => 0;
            get => ShipController.Instance.transform.position.x - ShipSpawner.Instance.transform.position.x;
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
        wavePeriodRandomizer.timer.onPulse += UpdateWavePeriod;
        wavePeriodRandomizer.timer.ResetPulseTo(
            wavePeriodRandomizer.timerRange.x + 
            (wavePeriodRandomizer.timerRange.y - wavePeriodRandomizer.timerRange.x) / 2
        );
        
        waveNoiseRandomizer.timer.onPulse += UpdateWaveNoise;
        waveNoiseRandomizer.timer.ResetPulseTo(
            waveNoiseRandomizer.timerRange.x + 
            (waveNoiseRandomizer.timerRange.y - waveNoiseRandomizer.timerRange.x) / 2
        );

        stateTimer.onPulse += UpdateState;
        stateTimer.ResetPulseTo(timeRangeUntilStateChange.y);

        hostilityLerper = StartCoroutine(LerpHostility());

        // GameObject prefab = GetRandomCell();
        // Instantiate(prefab,new Vector3(120,0,0), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        stateTimer.Tick();
        wavePeriodRandomizer.timer.Tick();
        waveNoiseRandomizer.timer.Tick();

        // print($"wavePeriod: {wavePeriod}\nWavePeriod: {WavePeriod}");

        while (cellCount < maxCellCount) SpawnCell();
    }

    private void OnDestroy() 
    {
        StopCoroutine(hostilityLerper);
    }

    #region Public Functions
        public void ResetGame() 
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        public void EndGame()
        {
            gameState = GameState.GameEnded;

            gameUI.SetActive(false);
            gameOverScreen.SetActive(true);
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


        private IEnumerator LerpHostility()
        {
            while (true)
            {
                yield return null;

                switch (gameState)
                {
                    case GameState.Calm:
                        hostilityFactor -= .05f * Time.deltaTime;
                        break;
                    case GameState.Hostile:
                        hostilityFactor += .025f * Time.deltaTime;
                        break;
                    default:
                        break;
                }
                
                hostilityFactor = Mathf.Clamp01(hostilityFactor);
            }
        }
        private void UpdateState() 
        {
            if (gameState != GameState.None && gameState != GameState.GameEnded)
            {
                if (gameState == GameState.Calm)
                {
                    gameState = GameState.Hostile;
                    stateTimer.SetPulseTo(
                        Random.Range(hostileTimeRange.x, hostileTimeRange.y)
                    );
                }
                else if (gameState == GameState.Hostile)
                {
                    gameState = GameState.Calm;
                    stateTimer.SetPulseTo(
                        Random.Range(calmTimeRange.x, calmTimeRange.y)
                    );
                }

                // gameState = gameState != GameState.Hostile ?
                //     GameState.Hostile :
                //     GameState.Calm
                // ;
            }

            // stateTimer.SetPulseTo(
            //     Random.Range(timeRangeUntilStateChange.x, timeRangeUntilStateChange.y)
            // );

        }
        private void UpdateWaveNoise() 
        {
            waveNoise = Random.Range(waveNoiseRandomizer.valueRange.x, waveNoiseRandomizer.valueRange.y);
            waveNoiseRandomizer.timer.SetPulseTo( Random.Range(waveNoiseRandomizer.timerRange.x, waveNoiseRandomizer.timerRange.y) );
        }
        private void UpdateWavePeriod() 
        {
            wavePeriod = Random.Range(wavePeriodRandomizer.valueRange.x, wavePeriodRandomizer.valueRange.y);
            wavePeriodRandomizer.timer.SetPulseTo( Random.Range(wavePeriodRandomizer.timerRange.x, wavePeriodRandomizer.timerRange.y) );
        }
    #endregion
}
