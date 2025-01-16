using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class LevelGenerator : NetworkBehaviour
{

    [SerializeField] public GameObject pipeSet;
    [SerializeField] public float distanceBetweenSets;
    [SerializeField] public float spawnInterval;

    public NetworkVariable<float> speed;
    

    [Range(1f, 2f)]
    [SerializeField] public float speedMultiplier;

    [SerializeField] public float speedChangeInterval;
    [SerializeField] public Transform pipeSpawnPos;
    [SerializeField] public float minHeight;
    [SerializeField] public float maxHeight;

    public AnimationCurve speedIncrease;
    public float speedIncreaseDuration;
    float t;

    public List<GameObject> spawnPipes = new List<GameObject>();


    float tempSpeed;
    bool speedChanged;

    float moveTimer;
    float spawnTimer;
    Rigidbody2D rb;

    public static LevelGenerator Instance;

    public NetworkVariable<bool> canStart  = new NetworkVariable<bool>(false);

    public UnityEvent start;


    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Physics2D.IgnoreLayerCollision(3, 6);

        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;

        start.AddListener(StartGame);
        
    }

    [ServerRpc(RequireOwnership = false)]
    void StartGameServerRpc()
    {
        StartGameClientRpc();
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        start.Invoke();
    }

    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        Physics.IgnoreLayerCollision(3, 6);
        if (obj == 0)
        {
            if (IsServer)
            {
                speed.Value = 3;
            }
        }

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    // Update is called once per frame
    void Update()
    {
        if (canStart.Value)
        {
            Generate();
            MoveLevel();
        }


        if (IsServer)
        {
            if(NetworkManager.ConnectedClients.Count >= 2)
            {
                StartGameServerRpc();
            }
        }

    }

    void StartGame()
    {
        if (IsServer)
        {
            canStart.Value = true;
        }
        
    }

    void MoveLevel()
    {
        if (IsServer)
        {
            moveTimer += Time.deltaTime;
            if (moveTimer >= speedChangeInterval)
            {
                if (tempSpeed != speed.Value && !speedChanged)
                {
                    tempSpeed = speed.Value;
                    speedChanged = true;
                }

                if (t < 1f)
                {
                    t += Time.deltaTime / speedIncreaseDuration;
                    t = Mathf.Clamp01(t);
                }

                speed.Value = Mathf.Lerp(speed.Value, tempSpeed * (speedMultiplier), speedIncrease.Evaluate(t));
                if (Mathf.Approximately(speed.Value, tempSpeed * speedMultiplier))
                {
                    moveTimer = 0;
                    speedChanged = false;
                    t = 0;
                }
            }
        }

        rb.linearVelocity = -Vector2.right * speed.Value;
    }

    void Generate()
    {
        if (!IsServer) return;

        if(spawnPipes.Count == 0)
        {
            SpawnPipeSet();
        }
        else
        {
            Vector2 pipePos = new Vector2(spawnPipes[spawnPipes.Count - 1].transform.position.x, 0);
            Vector2 spawnPos = new Vector2(pipeSpawnPos.position.x, 0);


            if (Vector2.Distance(pipePos, spawnPos) >= distanceBetweenSets)
            {
                SpawnPipeSet();
            }
        }
    }


    void SpawnPipeSet()
    {
        GameObject newPipe = Instantiate(pipeSet, new Vector2(pipeSpawnPos.position.x, Random.Range(minHeight, maxHeight)), Quaternion.identity);
        spawnPipes.Add(newPipe);
        newPipe.GetComponent<NetworkObject>().Spawn();
        newPipe.transform.SetParent(transform);

        if(spawnPipes.Count >= 7)
        {
            Destroy(spawnPipes[0]);
            spawnPipes.RemoveAt(0);
        }
    }
}
