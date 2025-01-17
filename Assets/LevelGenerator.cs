using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LevelGenerator : NetworkBehaviour
{

    [SerializeField] public GameObject pipeSet;
    [SerializeField] public float distanceBetweenSets;
    [SerializeField] public float spawnInterval;

    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI rematchCountText;

    public GameObject startButton;
    public GameObject rematchButton;

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

    [HideInInspector] public UnityEvent start;
    [HideInInspector] public UnityEvent playerJoined;

    public GameObject waitingText;

    public bool isPractice = false;

    public float maxCountdown;

    float countdownTimer;

    bool startCountdown;

    public GameObject killbox;

    public NetworkVariable<bool> end = new NetworkVariable<bool>(false);

    public int maxPlayerCount;

    int clientsReady;

    int rematchCount;

    public bool killBoxActive;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Physics2D.IgnoreLayerCollision(3, 6);
        Physics2D.IgnoreLayerCollision(7, 7);

        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;

        start.AddListener(StartGame);

        countdownTimer = maxCountdown;
        
    }

    [ServerRpc(RequireOwnership = false)]
    void StartCountdownServerRpc()
    {
        StartCountdownClientRpc();
    }

    [ClientRpc]
    void StartCountdownClientRpc()
    {
        startCountdown = true;
        countdownText.gameObject.SetActive(true);

    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        
        StartGameClientRpc();
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        if (killBoxActive)
        {
            killbox.SetActive(true);
        }
        
        start.Invoke();
    }

    [ClientRpc]
    void HidePlayerObjectClientRpc(ulong id, ulong clientId, int playerCount)
    {
        waitingText.SetActive(true);
        playerCountText.gameObject.SetActive(true);

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var playerObj))
        {
            playerObj.gameObject.SetActive(false);
        }

        if(NetworkManager.Singleton.LocalClientId == clientId && NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.activeSelf)
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.SetActive(false);
        }

        playerCountText.text = playerCount.ToString() + "/" + maxPlayerCount.ToString();
    }

    [ServerRpc(RequireOwnership = false)]
    void HidePlayerServerRpc()
    {
        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++) 
        {
            HidePlayerObjectClientRpc(NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.NetworkObjectId, (ulong)i, playerCount);
        }

        
    }

    [ServerRpc(RequireOwnership = false)]
    void ShowPlayerServerRpc()
    {
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
        {
            ShowPlayerClientRpc(NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.NetworkObjectId);
        }
    }

    [ClientRpc]
    void ShowPlayerClientRpc(ulong objectId)
    {
        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var playerObj))
        {
            playerObj.gameObject.SetActive(true);
            waitingText.SetActive(false);
            playerCountText.gameObject.SetActive(false);
            startButton.SetActive(false);
        }
    }

    public void StartFromButton()
    {
        rematchCountText.gameObject.SetActive(false);

        rematchCount = 0;
        
        
        ShowPlayerServerRpc();
        StartCountdownServerRpc();
    }

    

    public void RematchButtonPress()
    {
        rematchButton.GetComponent<Button>().interactable = false;
        RematchServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RematchServerRpc()
    {
        rematchCount++;
        UpdateRematchTextClientRpc(NetworkManager.Singleton.ConnectedClients.Count, rematchCount);
        if(rematchCount >= NetworkManager.Singleton.ConnectedClients.Count)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                var player = client.Value.PlayerObject;
                player.GetComponent<PlayerMovement>().isOut.Value = false;
                player.transform.position = Vector3.zero;
            }
            end.Value = false;
            RematchClientRpc(NetworkManager.Singleton.ConnectedClients.Count);
        }
        
    }

    [ClientRpc]
    void UpdateRematchTextClientRpc(int playerCount, int rematchCount)
    {
        rematchCountText.text = rematchCount.ToString() + "/" + playerCount.ToString();
        Debug.Log(rematchCount);
    }

    [ClientRpc]
    void RematchClientRpc(int playerCount)
    {
        rematchButton.SetActive(false);
        winText.gameObject.SetActive(false);
        StartFromButton();
        
    }


    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        playerJoined.Invoke();

        if (!isPractice)
        {
            if (IsServer) 
            {
                if(NetworkManager.Singleton.ConnectedClients.Count >= 2)
                {
                    startButton.SetActive(true);
                }
                
                if (NetworkManager.Singleton.ConnectedClients.Count < maxPlayerCount)
                {
  
                    HidePlayerServerRpc();


                }
                else if (NetworkManager.Singleton.ConnectedClients.Count >= maxPlayerCount)
                {
                    ShowPlayerServerRpc();
                }
            }

        }


        if (obj == 0)
        {


            if (IsServer)
            {


                speed.Value = 3;
            }
        }

        if (isPractice)
        {

            waitingText.SetActive(false);
            NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.SetActive(true);
            StartGameServerRpc();
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
            if(!end.Value && NetworkManager.ConnectedClients.Count >= maxPlayerCount && !canStart.Value)
            {
                StartCountdownServerRpc();
            }
        }

        if (startCountdown)
        {
            countdownTimer -= Time.deltaTime;
            countdownText.text = (countdownTimer+1).ToString("F0");
            if(countdownTimer <= 0)
            {
                
                countdownText.text = "GO";
                if (!IsServer)
                {
                    ClientIsReadyServerRpc();
                }

                if (countdownTimer <= -1)
                {


                    countdownText.gameObject.SetActive(false);
                    countdownTimer = maxCountdown;
                    startCountdown = false;
                }

            }
        }

    }

    [ServerRpc(RequireOwnership = false)]
    void ClientIsReadyServerRpc()
    {
        clientsReady++;
        if(clientsReady >= NetworkManager.Singleton.ConnectedClients.Count)
        {
            StartGameServerRpc();
            HideCountdownTextClientRpc();
        }

    }

    [ClientRpc]
    void HideCountdownTextClientRpc()
    {
        countdownText.gameObject.SetActive(false);
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
