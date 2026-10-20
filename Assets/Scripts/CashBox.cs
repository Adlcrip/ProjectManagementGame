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
        public string name;          // Just a label for you, e.g. "Bill 100" or "Coin 10"
        public int value;            // 100, 50, 20, 10, 5, 1
        public GameObject prefab;    // What shows up on the change tray
    }

    [Header("References")]
    public CustomerQueue customerQueue;   // Drag the object with CustomerQueue here
    public GameObject cashBoxPanel;       // The CashBox object that pops up (hidden until the cashier is clicked)

    [Header("Cash (same order as your buttons)")]
    public CashItem[] cashItems;          // 0 = Bill 100, 1 = Bill 50, 2 = Bill 20, 3 = Bill 10, 4 = Coin 10, 5 = Coin 5, 6 = Coin 1

    [Header("Change Tray")]
    public Transform changeTrayPoint;     // Top-left of the right tray, items fill in from here
    public Vector2 spacing = new Vector2(0.3f, 0.3f);   // Gap between items
    public int columns = 4;               // Items per row

    [Header("UI")]
    public TMP_Text totalText;            // Optional: shows how much change is on the tray
    public TMP_Text changeDueText;        // Optional: shows how much change the player must give, e.g. "Change: P250"
    public string currencySymbol = "P";

    [Header("Events (optional, for sounds, score, etc.)")]
    public UnityEvent onCorrectChange;
    public UnityEvent onWrongChange;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private int total = 0;

    void Start()
    {
        // Start hidden (skipped if this script is on the panel itself, so keep the panel disabled in the editor then)
        if (cashBoxPanel != null && cashBoxPanel != gameObject)
            cashBoxPanel.SetActive(false);

        // Lets clicks reach the money sprites on the change tray
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<Physics2DRaycaster>() == null)
            cam.gameObject.AddComponent<Physics2DRaycaster>();

        // If the customer's patience runs out, close the panel and clear the tray
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

    // Hook the Cashier button's OnClick to this
    public void OpenCashBox()
    {
        // Only works if the customer's money is showing in the bubble
        if (customerQueue == null || !customerQueue.HasPaid) return;

        // Receipt is showing (exact payment, or correct change already given): customer leaves
        if (customerQueue.ReadyToLeave)
        {
            customerQueue.FinishOrder();
            return;
        }

        UpdateChangeDueText();

        if (cashBoxPanel != null)
            cashBoxPanel.SetActive(true);
    }

    // Optional: hook a close button to this
    public void CloseCashBox()
    {
        if (cashBoxPanel != null)
            cashBoxPanel.SetActive(false);
    }

    // Hook each cash button's OnClick to this and pass its index from the Cash Items list
    public void AddCash(int itemIndex)
    {
        // Only works once the customer has paid, and before the change is given
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

        // Later items draw on top of earlier ones
        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sortingOrder += n;

        // Make the money clickable: it needs a collider (sized to the sprite) and a click handler
        GameObject colliderTarget = (sr != null) ? sr.gameObject : obj;
        if (colliderTarget.GetComponent<Collider2D>() == null)
            colliderTarget.AddComponent<BoxCollider2D>();

        TrayMoneyClick click = obj.AddComponent<TrayMoneyClick>();
        click.cashBox = this;

        spawned.Add(obj);
        total += item.value;
        UpdateTotalText();
    }

    // Hook a "Give Change" button to this
    public void ConfirmChange()
    {
        // Needs a customer who has paid, and change that hasn't been given yet
        if (customerQueue == null || customerQueue.CurrentPayment <= 0) return;
        if (customerQueue.ReadyToLeave) return;

        int due = customerQueue.ChangeDue;
        int given = total;

        // Any amount is accepted. Right or wrong, it gets recorded for the report.
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
        customerQueue.CompleteChange(given);   // Cashier shows the receipt; click the cashier to send the customer away
    }

    // Hook a "Clear" button to this if you want one
    public void ClearTray()
    {
        foreach (GameObject obj in spawned)
            if (obj != null) Destroy(obj);

        spawned.Clear();
        total = 0;
        UpdateTotalText();
    }

    // Shows "Change: P250" using the current customer's change due
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