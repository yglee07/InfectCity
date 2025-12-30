using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Game.Instance.dragUnit == null) return;

        Game.Instance.dragUnit.BeginUIDrag();
        Game.Instance.dragUnit.UpdatePreviewByScreenPos(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Game.Instance.dragUnit == null) return;

        Game.Instance.dragUnit.UpdatePreviewByScreenPos(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Game.Instance.dragUnit == null) return;

        if (IsPointerOverUI(eventData.position))
        {
            Game.Instance.dragUnit.CancelUIDrag();
            Debug.Log("유닛 드래그 취소 - UI 위");
            return;
        }

        Game.Instance.dragUnit.EndUIDrag(eventData.position);
    }

    bool IsPointerOverUI(Vector2 screenPos)
    {
        PointerEventData ped = new PointerEventData(EventSystem.current);
        ped.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        return results.Count > 0;
    }
}
