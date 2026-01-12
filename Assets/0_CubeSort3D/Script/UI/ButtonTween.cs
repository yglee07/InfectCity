using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ButtonTween : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private Button targetButton;
    
    [Header("Animation Settings")]
    private float scaleTo = 1.2f;
   private float duration = 0.075f;
    private Ease easeType = Ease.Linear;
    
    private Vector3 originalScale;
    private Tween pressTween;
    private Tween clickTween;
    
    void Start()
    {
        // 버튼이 할당되지 않았으면 현재 오브젝트에서 찾기
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }
        
        // 원래 스케일 저장
        if (targetButton != null)
        {
            originalScale = Vector3.one;
            
            // 버튼 이벤트에 애니메이션 연결
            targetButton.onClick.AddListener(OnButtonClick);
            
            // 버튼을 누르고 있을 때 커지게 (PointerDown)
            var eventTrigger = targetButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = targetButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // PointerDown 이벤트 추가
            var pointerDownEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDownEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) => { OnButtonPress(); });
            eventTrigger.triggers.Add(pointerDownEntry);
        }
        else
        {
            Debug.LogWarning("Button component not found!");
        }
    }
    
    // 버튼을 누르고 있을 때 (PointerDown)
    private void OnButtonPress()
    {

         if(targetButton.interactable == false)
        {
            return;
        }
        // 기존 애니메이션 중지
        if (pressTween != null && pressTween.IsActive())
        {
            pressTween.Kill();
        }
        
        // 버튼을 커지게
        pressTween = targetButton.transform.DOScale(originalScale * scaleTo, duration)
            .SetEase(easeType);
    }
    
    // 버튼 클릭 완료 시 (onClick)
    private void OnButtonClick()
    {
        if(targetButton.interactable == false)
        {
            return;
        }
        // 기존 애니메이션 중지
        if (clickTween != null && clickTween.IsActive())
        {
            clickTween.Kill();
        }
        
        // 버튼을 원상태로 복원
        clickTween = targetButton.transform.DOScale(originalScale, duration)
            .SetEase(easeType);
    }
    
    // Inspector에서 직접 호출 가능한 메서드
    public void PlayAnimation()
    {
        OnButtonPress();
    }
    
    void OnDestroy()
    {
        // 클린업
        if (pressTween != null && pressTween.IsActive())
        {
            pressTween.Kill();
        }
        
        if (clickTween != null && clickTween.IsActive())
        {
            clickTween.Kill();
        }
        
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(OnButtonClick);
        }
    }
}

