﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{

    #region Singleton
    public static LevelGenerator Instance;

    private void Awake()
    {
        Instance = this;
    }
    #endregion

    [System.Serializable]
    public class MultiPool
    {
        public string tag;
        public GameObject prefab1;
        public GameObject prefab2;
        public GameObject prefab3;
        public int size;
    }

    [System.Serializable]
    public class SinglePool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    Dictionary<string, Queue<GameObject>> LvlComponentDict; //for background use mainly
    Dictionary<string, Queue<Obstacle>> ObstacleDict; //for obstacle use mainly
    public List<MultiPool> levels;
    public List<SinglePool> obstacles;
    public GameObject StartLvl;
    GameObject StartLevel; //for code use
    GameManager game;
    public float transitionSpeed;
    Vector3 levelOffset = new Vector3(0, 10.8f, 0); // used to offset a level when it is spawned so that is spawns above the active level
    GameObject NextLvl;
    GameObject CurrentLvl;
    Obstacle NextObstacle;
    Obstacle CurrentObstacle;
    Obstacle PreviousObstacle;
    bool currentlyTransitioning;
    bool playedOnce; // ateast one game session has been started since opening app, used to get rid of NullReferenceException when GameOverConfirmed() is called after app is opened
    Vector3[] travelPath1;
    bool obstacleDespawned;
    int obstacleSpawnCounter;

    public delegate void LevelDelegate();
    public static event LevelDelegate TransitionDone;
    public static event LevelDelegate NextLvlGenerated;

    private void OnEnable()
    {
        GameManager.GameStarted += GameStarted;
        GameManager.GameOverConfirmed += GameOverConfirmed;
        EnemyBehavior.AbsorbDone += AbsorbDone;
        EnemyBehavior.AbsorbDoneAndRichochet += AbsorbDone;
        GameManager.MoveToNextLvl += MoveToNextLvl;
    }

    private void OnDisable()
    {
        GameManager.GameStarted -= GameStarted;
        GameManager.GameOverConfirmed -= GameOverConfirmed;
        EnemyBehavior.AbsorbDone -= AbsorbDone;
        EnemyBehavior.AbsorbDoneAndRichochet -= AbsorbDone;
        GameManager.MoveToNextLvl -= MoveToNextLvl;
    }

    public class Obstacle
    {
        public GameObject gameObject;
        public Transform transform;
        public Vector3[] path;

        public Obstacle(GameObject go)
        {
            this.transform = go.transform.Find("TargetTravelPath");
            this.gameObject = go;
            this.path = new Vector3[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                this.path[i] = transform.GetChild(i).localPosition;
            }
        }
    }

    private void Start()
    {
        obstacleSpawnCounter = 0;
        obstacleDespawned = false;

        game = GameManager.Instance;

        currentlyTransitioning = false;

        StartLevel = Instantiate(StartLvl, transform);
        CurrentLvl = StartLevel;

        LvlComponentDict = new Dictionary<string, Queue<GameObject>>();
        ObstacleDict = new Dictionary<string, Queue<Obstacle>>();

        foreach (MultiPool pool in levels)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj1 = Instantiate(pool.prefab1);
                obj1.SetActive(false);
                objectPool.Enqueue(obj1);

                GameObject obj2 = Instantiate(pool.prefab2);
                obj2.SetActive(false);
                objectPool.Enqueue(obj2);

                GameObject obj3 = Instantiate(pool.prefab3);
                obj3.SetActive(false);
                objectPool.Enqueue(obj3);
            }

            LvlComponentDict.Add(pool.tag, objectPool);
        }

        foreach (SinglePool pool in obstacles)
        {
            Queue<Obstacle> obstaclePool = new Queue<Obstacle>();

            for (int i = 0; i < pool.size; i++)
            {
                Obstacle obstacle = new Obstacle(Instantiate(pool.prefab));
                obstacle.gameObject.SetActive(false);
                obstaclePool.Enqueue(obstacle);
            }

            ObstacleDict.Add(pool.tag, obstaclePool);
        }

    }

    void GameOverConfirmed()
    {
        obstacleSpawnCounter = 0;
        obstacleDespawned = false;

        if (playedOnce)
        {
            #region Disactivates All Level Prefabs
            foreach (GameObject obj in LvlComponentDict["Level2"])
            {
                obj.SetActive(false);
            }
            foreach (GameObject obj in LvlComponentDict["Level3"])
            {
                obj.SetActive(false);
            }
            foreach (GameObject obj in LvlComponentDict["Level4"])
            {
                obj.SetActive(false);
            }
            foreach (GameObject obj in LvlComponentDict["Level5"])
            {
                obj.SetActive(false);
            }
            #endregion

            #region Disactivates All Obstacle Prefabs
            foreach (Obstacle ob in ObstacleDict["Obstacle1_Lvl2"])
            {
                ob.gameObject.SetActive(false);
            }
            foreach (Obstacle ob in ObstacleDict["Obstacle2_Lvl2"])
            {
                ob.gameObject.SetActive(false);
            }
            foreach (Obstacle ob in ObstacleDict["Obstacle3_Lvl2"])
            {
                ob.gameObject.SetActive(false);
            }
            foreach (Obstacle ob in ObstacleDict["Obstacle4_Lvl2"])
            {
                ob.gameObject.SetActive(false);
            }
            foreach (Obstacle ob in ObstacleDict["Obstacle5_Lvl2"])
            {
                ob.gameObject.SetActive(false);
            }
            #endregion

            CurrentLvl = StartLevel;
            CurrentLvl.transform.position = Vector3.zero;
        }
    }

    public GameObject SpawnFromPool(string tag, Vector2 position, Quaternion rotation)
    {
        GameObject objectToSpawn = LvlComponentDict[tag].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        LvlComponentDict[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }

    public Obstacle SpawnFromObstacles(string tag, Vector2 position, Quaternion rotation)
    {
        obstacleSpawnCounter++;

        Obstacle obstacleToSpawn = ObstacleDict[tag].Dequeue();

        obstacleToSpawn.gameObject.SetActive(true);
        obstacleToSpawn.gameObject.transform.position = position;
        obstacleToSpawn.gameObject.transform.rotation = rotation;

        ObstacleDict[tag].Enqueue(obstacleToSpawn);

        return obstacleToSpawn;
    }

    void AbsorbDone()
    {
        PreviousObstacle = CurrentObstacle;
        currentlyTransitioning = true;
    }

    void MoveToNextLvl()
    {
        PreviousObstacle = CurrentObstacle;
        currentlyTransitioning = true;
    }

    private void FixedUpdate()
    {
        if (currentlyTransitioning)
        {
            CurrentLvl.transform.position = Vector2.MoveTowards(CurrentLvl.transform.position, this.transform.position - levelOffset, Time.deltaTime * transitionSpeed);
            NextLvl.transform.position = Vector2.MoveTowards(NextLvl.transform.position, Vector3.zero, Time.deltaTime * transitionSpeed);

            if (NextLvl.transform.position == Vector3.zero)
            {
                CurrentLvl = NextLvl;
                CurrentObstacle = NextObstacle;
                TransitionDone();
                GenerateNextLvl();
                currentlyTransitioning = false;

                if (obstacleDespawned)
                {
                    PreviousObstacle.transform.localPosition = Vector3.right * 1000;
                }
            }
        }
    }

    void GameStarted()
    {
        //NextLvl = SpawnFromPool("Level2", transform.position + levelOffset, transform.rotation);
        //NextLvlGenerated();
        GenerateNextLvl();
        playedOnce = true;
    }

    void GenerateNextLvl()
    {
        if (game.GetScore >= 0 && game.GetScore < 1)
        {
            NextLvl = SpawnFromPool("Level2", transform.position + levelOffset, transform.rotation);
        }
        /*
        if (game.GetScore >= 2 && game.GetScore < 4)
        {
            NextLvl = SpawnFromPool("Level3", transform.position + levelOffset, transform.rotation);
        }
        if (game.GetScore >= 4 && game.GetScore < 6)
        {
            NextLvl = SpawnFromPool("Level4", transform.position + levelOffset, transform.rotation);
            NextObstacle = SpawnFromObstacles("Obstacle" + Random.Range(1, 1) + "_Lvl4", transform.position + levelOffset, transform.rotation);
            NextObstacle.go.transform.parent = NextLvl.transform;
        }
        */
        if (game.GetScore >= 1)
        {
            if (obstacleSpawnCounter == 2)
            {
                obstacleDespawned = true;
            }
            NextLvl = SpawnFromPool("Level5", transform.position + levelOffset, transform.rotation);
            NextObstacle = SpawnFromObstacles("Obstacle" + Random.Range(1, 6) + "_Lvl2", transform.position + levelOffset, transform.rotation);
            NextObstacle.gameObject.transform.parent = NextLvl.transform;
        }
        NextLvlGenerated();
    }

    public Transform GetNextLvl
    {
        get
        {
            return NextLvl.transform;
        }
    }

    public Transform GetCurrentLvl
    {
        get
        {
            return CurrentLvl.transform;
        }
    }

    public Vector3[] GetNextObstaclePath
    {
        get
        {
            return NextObstacle.path;
        }
    }

    public Vector3[] GetCurrentObstaclePath
    {
        get
        {
            return CurrentObstacle.path;
        }
    }
}
