using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{

    Rigidbody2D rb;
    [SerializeField] public float jumpForce;
    Animator anim;
    Animator otherAnim;

    public float tiltAngle = 30f;
    public float tiltSpeed = 5f;

    public SpriteRenderer sr;
    public Sprite blueSprite;
    public AnimationClip blueFlap;

    string stateName = "UP";

    public NetworkVariable<bool> isOut = new NetworkVariable<bool>(false);

    bool paused;
    bool started;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

        

        

    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;
        LevelGenerator.Instance.start.AddListener(OnGameStart);
    }

    void OnGameStart()
    {
        rb.simulated = true;
    }

    public override void OnNetworkSpawn()
    {


        if (!IsOwner)
        {
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(true);
            anim = transform.GetChild(1).GetComponent<Animator>();
            otherAnim = transform.GetChild(0).GetComponent<Animator>();
        }
        else
        {
            transform.GetChild(0).gameObject.SetActive(true);
            transform.GetChild(1).gameObject.SetActive(false);
            anim = transform.GetChild(0).GetComponent<Animator>();
            otherAnim = transform.GetChild(1).GetComponent<Animator>();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ServerPauseServerRpc(bool value)
    {
        ServerPauseClientRpc(value);
        paused = value;
    }


    [ClientRpc]
    void ServerPauseClientRpc(bool value)
    {
        if (value)
        {
            Time.timeScale = 0.00001f;
        }
        else
        {
            Time.timeScale = 1f;
        }
        
    }
    

    // Update is called once per frame
    void Update()
    {


        if (!IsOwner) return;


        if (IsServer)
        {
            if (Input.GetKeyDown(KeyCode.P) && !paused)
            {
                ServerPauseServerRpc(true);

            }
            else if (Input.GetKeyDown(KeyCode.P) && paused)
            {
                ServerPauseServerRpc(false);
            }
        }


        if(Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Fire1"))
        {
            anim.ResetTrigger("flap");
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            anim.SetTrigger("flap");
            PlayAnimationServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        float verticalVelocity = rb.linearVelocityY;

        // Calculate the target tilt angle based on the vertical velocity
        float targetAngle = 0f;

        if (verticalVelocity > 0) // Moving up
        {
            targetAngle = tiltAngle; // Tilt up
        }
        else if (verticalVelocity < 0) // Falling
        {
            targetAngle = -tiltAngle; // Tilt down
        }

        // Smoothly rotate the object towards the target angle
        float currentAngle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, Time.deltaTime * tiltSpeed);
        transform.eulerAngles = new Vector3(0, 0, currentAngle);
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return;

        if (other.transform.CompareTag("KillBox"))
        {
            HitKillBoxServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }



    [ServerRpc(RequireOwnership = false)]
    void HitKillBoxServerRpc(ulong clientId)
    {
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerMovement>().isOut.Value = true;
        Debug.Log(clientId.ToString() + " has hit the killbox");
        int outPlayers = 0;


        for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++) 
        {
            if (NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.GetComponent<PlayerMovement>().isOut.Value)
            {
                outPlayers++;
            }


        }


        if (outPlayers >= LevelGenerator.Instance.maxPlayerCount - 1)
        {
            
            ulong winningPlayer = 999;
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
            {
                ulong id = NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.NetworkObjectId;
                if (!NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.GetComponent<PlayerMovement>().isOut.Value)
                {
                    winningPlayer = NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.OwnerClientId;
                }
                HidePlayerClientRpc(id);

            }
            EndGameClientRpc(winningPlayer);

        }


    }

    [ClientRpc]
    void HidePlayerClientRpc(ulong id)
    {
        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var playerObj))
        {
            playerObj.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    void EndGameClientRpc(ulong clientId)
    {
        if (IsServer)
        {
            LevelGenerator.Instance.canStart.Value = false;
            LevelGenerator.Instance.speed.Value = 3;
        }

        LevelGenerator.Instance.end = true;

        for (int i = LevelGenerator.Instance.spawnPipes.Count - 1; i >= 0; i--)
        {

            Destroy(LevelGenerator.Instance.spawnPipes[i]);
        }
        NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.SetActive(false);

        
        LevelGenerator.Instance.winText.text = "Player " + clientId.ToString() + " has won!";
        LevelGenerator.Instance.winText.gameObject.SetActive(true);
        LevelGenerator.Instance.transform.position = Vector3.zero;
    }

    [ServerRpc(RequireOwnership = false)]
    void PlayAnimationServerRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            GameObject playerObject = networkClient.PlayerObject.gameObject;
            PlayAnimationClientRpc(playerObject.GetComponent<NetworkObject>().NetworkObjectId, clientId);
        }
        
    }

    [ClientRpc]
    void PlayAnimationClientRpc(ulong objectId, ulong clientId)
    {
        if(NetworkManager.Singleton.LocalClientId != clientId)
        {
            GameObject playerSprite = null;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var obj))
            {
                for (var i = 0; i < obj.transform.childCount; i++)
                {
                    if (obj.gameObject.activeSelf)
                    {
                        playerSprite = obj.transform.GetChild(i).gameObject;
                    }
                }
                playerSprite.GetComponent<Animator>().SetTrigger("flap");
            }
        }

    }
}
