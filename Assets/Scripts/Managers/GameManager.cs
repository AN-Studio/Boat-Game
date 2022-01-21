using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState {Calm, Hostile}

    [System.Serializable]
    public struct RNGCell
    {
        public Cell prefab;
        public int weight; 
    }

    #region Singleton
        public static GameManager Instance {get; private set;}
    #endregion

    #region Events
        public delegate void OnStateChanged();
        public OnStateChanged onStateChanged;
    #endregion

    #region Settings
        [Header("Settings")]
        public float secondsUntilHostile = 5f;
        public float secondsUntilCalm = 10f;
    #endregion

    #region Game State
        [Header("Game State")]
        [SerializeField] int maxCellCount;
        private int cellCount = 1;
        public bool gameStarted = false;
        public bool gameEnded = false;
        [Range(.1f,10f)] public float waveIntensity = 1;
        [Range(0,60f)] public float windSpeed = 0;
        public GameState state = GameState.Calm;
        private WaitForSeconds waitUntilCalm;
        private WaitForSeconds waitUntilHostile;
    #endregion

    #region References
    
        #region FMOD Global Parameters
            [Header("Global FMOD Parameters")]
            [FMODUnity.ParamRef] [SerializeField] string fmodWindParameter;
            [FMODUnity.ParamRef] [SerializeField] string fmodWavesParameter;
        #endregion

        #region Private
            private Coroutine timer;
            private Coroutine windLerpCoroutine;
            private Coroutine waveLerpCoroutine;
        #endregion

        [Header("World Generation")]
        public Transform lastEndpoint;
        public List<RNGCell> cells;

    #endregion

    #region Properties
        float WaveIntensityFormula {
            get {
                if (state == GameState.Calm)
                    return 1f;
                else
                    return 3f; // (int) distance / 200 + randomFactor
            }
        }
        float WindSpeedFormula {
            get {
                if (state == GameState.Calm)
                    return 5f;
                else
                    return 15f; //  5f * ((int) distance / 200 + randomFactor)
            }
        }
    #endregion

    #region MonoBehaviour
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

            waitUntilCalm = new WaitForSeconds(secondsUntilCalm);
            waitUntilHostile = new WaitForSeconds(secondsUntilHostile);
        }

        private void OnEnable() {
            timer = StartCoroutine(RunStateTimer());
        }

        private void OnDisable() {
            StopCoroutine(timer);
        }

        // Start is called before the first frame update
        void Start()
        {
            onStateChanged += UpdateGameVariables;
        }

        // Update is called once per frame
        void Update()
        {
            while (cellCount < maxCellCount) SpawnCell();

            if (fmodWindParameter != null)
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName(fmodWindParameter, windSpeed);

            if (fmodWavesParameter != null)
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName(fmodWavesParameter, waveIntensity);
        }
    #endregion

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

        void UpdateGameVariables()
        {
            // Stop all previous Lerping
            if (windLerpCoroutine != null) 
                StopCoroutine(windLerpCoroutine);
            if (waveLerpCoroutine != null) 
                StopCoroutine(waveLerpCoroutine);
            
            // Start a new Lerping process
            if (state == GameState.Calm) {    
                windLerpCoroutine = 
                    StartCoroutine(LerpWindOverTime(WindSpeedFormula, secondsUntilHostile / 2f)
                );
                
                waveLerpCoroutine = 
                    StartCoroutine(LerpWavesOverTime(WaveIntensityFormula, secondsUntilHostile / 2f)
                );
            }
            else {
                windLerpCoroutine = 
                    StartCoroutine(LerpWindOverTime(WindSpeedFormula, secondsUntilCalm / 2f)
                );
                
                waveLerpCoroutine = 
                    StartCoroutine(LerpWavesOverTime(WaveIntensityFormula, secondsUntilCalm / 2f)
                );
            }
        }

        public void SwitchState()
        {
            state = state == GameState.Calm? 
                GameState.Hostile : 
                GameState.Calm
            ;

            onStateChanged?.Invoke();
        }

        IEnumerator TimeUntilCalm()
        {
            yield return waitUntilCalm;
            
            state = GameState.Calm;
            onStateChanged?.Invoke();
        }

        IEnumerator TimeUntilHostile()
        {
            yield return waitUntilHostile;
            
            state = GameState.Hostile;
            onStateChanged?.Invoke();
        }

        IEnumerator RunStateTimer() 
        {
            while (true)
            {
                if (gameStarted && !gameEnded)
                {
                    if (state == GameState.Calm)
                        yield return TimeUntilHostile();

                    if (state == GameState.Hostile)
                        yield return TimeUntilCalm();
                }
                else
                {
                    yield return null;
                }
            }
        }

        IEnumerator LerpWindOverTime(float target, float seconds = 3f)
        {
            float start = windSpeed;
            float timer = 0f;

            while ( timer < seconds ) 
            {
                timer += Time.deltaTime;
                windSpeed = Mathf.Lerp(start, target, timer / seconds);

                yield return null;
            }
        }

        IEnumerator LerpWavesOverTime(float target, float seconds = 3f)
        {
            float start = waveIntensity;
            float timer = 0f;

            while ( timer < seconds ) 
            {
                timer += Time.deltaTime;
                waveIntensity = Mathf.Lerp(start, target, timer / seconds);

                yield return null;
            }
        }

    #endregion
}
