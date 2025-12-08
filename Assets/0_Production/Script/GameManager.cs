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

    public ControlMode controlMode = ControlMode.Infect;

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
        ActivateLobbyView();
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
        ActivateLobbyView();
    }
    private void ActivateLobbyView()
    {
        CameraController cam = Camera.main.GetComponent<CameraController>();
        if (cam != null)
            cam.SnapToOrigin();

        lobbyView.SetActive(true);
        gameView.SetActive(false);

        uiLobby.SetActive(true);
        uiGameHUD.SetActive(false);

        Lobby lobby = lobbyView.GetComponent<Lobby>();
        if (lobby != null)
            lobby.RefreshLobby();
    }
    // ============================
    //     GAME CLEAR EVENT
    // ============================
    //public void OnGameClear()
    //{
    //    SaveSystem.Data.stage++;
    //    SaveSystem.Data.coin += 10;
    //    SaveSystem.Save();

    //    ReturnToLobby();
    //}

 

}
