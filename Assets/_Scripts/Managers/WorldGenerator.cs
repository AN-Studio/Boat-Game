using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGenerator : Singleton<WorldGenerator>
{
    #region Data Structures
        [System.Serializable]
        public class Biome
        {
            public string name;
            [Range(1,3)] public int dangerLevel = 1;
            public RandomizedList<Cell> cells;
            public bool IsEmpty {
                get => cells.Count == 0;
            }
        }

        [System.Serializable]
        public class Settings
        {
            public int maxActiveCells = 4;
            [Range(1,3)] public int dangerLevel = 1;
            public Vector2Int cellsUntilNewBiome = new Vector2Int(5,10);
        }

    #endregion

    #region References
        [Header("References")]
        public Cell portCell;
        public Transform lastEndpoint;
        private Transform gameWorld;
        private Route route;
    #endregion

    [Space]
    #region Variables
        public Settings settings;
        private int activeCellCount = 1;
        private int cellSpawnCount = 1;
        private int cellsUntilNewBiome = 0;
        private int checkpointsReached = 0;
    #endregion

    #region World Cells
        private Biome currentBiome;
        public List<WeightedItem<Biome>> biomes;
        private RandomizedList<Biome> routeBiomes;
    #endregion

    #region Properties
        public Route Route {
            get => route;
        } 

        public float RouteProgress {
            get => Mathf.Clamp01(checkpointsReached / (float) route.length);
        }
    #endregion

    protected override void Awake() 
    {
        base.Awake();
        gameWorld = GameObject.Find("Game World").transform;
    }

    protected void Start() 
    {
        currentBiome = biomes[0].item;
        cellsUntilNewBiome = Random.Range(settings.cellsUntilNewBiome.x, settings.cellsUntilNewBiome.y);

        StartCoroutine(BiomeController());
        
        SetRouteTo(ScriptableObject.CreateInstance("Route") as Route);
    }

    void Update() 
    {
        while (cellSpawnCount <= route.length && activeCellCount < settings.maxActiveCells) 
            SpawnCell();
    }

    public void TickCheckpoint() => checkpointsReached++;

    void SetRouteTo(Route route)
    {
        this.route = route;
        routeBiomes = new RandomizedList<Biome>(
            from biome in biomes
            where biome.item.dangerLevel <= route.dangerLevel && !biome.item.IsEmpty
            select biome
        );
    }

    void SpawnCell()
    {
        Cell cell;
        if (cellSpawnCount == route.length)
            cell = portCell;
        else
            cell = currentBiome.cells.GetRandom();

        Cell instance  = Instantiate(cell, lastEndpoint.position, Quaternion.identity, gameWorld);
        lastEndpoint = instance.EndPoint;
        
        activeCellCount++;
        cellSpawnCount++;
        cellsUntilNewBiome--;
    }

    public void DespawnCell(Cell instance)
    {
        activeCellCount--;

        Destroy(instance);
    }

    IEnumerator BiomeController()
    {
        while (true)
        {
            if (cellsUntilNewBiome <= 0)
            {
                currentBiome = routeBiomes.GetRandom();
                cellsUntilNewBiome = Random.Range(settings.cellsUntilNewBiome.x, settings.cellsUntilNewBiome.y);
            }

            yield return null;
        }
    }

}