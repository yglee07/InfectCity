using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("View Groups")]
    public GameObject lobbyView;
    public GameObject gameView;

    [Header("UI Groups")]
    public GameObject uiLobby;
    public GameObject uiGameHUD;



    void Awake()
    {
        SaveSystem.Load();
        Instance = this;
    }

    void Start()
    {
        ShowLobby();
    }

    // ============================
    //         LOBBY
    // ============================
    public void ShowLobby()
    {
        lobbyView.SetActive(true);
        gameView.SetActive(false);

        uiLobby.SetActive(true);
        uiGameHUD.SetActive(false);

        Lobby lobby = lobbyView.GetComponent<Lobby>();
        if (lobby != null)
            lobby.RefreshLobby();
    }

    // ============================
    //         GAME
    // ============================
    public void StartGame()
    {
        lobbyView.SetActive(false);
        gameView.SetActive(true);

        uiLobby.SetActive(false);
        uiGameHUD.SetActive(true);

 
    }

    // ============================
    //     RETURN TO LOBBY
    // ============================
    public void ReturnToLobby()
    {
        uiLobby.SetActive(true);
        uiGameHUD.SetActive(false);

        lobbyView.SetActive(true);
        gameView.SetActive(false);

        Lobby lobby = lobbyView.GetComponent<Lobby>();
        if (lobby != null)
            lobby.RefreshLobby();
    }

    // ============================
    //     GAME CLEAR EVENT
    // ============================
    public void OnGameClear()
    {
        SaveSystem.Data.stage++;
        SaveSystem.Save();

        ReturnToLobby();
    }

 

}
