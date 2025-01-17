using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : NetworkBehaviour
{

    Rigidbody2D rb;
    float jumpForce;
    float fallForce;

    Animator anim;
    Animator otherAnim;

    public float tiltAngle = 30f;
    public float tiltSpeed = 5f;

    public SpriteRenderer sr;
    public Sprite blueSprite;
    public AnimationClip blueFlap;

    public float minJumpForce;
    public float maxJumpForce;
    public float jumpChargeMultiplier;
    string stateName = "UP";

    public NetworkVariable<bool> isOut = new NetworkVariable<bool>(false);

    bool paused;
    bool started;
    bool chargeJump;
    bool addForceUp;
    bool addForceDown;
    bool chargeFall;

    float jumpTimer;

    public AudioSource bonkSource;
    public AudioSource flapSource;
    public AudioSource grassLandingSource;

    bool hasCollided;
    bool hasPlayedSound;

    float playedSoundTimer;

    Collision2D collision2D;


    void Start()
    {
        jumpForce = minJumpForce;
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

    void Update()
    {
        if (!IsOwner) return;

        if (hasPlayedSound)
        {
            playedSoundTimer += Time.deltaTime;
            if(playedSoundTimer >= 1f)
            {
                hasPlayedSound = false;
                playedSoundTimer = 0f;
            }
        }

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

        if (chargeJump)
        {
            jumpTimer += Time.deltaTime;
            if(jumpTimer >= 0.1f) 
            {
                jumpForce += Time.deltaTime * jumpChargeMultiplier;
            }
            
        }

        if (chargeFall)
        {
            fallForce += Time.deltaTime * jumpChargeMultiplier;
        }

        jumpForce = Mathf.Clamp(jumpForce, minJumpForce, maxJumpForce);
        fallForce = Mathf.Clamp(fallForce, minJumpForce, maxJumpForce);

        if (Input.GetKeyDown(KeyCode.W) || Input.GetButtonDown("Fire1"))
        {
            chargeJump = true;
            if (LevelGenerator.Instance.isPractice && LevelGenerator.Instance.end.Value)
            {
                rb.simulated = true;
                LevelGenerator.Instance.StartGameServerRpc();
            }
        }
        else if (Input.GetKeyUp(KeyCode.W) || Input.GetButtonUp("Fire1"))
        {
            chargeJump = false;
            anim.ResetTrigger("flap");
            addForceUp = true;
            flapSource.pitch = Random.Range(0.8f, 1.2f);
            flapSource.Play();
            anim.SetTrigger("flap");
            PlayAnimationServerRpc(NetworkManager.Singleton.LocalClientId);
            
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetButtonDown("Fire2"))
        {
            chargeFall = true;
        }
        else if (Input.GetKeyUp(KeyCode.S) || Input.GetButtonUp("Fire2"))
        {
            chargeJump = false;
            flapSource.pitch = Random.Range(0.8f, 1.2f);
            flapSource.Play();
            anim.ResetTrigger("flap");
            addForceDown = true;
            anim.SetTrigger("flap");
            PlayAnimationServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        float verticalVelocity = rb.linearVelocityY;
        float targetAngle = 0f;

        if (verticalVelocity > 0)
        {
            targetAngle = tiltAngle;
        }
        else if (verticalVelocity < 0)
        {
            targetAngle = -tiltAngle;
        }

        float currentAngle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, Time.deltaTime * tiltSpeed);
        transform.eulerAngles = new Vector3(0, 0, currentAngle);
    }

    void FixedUpdate()
    {
        if (addForceUp)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            addForceUp = false;
            jumpForce = minJumpForce;
            jumpTimer = 0;
        }

        if (addForceDown)
        {
            if(rb.linearVelocityY > 0)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(Vector2.down * (fallForce / 2), ForceMode2D.Impulse);
            }
            else if(rb.linearVelocityY < 0)
            {
                rb.AddForce(Vector2.down * (fallForce / 5), ForceMode2D.Impulse);
            }

            addForceDown = false;
            jumpForce = minJumpForce;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return;

        if (other.transform.CompareTag("KillBox"))
        {
            transform.position = Vector3.zero;
            HitKillBoxServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }



    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!IsOwner) return;


        hasCollided = true;
        if(other.transform.CompareTag("Pipe") && hasCollided)
        {
            if (collision2D == null)
            {
                bonkSource.pitch = Random.Range(0.8f, 1.2f);
                bonkSource.Play();
                PlayBonkServerRpc();
                hasPlayedSound = true;
                collision2D = other;
            }

            
        }

        if (other.transform.CompareTag("Floor"))
        {
            if (collision2D == null)
            {
                grassLandingSource.pitch = Random.Range(0.8f, 1.2f);
                grassLandingSource.Play();
                PlayGrassLandingSoundServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PlayGrassLandingSoundServerRpc()
    {
        PlayGrassLandingSoundClientRpc();
    }

    [ClientRpc]
    void PlayGrassLandingSoundClientRpc()
    {
        grassLandingSource.pitch = Random.Range(0.8f, 1.2f);
        grassLandingSource.Play();
    }


    [ServerRpc(RequireOwnership = false)]
    void PlayBonkServerRpc()
    {
        PlayBonkClientRpc();
    }

    [ClientRpc]
    void PlayBonkClientRpc()
    {
        bonkSource.pitch = Random.Range(0.8f, 1.2f);
        bonkSource.Play();
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        collision2D = null;
        hasCollided = false;
    }


    [ServerRpc(RequireOwnership = false)]
    void HitKillBoxServerRpc(ulong clientId)
    {
        if (!LevelGenerator.Instance.isPractice)
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerMovement>().isOut.Value = true;

            int outPlayers = 0;

            for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
            {
                if (NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.GetComponent<PlayerMovement>().isOut.Value)
                {
                    outPlayers++;
                }
            }

            if (outPlayers >= NetworkManager.Singleton.ConnectedClients.Count - 1)
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
                EndGameClientRpc(winningPlayer, NetworkManager.Singleton.ConnectedClients.Count);
            }
        }
        else
        {
            LevelGenerator.Instance.canStart.Value = false;
            LevelGenerator.Instance.end.Value = true;
            LevelGenerator.Instance.speed.Value = 3;
            transform.position = Vector3.zero;
            LevelGenerator.Instance.transform.position = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
            rb.simulated = false;
            for (int i = LevelGenerator.Instance.spawnPipes.Count - 1; i >= 0; i--)
            {
                Destroy(LevelGenerator.Instance.spawnPipes[i]);
            }
            LevelGenerator.Instance.spawnPipes.Clear();
        }
    }

    [ClientRpc]
    void HidePlayerClientRpc(ulong id)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var playerObj))
        {
            playerObj.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    void EndGameClientRpc(ulong clientId, int playerCount)
    {
        if (IsServer)
        {
            LevelGenerator.Instance.canStart.Value = false;
            LevelGenerator.Instance.end.Value = true;
            LevelGenerator.Instance.speed.Value = 3;
        }

        for (int i = LevelGenerator.Instance.spawnPipes.Count - 1; i >= 0; i--)
        {
            Destroy(LevelGenerator.Instance.spawnPipes[i]);
        }

        LevelGenerator.Instance.rematchCountText.gameObject.SetActive(true);
        LevelGenerator.Instance.rematchCountText.text = "0/" + playerCount.ToString();

        LevelGenerator.Instance.spawnPipes.Clear();

        NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.SetActive(false);
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Rigidbody2D>().simulated = false;

        LevelGenerator.Instance.rematchButton.GetComponent<Button>().interactable = true;
        LevelGenerator.Instance.rematchButton.SetActive(true);
        LevelGenerator.Instance.winText.text = "Player " + (clientId + 1).ToString() + " has won!";
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
        if (NetworkManager.Singleton.LocalClientId != clientId)
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
                if (playerSprite.GetComponent<Animator>())
                {
                    playerSprite.GetComponent<Animator>().SetTrigger("flap");
                }
                
            }
        }
    }
}
