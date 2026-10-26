using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CustomerQueue : MonoBehaviour
{
    [Header("Customer Prefabs")]
    public GameObject customerPrefab;   
    public GameObject customerPrefab2;   

    [Header("Positions")]
    public Transform spawnPoint;
    public Transform counterPosition;
    public Transform exitPoint;         

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Visits")]
    public int maxVisits = 5;

    [Header("Timing")]
    public float spawnDelay = 3f;        

    [Header("UI")]
    public GameObject speechBubble;      

    [Header("Tickets")]
    public GameObject[] ticketPrefabs;   
    public Transform ticketSpawnPoint;  
    public Transform trayPoint;          
    public int[] ticketPrices = { 250, 300 };   
    public TMP_Text priceText;           
    public string currencySymbol = "$";  

    [Header("Payment")]
    public GameObject bill100Prefab;    
    public GameObject bill50Prefab;    
    public GameObject bigMoneyPrefab;   
    public Vector3 billOffset = new Vector3(0.3f, 0f, 0f);  
    public int bigPayment = 500;
    [Range(0f, 1f)]
    public float exactPayChance = 0.5f; 
    public bool onlyAcceptCorrectTicket = false;  

    [Header("Patience Timer")]
    public float patienceTime = 5f;     
    public TMP_Text timerText;         
    public float warningTime = 5f;      
    public Color normalTimerColor = Color.green;
    public Color warningTimerColor = Color.red;

    [Header("Report Screen")]
    public GameObject reportScreen;     
    public Animator starAnimator;       
    public TMP_Text resultText;          
    public TMP_Text checklistText;      
    public int starsToPass = 1;          
    public string winMessage = "YOU WIN!";
    public string failMessage = "FAILED";
    private const string StarsParam = "stars";  

    public event System.Action OnCustomerTimedOut;

    [Header("Cashier")]
    public Animator cashierAnimator;     
    private const string HasTicketParam = "hasTicket";

    public int CurrentTicketIndex { get; private set; } = -1;

    public int CurrentTicketPrice { get; private set; } = 0;

    public int CurrentPayment { get; private set; } = 0;
    public int ChangeDue => CurrentPayment - CurrentTicketPrice;

    public bool HasPaid => CurrentPayment > 0;

    public bool ChangeGiven { get; private set; } = false;

    public bool ReadyToLeave => HasPaid && (ChangeDue == 0 || ChangeGiven);

    
    public int ChangeGivenAmount { get; private set; } = 0;
    public bool ChangeWasCorrect => ChangeGivenAmount == ChangeDue;

   
    [System.Serializable]
    public class OrderRecord
    {
        public int customerNumber;     
        public int ticketPrice;
        public int payment;
        public int changeDue;
        public int changeGiven;
        public bool ticketCorrect;     
        public bool changeCorrect;    
        public bool timedOut;         
    }

    public List<OrderRecord> orderRecords = new List<OrderRecord>();

    public int CorrectChangeCount { get; private set; } = 0;
    public int WrongChangeCount { get; private set; } = 0;
    public int CorrectTicketCount { get; private set; } = 0;
    public int WrongTicketCount { get; private set; } = 0;
    public int TimedOutCount { get; private set; } = 0;

    
    public bool ChoiceWasCorrect { get; private set; } = false;

    private GameObject currentCustomer;  
    private GameObject waitingCustomer;   
    private GameObject currentTicket;    
    private readonly List<GameObject> currentMoney = new List<GameObject>();  
    private GameObject currentTrayTicket; 
    private bool waitingForTicketChoice = false;

    private int spawnedCount = 0;
    private bool counterOccupied = false;
    private const string ChoosingParam = "isChoosing";
    private bool atCounter = false;
    private bool isSpawning = false;      
    private Coroutine patienceRoutine;   

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

       
        SetChoosing(customer, true);
        SetBubble(true);
        ShowRandomTicket();
        StartPatienceTimer(customer);

        
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

        
        CurrentTicketPrice = (ticketPrices != null && CurrentTicketIndex < ticketPrices.Length)
            ? ticketPrices[CurrentTicketIndex]
            : 0;
        SetPriceText(true);

        waitingForTicketChoice = true;

        Debug.Log("Ticket " + (CurrentTicketIndex + 1) + " popped up, price " + CurrentTicketPrice);
    }

   
    public void SubmitTicketChoice(int chosenIndex)
    {
        if (!atCounter || !waitingForTicketChoice) return;

        ChoiceWasCorrect = (chosenIndex == CurrentTicketIndex);
        Debug.Log("Player chose ticket " + (chosenIndex + 1) + (ChoiceWasCorrect ? " (correct)" : " (wrong)"));

        if (onlyAcceptCorrectTicket && !ChoiceWasCorrect) return;

        waitingForTicketChoice = false;

       
        SpawnTrayTicket(chosenIndex);

       
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

        
        bool payExact = Random.value < exactPayChance || bigPayment < CurrentTicketPrice;
        CurrentPayment = payExact ? CurrentTicketPrice : bigPayment;

       
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

       
        for (int i = 0; i < bills.Count; i++)
        {
            if (bills[i] == null)
            {
                Debug.LogWarning("A bill prefab is not assigned (100 / 50 / 500).");
                continue;
            }

            Vector3 pos = ticketSpawnPoint.position + billOffset * (i - (bills.Count - 1) / 2f);
            GameObject bill = Instantiate(bills[i], pos, Quaternion.identity);

           
            SpriteRenderer sr = bill.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.sortingOrder += i;

            currentMoney.Add(bill);
        }

        Debug.Log("Customer pays " + CurrentPayment + " for a " + CurrentTicketPrice + " ticket, change due " + ChangeDue);

       
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

       
        SetCashierTicket(false);
    }

   
    public void CompleteChange(int amountGiven)
    {
        if (!HasPaid) return;

        ChangeGivenAmount = amountGiven;
        ChangeGiven = true;
        SetCashierTicket(true);
    }

   
    public void FinishOrder()
    {
        if (!ReadyToLeave || !atCounter || currentCustomer == null) return;

        RecordOrder(false);

       
        FinishCurrentCustomer();
    }

    
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

       
        if (customer != currentCustomer || !atCounter) yield break;

        Debug.Log("Customer ran out of patience and left.");

        RecordOrder(true);
        OnCustomerTimedOut?.Invoke();   
        FinishCurrentCustomer();
    }

   
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
        
    }

    IEnumerator Leave(GameObject customer)
    {
        SetChoosing(customer, false);
        SetBubble(false);
        ClearOrder();

        if (exitPoint != null)
            yield return MoveTo(customer, exitPoint.position);

        Destroy(customer);

       
        if (orderRecords.Count >= maxVisits)
            ShowReport();
    }

   
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

   
    string BuildChecklist()
    {
        const string ok = "<color=#2E9E3E>[OK]</color>";
        const string bad = "<color=#C62828>[X]</color>";

       
        string line1 = (TimedOutCount == 0)
            ? ok + " Served every customer"
            : bad + " Customers who left: " + TimedOutCount;

        
        string line2;
        if (WrongChangeCount > 0)
            line2 = bad + " Wrong change: " + WrongChangeCount;
        else if (CorrectChangeCount == 0)
            line2 = bad + " No change given";
        else
            line2 = ok + " Exact change every time";

       
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