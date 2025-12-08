using UnityEngine;

public class CheatManager : MonoBehaviour
{
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SaveSystem.Data.coin += 10;
            SaveSystem.Save();

            Debug.Log("Cheat: +10 coin");

            // 🔥 로비 켜져 있으면 자동 갱신
            if (GameManager.Instance.lobbyView.activeInHierarchy)
            {
                Lobby lobby = GameManager.Instance.lobbyView.GetComponent<Lobby>();
                if (lobby != null) lobby.RefreshLobby();
            }
        }
#endif
    }

}
