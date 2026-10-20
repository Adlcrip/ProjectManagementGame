using UnityEngine;
using UnityEngine.EventSystems;

// Added automatically by CashBox to every bill/coin placed on the change tray.
// Clicking any of them confirms the change.
public class TrayMoneyClick : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public CashBox cashBox;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cashBox != null)
            cashBox.ConfirmChange();
    }
}