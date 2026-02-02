using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool isNext { get; set; }

    public float spawnDelay = 1f;

    [SerializeField] private Seed[] seedPrefab;
    [SerializeField] private Transform seedPosition;
    [SerializeField] private TMP_Text textScore;

    private int totalScore;
    public void AddScore(int score) => SetScoreText(totalScore += score);

    private float currentPower = 1.0f;
    private bool isCharging = false;
    public float chargeSpeed = 5.0f;
    private Throw currentThrowInstance;
    private float chargeTimer = 0f;

    public float powerLowerBound = 1.0f;
    public float powerUpperBound = 5.0f;

    public int MaxSeedNo => seedPrefab.Length;

    private bool isHolding = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Start()
    {
        CreateSeed();
    }

    private void HandleInput()
    {
        if (currentThrowInstance == null) return;

        // Start Charging
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currentPower = 1.0f;
            chargeTimer = 0f;
        }

        if (isCharging && Input.GetMouseButton(0))
        {
            chargeTimer += Time.deltaTime;

            float rawPower = 1.0f + Mathf.PingPong(chargeTimer * chargeSpeed, 4.0f);

            float normalized = (rawPower - 1.0f) / 4.0f;
            float curvePower = Mathf.Pow(normalized, 2);

            currentPower = Mathf.Lerp(powerLowerBound, powerUpperBound, curvePower);

            currentThrowInstance.SetShakeIntensity(normalized);
        }

        if (isCharging && Input.GetMouseButtonUp(0))
        {
            Debug.Log($"Throw Power: {currentPower:F2}");
            isCharging = false;
            currentThrowInstance.StartThrow(currentPower);
            currentThrowInstance = null;
            isHolding = false;
        }
    }

    public void Update()
    {
        HandleInput();
    }

    public void FixedUpdate()
    {
        // if (isNext)
        // {
        //     isNext = false;
        //     Invoke("CreateSeed", spawnDelay);
        // }
    }

    private void CreateSeed()
    {
        if (isHolding) return;
        if (seedPrefab == null || seedPrefab.Length < 2 || seedPosition == null) return;
        isHolding = true;
        int i = Random.Range(0, seedPrefab.Length - 2);
        Seed seedIns = Instantiate(seedPrefab[i], seedPosition.position, seedPosition.rotation);
        // Seed seedIns = Instantiate(seedPrefab[i], seedPosition);
        seedIns.seedNo = i;
        seedIns.gameObject.SetActive(true);
        currentThrowInstance = seedIns.GetComponent<Throw>();
    }

    public void ProceedNext()
    {
        Invoke(nameof(CreateSeed), spawnDelay);
    }

    public void MergeNext(Vector3 target, int seedNo, GameObject[] toDestory)
    {
        if (seedPrefab == null || seedNo + 1 >= seedPrefab.Length) return;
        Seed seedIns = Instantiate(seedPrefab[seedNo + 1], target, Quaternion.identity);
        seedIns.seedNo = seedNo + 1;
        seedIns.SetIsScored();
        Throw throwComp = seedIns.GetComponent<Throw>();
        if (throwComp != null)
        {
            throwComp.SetIsThrown(true);
        }

        seedIns.SetAllowMerge();
        Rigidbody seedRb = seedIns.GetComponent<Rigidbody>();
        if (seedRb != null)
        {
            seedRb.isKinematic = false;
        }

        if (toDestory.Length > 1 && toDestory[1] != null)
        {
            Rigidbody otherRb = toDestory[1].GetComponent<Rigidbody>();
            if (otherRb != null && seedRb != null)
            {
                seedRb.linearVelocity = otherRb.linearVelocity;
                seedRb.angularVelocity = otherRb.angularVelocity;
            }
        }
        for (int i = 0; i < toDestory.Length; i++)
        {
            Destroy(toDestory[i]);
        }

        seedIns.gameObject.SetActive(true);

        totalScore += seedIns.score;
        SetScoreText(totalScore);
    }

    private void SetScoreText(int score)
    {
        textScore.text = $"SCORE\n{score:D4}";
    }
}
