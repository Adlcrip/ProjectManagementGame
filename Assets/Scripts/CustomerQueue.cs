using System.Collections;
using UnityEngine;

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

    private GameObject currentCustomer;   // At the counter (or walking to it)
    private GameObject waitingCustomer;   // Standing at the spawn point

    private int spawnedCount = 0;
    private bool counterOccupied = false;
    private const string ChoosingParam = "isChoosing";
    private bool atCounter = false;
    private bool isSpawning = false;      // True while waiting for the spawn delay

    void Start()
    {
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

        // Customer reached the counter -> switch to the choosing animation
        SetChoosing(customer, true);

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

    // Call this when the order is done
    public void FinishCurrentCustomer()
    {
        if (!atCounter || currentCustomer == null) return;

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
        if (exitPoint != null)
            yield return MoveTo(customer, exitPoint.position);

        Destroy(customer);
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