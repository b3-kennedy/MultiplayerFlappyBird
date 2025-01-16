using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkUIManager : NetworkBehaviour
{
    public GameObject buttons;

    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public void Host()
    {
        NetworkManager.Singleton.StartHost();
        buttons.SetActive(false);
    }

    public void Join()
    {
        NetworkManager.Singleton.StartClient();
        buttons.SetActive(false);
    }

    void OnClientDisconnect(ulong clientId)
    {
        Scene currentScene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.name);
    }


}
