using UnityEngine;
using UnityEngine.EventSystems;

public class TrayMoneyClick : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public CashBox cashBox;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cashBox != null)
            cashBox.ConfirmChange();
    }
}