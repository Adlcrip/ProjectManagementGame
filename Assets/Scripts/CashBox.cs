using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CashBox : MonoBehaviour
{
    [System.Serializable]
    public class CashItem
    {
        public string name;         
        public int value;          
        public GameObject prefab;   
    }

    [Header("References")]
    public CustomerQueue customerQueue;  
    public GameObject cashBoxPanel;      

    [Header("Cash (same order as your buttons)")]
    public CashItem[] cashItems;         

    [Header("Change Tray")]
    public Transform changeTrayPoint;    
    public Vector2 spacing = new Vector2(0.3f, 0.3f);  
    public int columns = 4;               

    [Header("UI")]
    public TMP_Text totalText;            
    public TMP_Text changeDueText;       

    [Header("Events (optional, for sounds, score, etc.)")]
    public UnityEvent onCorrectChange;
    public UnityEvent onWrongChange;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private int total = 0;

    void Start()
    {
       
        if (cashBoxPanel != null && cashBoxPanel != gameObject)
            cashBoxPanel.SetActive(false);

       
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<Physics2DRaycaster>() == null)
            cam.gameObject.AddComponent<Physics2DRaycaster>();

       
        if (customerQueue != null)
            customerQueue.OnCustomerTimedOut += HandleCustomerTimedOut;

        UpdateTotalText();
    }

    void OnDestroy()
    {
        if (customerQueue != null)
            customerQueue.OnCustomerTimedOut -= HandleCustomerTimedOut;
    }

    void HandleCustomerTimedOut()
    {
        ClearTray();
        CloseCashBox();
    }

    
    public void OpenCashBox()
    {
        

      
        if (customerQueue == null || !customerQueue.HasPaid) return;

        
        if (customerQueue.ReadyToLeave)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.cashier);

            customerQueue.FinishOrder();
            return;
        }

        UpdateChangeDueText();

        if (cashBoxPanel != null)
            cashBoxPanel.SetActive(true);
    }

   
    public void CloseCashBox()
    {
       
        if (cashBoxPanel != null)
            cashBoxPanel.SetActive(false);
    }

   
    public void AddCash(int itemIndex)
    {
       
        if (customerQueue == null || customerQueue.CurrentPayment <= 0) return;
        if (customerQueue.ReadyToLeave) return;

        if (cashItems == null || itemIndex < 0 || itemIndex >= cashItems.Length)
        {
            Debug.LogWarning("No cash item at index " + itemIndex);
            return;
        }

        CashItem item = cashItems[itemIndex];

        if (item.prefab == null || changeTrayPoint == null)
        {
            Debug.LogWarning("Prefab or change tray point not assigned for " + item.name);
            return;
        }

        int n = spawned.Count;
        int col = n % Mathf.Max(1, columns);
        int row = n / Mathf.Max(1, columns);

        GameObject obj = Instantiate(item.prefab, changeTrayPoint);
        obj.transform.localPosition = new Vector3(col * spacing.x, -row * spacing.y, 0f);
        obj.transform.localRotation = Quaternion.identity;

      
        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sortingOrder += n;

      
        GameObject colliderTarget = (sr != null) ? sr.gameObject : obj;
        if (colliderTarget.GetComponent<Collider2D>() == null)
            colliderTarget.AddComponent<BoxCollider2D>();

        TrayMoneyClick click = obj.AddComponent<TrayMoneyClick>();
        click.cashBox = this;

        spawned.Add(obj);
        total += item.value;
        UpdateTotalText();
    }


    public void ConfirmChange()
    {
        if (customerQueue == null || customerQueue.CurrentPayment <= 0) return;
        if (customerQueue.ReadyToLeave) return;

        int due = customerQueue.ChangeDue;
        int given = total;

       
        if (given == due)
        {
            Debug.Log("Correct change: " + given);
            onCorrectChange.Invoke();
        }
        else
        {
            Debug.Log("Wrong change. Gave " + given + ", needed " + due);
            onWrongChange.Invoke();
        }

        ClearTray();
        CloseCashBox();
        customerQueue.CompleteChange(given);  
    }

   
    public void ClearTray()
    {
        foreach (GameObject obj in spawned)
            if (obj != null) Destroy(obj);

        spawned.Clear();
        total = 0;
        UpdateTotalText();
    }


    void UpdateChangeDueText()
    {
        if (changeDueText == null || customerQueue == null) return;

        changeDueText.text = "Change: " + currencySymbol + customerQueue.ChangeDue;
    }

    void UpdateTotalText()
    {
        if (totalText != null)
            totalText.text = currencySymbol + total;
    }
}