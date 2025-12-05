using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("View Groups")]
    public GameObject lobbyView;    // View/Lobby
    public GameObject gameView;     // View/Game

    [Header("UI Groups")]
    public GameObject uiLobby;      // UI/UI_Lobby
    public GameObject uiGameHUD;    // UI/UI_GameHUD

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
        // View 활성화
        lobbyView.SetActive(true);
        gameView.SetActive(false);

        // UI 활성화
        uiLobby.SetActive(true);
        uiGameHUD.SetActive(false);

        // 로비 새로고침
        Lobby lobby = lobbyView.GetComponent<Lobby>();
        if (lobby != null)
            lobby.RefreshLobby();
    }

    // ============================
    //         GAME
    // ============================
    public void StartGame()
    {
        // View 활성화
        lobbyView.SetActive(false);
        gameView.SetActive(true);

        // UI 활성화
        uiLobby.SetActive(false);
        uiGameHUD.SetActive(true);

        // 게임 로직 시작
        LevelManager.Instance.LoadLevel(0);
    }

    public void ReturnToLobby()
    {
        ShowLobby();
    }

    public void OnGameClear()
    {
        SaveSystem.Data.stage++;
        SaveSystem.Save();

        ReturnToLobby();
    }
}
