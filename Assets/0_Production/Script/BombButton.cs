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

        Game.Instance.dragInfector.EndUIDrag(eventData.position);
    }
}
