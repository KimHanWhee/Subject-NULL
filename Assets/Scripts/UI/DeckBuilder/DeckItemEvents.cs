using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 덱 편성 UI 아이템 공용 이벤트 릴레이 — 컨트롤러(DeckBuilderUI)가 콜백을 주입.
// Button 대신 쓰는 이유: 클릭 + 호버(툴팁) 3종을 한 컴포넌트로 처리.
public class DeckItemEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Action onClick;
    public Action onEnter;
    public Action onExit;

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left) onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData e) => onEnter?.Invoke();
    public void OnPointerExit(PointerEventData e) => onExit?.Invoke();
}
