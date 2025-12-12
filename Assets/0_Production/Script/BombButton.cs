using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BombButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // 버튼에서 드래그 시작할 때 한 번 호출
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Game.Instance.dragInfector == null) return;

        Game.Instance.dragInfector.BeginUIDrag();
        Game.Instance.dragInfector.UpdatePreviewByScreenPos(eventData.position);
    }

    // 드래그 중 매 프레임 호출 (버튼 영역 밖으로 나가도 계속 들어옴)
    public void OnDrag(PointerEventData eventData)
    {
        if (Game.Instance.dragInfector == null) return;

        Game.Instance.dragInfector.UpdatePreviewByScreenPos(eventData.position);
    }

    // 손 뗐을 때 한 번 호출
    public void OnEndDrag(PointerEventData eventData)
    {
        if (Game.Instance.dragInfector == null) return;
        // ★ 여기서 UI 위면 취소!
        if (IsPointerOverUI(eventData.position))
        {
            Game.Instance.dragInfector.CancelUIDrag();
            Debug.Log("드래그 취소 - UI 위");
            return;
        }

        Game.Instance.dragInfector.EndUIDrag(eventData.position);
    }

    bool IsPointerOverUI(Vector2 screenPos)
    {
        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        return results.Count > 0; // UI 위면 true
    }
}
