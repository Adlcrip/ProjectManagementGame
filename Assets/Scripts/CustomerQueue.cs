using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CustomerQueue : MonoBehaviour
{
    [Header("Customer Prefabs")]
    public GameObject customerPrefab;    // Customer 1
    public GameObject customerPrefab2;   // Customer 2

    [Header("Positions")]
    public Transform spawnPoint;
    public Transform counterPosition;
    public Transform exitPoint;          // Optional: where finished customers walk to

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Visits")]
    public int maxVisits = 5;

    [Header("Timing")]
    public float spawnDelay = 3f;        // Delay before the next customer spawns

    [Header("UI")]
    public GameObject speechBubble;      // Drag your bubble object here

    [Header("Tickets")]
    public GameObject[] ticketPrefabs;   // Put your 2 ticket prefabs here
    public Transform ticketSpawnPoint;   // Where the customer's ticket (then the money) pops up
    public Transform trayPoint;          // Where the ticket the player clicked shows up (on the tray)
    public int[] ticketPrices = { 250, 300 };   // Same order as the ticket prefabs
    public TMP_Text priceText;           // Drag your price text here
    public string currencySymbol = "$";  // Change to whatever you want, or leave empty

    [Header("Payment")]
    public GameObject bill100Prefab;     // The 100 bill
    public GameObject bill50Prefab;      // The 50 bill (needed for the 250 ticket)
    public GameObject bigMoneyPrefab;    // Optional 500 bill. If empty, 500 is paid as five 100 bills
    public Vector3 billOffset = new Vector3(0.3f, 0f, 0f);   // Spacing between bills when several are shown
    public int bigPayment = 500;
    [Range(0f, 1f)]
    public float exactPayChance = 0.5f;  // 0.5 = half the customers pay exact, half pay with the big bill
    public bool onlyAcceptCorrectTicket = false;   // If true, a wrong click is ignored

    [Header("Patience Timer")]
    public float patienceTime = 5f;      // Seconds the customer waits at the counter before leaving
    public TMP_Text timerText;           // Optional: shows the seconds left
    public float warningTime = 5f;       // At or below this many seconds left, the timer turns red
    public Color normalTimerColor = Color.green;
    public Color warningTimerColor = Color.red;

    [Header("Report Screen")]
    public GameObject reportScreen;      // The ReporScreen object (hidden until the last customer leaves)
    public Animator starAnimator;        // The Animator that uses StarController (0Star, 1Star, 2Star, 3Star)
    public TMP_Text resultText;          // Optional: the WinOrFailed text
    public TMP_Text checklistText;       // Optional: a text that lists what the player did right and wrong
    public int starsToPass = 1;          // Stars needed to win
    public string winMessage = "YOU WIN!";
    public string failMessage = "FAILED";
    private const string StarsParam = "stars";   // Int parameter in StarController

    // CashBox listens to this so it can close the panel and clear the tray
    public event System.Action OnCustomerTimedOut;

    [Header("Cashier")]
    public Animator cashierAnimator;     // Drag the Cashier object (the one with the Animator) here
    private const string HasTicketParam = "hasTicket";

    // Which ticket the current customer asked for (0 or 1, -1 = none)
    public int CurrentTicketIndex { get; private set; } = -1;

    // Price of the current ticket (0 = none)
    public int CurrentTicketPrice { get; private set; } = 0;

    // How much the customer is paying, and the change the player owes back
    public int CurrentPayment { get; private set; } = 0;
    public int ChangeDue => CurrentPayment - CurrentTicketPrice;

    // True once the customer's money is showing in the bubble
    public bool HasPaid => CurrentPayment > 0;

    // True once the player gave the correct change from the cash box
    public bool ChangeGiven { get; private set; } = false;

    // True when the receipt is ready: exact payment, or correct change already given
    public bool ReadyToLeave => HasPaid && (ChangeDue == 0 || ChangeGiven);

    // How much change the player actually handed over, and whether it matched what was due
    public int ChangeGivenAmount { get; private set; } = 0;
    public bool ChangeWasCorrect => ChangeGivenAmount == ChangeDue;

    // ---------- Report data (use this later for your report screen) ----------
    [System.Serializable]
    public class OrderRecord
    {
        public int customerNumber;     // 1 = first customer served, 2 = second, ...
        public int ticketPrice;
        public int payment;
        public int changeDue;
        public int changeGiven;
        public bool ticketCorrect;     // Did the player pick the ticket the customer asked for?
        public bool changeCorrect;     // Was the change exactly what was due?
        public bool timedOut;          // True if the customer left because the timer ran out
    }

    public List<OrderRecord> orderRecords = new List<OrderRecord>();

    public int CorrectChangeCount { get; private set; } = 0;
    public int WrongChangeCount { get; private set; } = 0;
    public int CorrectTicketCount { get; private set; } = 0;
    public int WrongTicketCount { get; private set; } = 0;
    public int TimedOutCount { get; private set; } = 0;

    // Result of the player's last ticket click
    public bool ChoiceWasCorrect { get; private set; } = false;

    private GameObject currentCustomer;   // At the counter (or walking to it)
    private GameObject waitingCustomer;   // Standing at the spawn point
    private GameObject currentTicket;     // Ticket currently shown
    private readonly List<GameObject> currentMoney = new List<GameObject>();   // Bills currently shown
    private GameObject currentTrayTicket; // Ticket the player put on the tray
    private bool waitingForTicketChoice = false;

    private int spawnedCount = 0;
    private bool counterOccupied = false;
    private const string ChoosingParam = "isChoosing";
    private bool atCounter = false;
    private bool isSpawning = false;      // True while waiting for the spawn delay
    private Coroutine patienceRoutine;    // The running patience timer

    void Start()
    {
        SetBubble(false);
        SetPriceText(false);
        SetCashierTicket(false);
        SetTimerText(-1f);

        if (reportScreen != null)
            reportScreen.SetActive(false);

        SpawnCustomer();
    }

    void SpawnCustomer()
    {
        if (spawnedCount >= maxVisits || waitingCustomer != null) return;

        // Spawn order: 1, 2, 1, 2, 1
        GameObject prefab = (spawnedCount % 2 == 0) ? customerPrefab : customerPrefab2;

        spawnedCount++;
        Debug.Log($"Customer {(spawnedCount % 2 == 1 ? 1 : 2)} spawned ({spawnedCount}/{maxVisits})");

        waitingCustomer = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        TryMoveToCounter();
    }

    IEnumerator SpawnAfterDelay()
    {
        if (isSpawning) yield break;
        isSpawning = true;

        yield return new WaitForSeconds(spawnDelay);

        isSpawning = false;
        SpawnCustomer();
    }

    void TryMoveToCounter()
    {
        if (waitingCustomer == null) return;
        if (counterOccupied) return;

        counterOccupied = true;
        currentCustomer = waitingCustomer;
        waitingCustomer = null;

        StartCoroutine(WalkToCounter(currentCustomer));
    }

    IEnumerator WalkToCounter(GameObject customer)
    {
        atCounter = false;

        yield return MoveTo(customer, counterPosition.position);

        atCounter = true;

        // Customer reached the counter -> choosing animation + speech bubble
        SetChoosing(customer, true);
        SetBubble(true);
        ShowRandomTicket();
        StartPatienceTimer(customer);

        // Spawn the next one after the delay
        if (spawnedCount < maxVisits)
            StartCoroutine(SpawnAfterDelay());
    }

    void SetChoosing(GameObject customer, bool value)
    {
        if (customer == null) return;

        Animator anim = customer.GetComponentInChildren<Animator>();

        if (anim == null)
        {
            Debug.LogWarning("No Animator found on " + customer.name);
            return;
        }

        if (anim.runtimeAnimatorController == null)
        {
            Debug.LogWarning("Animator has no Controller assigned on " + customer.name);
            return;
        }

        anim.SetBool(ChoosingParam, value);
        Debug.Log(ChoosingParam + " set to " + value + " on " + customer.name);
    }

    void SetBubble(bool value)
    {
        if (speechBubble != null)
            speechBubble.SetActive(value);
    }

    // Switches the cashier between NoTicket (false) and WithTicket (true)
    void SetCashierTicket(bool value)
    {
        if (cashierAnimator == null) return;

        cashierAnimator.SetBool(HasTicketParam, value);
    }

    void ShowRandomTicket()
    {
        ClearOrder();

        if (ticketPrefabs == null || ticketPrefabs.Length == 0 || ticketSpawnPoint == null)
        {
            Debug.LogWarning("Ticket prefabs or ticket spawn point not assigned.");
            return;
        }

        CurrentTicketIndex = Random.Range(0, ticketPrefabs.Length);
        currentTicket = Instantiate(ticketPrefabs[CurrentTicketIndex], ticketSpawnPoint.position, Quaternion.identity);

        // Show the price of this ticket
        CurrentTicketPrice = (ticketPrices != null && CurrentTicketIndex < ticketPrices.Length)
            ? ticketPrices[CurrentTicketIndex]
            : 0;
        SetPriceText(true);

        waitingForTicketChoice = true;

        Debug.Log("Ticket " + (CurrentTicketIndex + 1) + " popped up, price " + CurrentTicketPrice);
    }

    // Called when the player clicks a ticket (0 = first ticket, 1 = second ticket)
    public void SubmitTicketChoice(int chosenIndex)
    {
        if (!atCounter || !waitingForTicketChoice) return;

        ChoiceWasCorrect = (chosenIndex == CurrentTicketIndex);
        Debug.Log("Player chose ticket " + (chosenIndex + 1) + (ChoiceWasCorrect ? " (correct)" : " (wrong)"));

        if (onlyAcceptCorrectTicket && !ChoiceWasCorrect) return;

        waitingForTicketChoice = false;

        // Put the ticket the player clicked on the tray
        SpawnTrayTicket(chosenIndex);

        // Remove the customer's ticket and use the same spot for the money
        if (currentTicket != null)
        {
            Destroy(currentTicket);
            currentTicket = null;
        }

        SpawnMoney();
    }

    void SpawnTrayTicket(int index)
    {
        if (trayPoint == null || ticketPrefabs == null || index < 0 || index >= ticketPrefabs.Length)
        {
            Debug.LogWarning("Tray point not assigned or invalid ticket index.");
            return;
        }

        // Parent to the tray point so its position, rotation, and scale control how the ticket looks
        currentTrayTicket = Instantiate(ticketPrefabs[index], trayPoint);
        currentTrayTicket.transform.localPosition = Vector3.zero;
        currentTrayTicket.transform.localRotation = Quaternion.identity;
    }

    void SpawnMoney()
    {
        if (ticketSpawnPoint == null || CurrentTicketIndex < 0)
        {
            Debug.LogWarning("Ticket spawn point not assigned or no active ticket.");
            return;
        }

        // Randomly pay exact, or pay with the big amount (only if it covers the price)
        bool payExact = Random.value < exactPayChance || bigPayment < CurrentTicketPrice;
        CurrentPayment = payExact ? CurrentTicketPrice : bigPayment;

        // Work out which bills to show
        List<GameObject> bills = new List<GameObject>();

        if (CurrentPayment == bigPayment && bigMoneyPrefab != null)
        {
            bills.Add(bigMoneyPrefab);
        }
        else
        {
            int hundreds = CurrentPayment / 100;
            int fifties = (CurrentPayment % 100) / 50;

            for (int i = 0; i < hundreds; i++) bills.Add(bill100Prefab);
            for (int i = 0; i < fifties; i++) bills.Add(bill50Prefab);
        }

        // Spawn them side by side, centered on the ticket spawn point
        for (int i = 0; i < bills.Count; i++)
        {
            if (bills[i] == null)
            {
                Debug.LogWarning("A bill prefab is not assigned (100 / 50 / 500).");
                continue;
            }

            Vector3 pos = ticketSpawnPoint.position + billOffset * (i - (bills.Count - 1) / 2f);
            GameObject bill = Instantiate(bills[i], pos, Quaternion.identity);

            // Later bills draw on top of earlier ones
            SpriteRenderer sr = bill.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.sortingOrder += i;

            currentMoney.Add(bill);
        }

        Debug.Log("Customer pays " + CurrentPayment + " for a " + CurrentTicketPrice + " ticket, change due " + ChangeDue);

        // Exact payment: no change needed, so the cashier shows the ticket (WithTicket animation)
        if (ChangeDue == 0)
            SetCashierTicket(true);
    }

    void SetPriceText(bool show)
    {
        if (priceText == null) return;

        if (show)
            priceText.text = currencySymbol + CurrentTicketPrice;

        priceText.gameObject.SetActive(show);
    }

    // Clears the ticket, money, and price text
    void ClearOrder()
    {
        if (currentTicket != null)
            Destroy(currentTicket);

        foreach (GameObject bill in currentMoney)
            if (bill != null) Destroy(bill);
        currentMoney.Clear();

        if (currentTrayTicket != null)
            Destroy(currentTrayTicket);

        currentTicket = null;
        currentTrayTicket = null;
        waitingForTicketChoice = false;
        CurrentTicketIndex = -1;
        CurrentTicketPrice = 0;
        CurrentPayment = 0;
        ChangeGiven = false;
        ChangeGivenAmount = 0;
        ChoiceWasCorrect = false;
        SetPriceText(false);

        // Cashier goes back to NoTicket
        SetCashierTicket(false);
    }

    // Called by CashBox when the player confirmed the correct change.
    // The cashier switches to WithTicket (receipt) and waits for a click.
    public void CompleteChange(int amountGiven)
    {
        if (!HasPaid) return;

        ChangeGivenAmount = amountGiven;
        ChangeGiven = true;
        SetCashierTicket(true);
    }

    // Called when the player clicks the cashier while the receipt is showing
    public void FinishOrder()
    {
        if (!ReadyToLeave || !atCounter || currentCustomer == null) return;

        RecordOrder(false);

        // Customer leaves, and ClearOrder() resets the cashier to NoTicket
        FinishCurrentCustomer();
    }

    // Saves how this order went, before the order is cleared
    void RecordOrder(bool timedOut)
    {
        OrderRecord record = new OrderRecord
        {
            customerNumber = orderRecords.Count + 1,
            ticketPrice = CurrentTicketPrice,
            payment = CurrentPayment,
            changeDue = ChangeDue,
            changeGiven = ChangeGivenAmount,
            ticketCorrect = ChoiceWasCorrect,
            changeCorrect = !timedOut && ChangeWasCorrect,
            timedOut = timedOut
        };

        orderRecords.Add(record);

        if (timedOut)
        {
            TimedOutCount++;
            Debug.Log($"Order {record.customerNumber}: TIMED OUT (customer left)");
            return;
        }

        if (record.ticketCorrect) CorrectTicketCount++; else WrongTicketCount++;
        if (record.changeCorrect) CorrectChangeCount++; else WrongChangeCount++;

        Debug.Log($"Order {record.customerNumber}: ticket {(record.ticketCorrect ? "correct" : "WRONG")}, " +
                  $"change due {record.changeDue}, given {record.changeGiven} ({(record.changeCorrect ? "correct" : "WRONG")})");
    }

    void StartPatienceTimer(GameObject customer)
    {
        StopPatienceTimer();
        patienceRoutine = StartCoroutine(PatienceTimer(customer));
    }

    void StopPatienceTimer()
    {
        if (patienceRoutine != null)
        {
            StopCoroutine(patienceRoutine);
            patienceRoutine = null;
        }

        SetTimerText(-1f);
    }

    IEnumerator PatienceTimer(GameObject customer)
    {
        float timeLeft = patienceTime;

        while (timeLeft > 0f)
        {
            SetTimerText(timeLeft);
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        patienceRoutine = null;
        SetTimerText(-1f);

        // Only act if this customer is still the one at the counter
        if (customer != currentCustomer || !atCounter) yield break;

        Debug.Log("Customer ran out of patience and left.");

        RecordOrder(true);
        OnCustomerTimedOut?.Invoke();   // CashBox closes the panel and clears the tray
        FinishCurrentCustomer();
    }

    // Pass a negative number to hide the text
    void SetTimerText(float secondsLeft)
    {
        if (timerText == null) return;

        if (secondsLeft < 0f)
        {
            timerText.gameObject.SetActive(false);
            return;
        }

        timerText.gameObject.SetActive(true);
        timerText.text = Mathf.CeilToInt(secondsLeft).ToString();
        timerText.color = (secondsLeft <= warningTime) ? warningTimerColor : normalTimerColor;
    }

    // Call this when the order is done
    public void FinishCurrentCustomer()
    {
        if (!atCounter || currentCustomer == null) return;

        StopPatienceTimer();
        StartCoroutine(Leave(currentCustomer));

        currentCustomer = null;
        atCounter = false;
        counterOccupied = false;

        if (waitingCustomer != null)
            TryMoveToCounter();
        else if (spawnedCount >= maxVisits)
            Debug.Log("All visits finished.");
        // Otherwise the next customer is still in the spawn delay and will walk up once spawned
    }

    IEnumerator Leave(GameObject customer)
    {
        SetChoosing(customer, false);
        SetBubble(false);
        ClearOrder();

        if (exitPoint != null)
            yield return MoveTo(customer, exitPoint.position);

        Destroy(customer);

        // Last customer has left: show the report
        if (orderRecords.Count >= maxVisits)
            ShowReport();
    }

    // 1 star: nobody left without being served (no timeouts)
    // 1 star: every change given was exact
    // 1 star: every ticket choice was correct
    int CalculateStars()
    {
        int stars = 0;

        if (TimedOutCount == 0) stars++;
        if (WrongChangeCount == 0 && CorrectChangeCount > 0) stars++;
        if (WrongTicketCount == 0 && CorrectTicketCount > 0) stars++;

        return stars;
    }

    void ShowReport()
    {
        int stars = CalculateStars();
        Debug.Log("Report: " + stars + " star(s). Timeouts " + TimedOutCount +
                  ", wrong change " + WrongChangeCount + ", wrong tickets " + WrongTicketCount);

        if (reportScreen != null)
            reportScreen.SetActive(true);

        if (starAnimator != null)
            starAnimator.SetInteger(StarsParam, stars);

        if (resultText != null)
            resultText.text = (stars >= starsToPass) ? winMessage : failMessage;

        if (checklistText != null)
            checklistText.text = BuildChecklist();
    }

    // One line per star condition: [OK] if passed, [X] with the count if not
    string BuildChecklist()
    {
        const string ok = "<color=#2E9E3E>[OK]</color>";
        const string bad = "<color=#C62828>[X]</color>";

        // Star 1: nobody left without being served
        string line1 = (TimedOutCount == 0)
            ? ok + " Served every customer"
            : bad + " Customers who left: " + TimedOutCount;

        // Star 2: every change was exact
        string line2;
        if (WrongChangeCount > 0)
            line2 = bad + " Wrong change: " + WrongChangeCount;
        else if (CorrectChangeCount == 0)
            line2 = bad + " No change given";
        else
            line2 = ok + " Exact change every time";

        // Star 3: every ticket was correct
        string line3;
        if (WrongTicketCount > 0)
            line3 = bad + " Wrong tickets: " + WrongTicketCount;
        else if (CorrectTicketCount == 0)
            line3 = bad + " No tickets sold";
        else
            line3 = ok + " Right ticket every time";

        return line1 + "\n" + line2 + "\n" + line3;
    }

    IEnumerator MoveTo(GameObject obj, Vector3 target)
    {
        while (obj != null && Vector3.Distance(obj.transform.position, target) > 0.02f)
        {
            obj.transform.position = Vector3.MoveTowards(
                obj.transform.position,
                target,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (obj != null)
            obj.transform.position = target;
    }
}