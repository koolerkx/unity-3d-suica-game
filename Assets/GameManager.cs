using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int MaxSeedNo => seedPrefab.Length;
    public float PowerUpperBound => powerUpperBound;

    [Header("Game State")]
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private int maxLife = 5;

    [Header("Seed Spawning")]
    [SerializeField] private Seed[] seedPrefab;
    [SerializeField] private Transform seedPosition;

    [Header("UI")]
    [SerializeField] private TMP_Text textScore;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private Image powerImage;

    [Header("End Game Panel")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text panelTimeText;
    [SerializeField] private TMP_Text panelScoreText;

    [Header("Power Indicator")]
    [SerializeField] private float powerImageMaxWidth = 200f;
    [SerializeField] private Color powerLowColor = Color.blue;
    [SerializeField] private Color powerHighColor = Color.red;

    [Header("Merge VFX")]
    [SerializeField] private GameObject vfxPrefab;

    [Header("Throw Settings")]
    [SerializeField] private float chargeSpeed = 5.0f;
    [SerializeField] private float powerLowerBound = 1.0f;
    [SerializeField] private float powerUpperBound = 5.0f;

    private bool isNext;
    private int totalScore;
    private int currentLife = 5;
    private double accumulatedTime = 0;
    private float currentPower = 1.0f;
    private bool isCharging = false;
    private Throw currentThrowInstance;
    private float chargeTimer = 0f;
    private bool isHolding = false;
    private bool gameOver = false;

    public void AddScore(int score) => SetScoreText(totalScore += score);

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
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }
        CreateSeed();
        SetLifeText(currentLife);
        SetTimerText(accumulatedTime);
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
            UpdatePowerIndicator(0f);
        }

        if (isCharging && Input.GetMouseButton(0))
        {
            chargeTimer += Time.deltaTime;

            float rawPower = 1.0f + Mathf.PingPong(chargeTimer * chargeSpeed, 4.0f);

            float normalized = (rawPower - 1.0f) / 4.0f;
            float curvePower = Mathf.Pow(normalized, 2);

            currentPower = Mathf.Lerp(powerLowerBound, powerUpperBound, curvePower);

            currentThrowInstance.SetShakeIntensity(normalized);
            UpdatePowerIndicator(normalized);
        }

        if (isCharging && Input.GetMouseButtonUp(0))
        {
            Debug.Log($"Throw Power: {currentPower:F2}");
            isCharging = false;
            currentThrowInstance.StartThrow(currentPower);
            currentThrowInstance = null;
            isHolding = false;
            UpdatePowerIndicator(0f);
        }
    }

    public void Update()
    {
        if (gameOver) return;

        accumulatedTime += Time.deltaTime;
        SetTimerText(accumulatedTime);
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
        Debug.Log("CreateSeed");
        if (isHolding) return;
        if (seedPrefab == null || seedPrefab.Length < 2 || seedPosition == null) return;
        isHolding = true;
        int i = Random.Range(0, seedPrefab.Length - 2);
        Seed seedIns = Instantiate(seedPrefab[i], seedPosition.position, seedPosition.rotation);
        // Seed seedIns = Instantiate(seedPrefab[i], seedPosition);
        seedIns.seedNo = i;
        seedIns.SetActiveSeed(true);
        seedIns.gameObject.SetActive(true);
        currentThrowInstance = seedIns.GetComponent<Throw>();
    }

    public void ProceedNext()
    {
        Invoke(nameof(CreateSeed), spawnDelay);
    }

    public void MergeNext(Vector3 target, int seedNo, Vector3 avgLinear, Vector3 avgAngular, GameObject[] toDestory)
    {
        if (seedPrefab == null || seedNo + 1 >= seedPrefab.Length) return;
        PlayEffectAt(target, Quaternion.identity);
        Seed seedIns = Instantiate(seedPrefab[seedNo + 1], target, Quaternion.identity);
        seedIns.seedNo = seedNo + 1;
        seedIns.SetIsScored();
        seedIns.SetActiveSeed(false);
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

        if (seedRb != null)
        {
            seedRb.linearVelocity = avgLinear;
            seedRb.angularVelocity = avgAngular;
        }
        for (int i = 0; i < toDestory.Length; i++)
        {
            Destroy(toDestory[i]);
        }

        seedIns.gameObject.SetActive(true);

        totalScore += seedIns.score;
        SetScoreText(totalScore);
    }

    public void PlayEffectAt(Vector3 position, Quaternion rotation)
    {
        if (vfxPrefab == null) return;
        GameObject effect = Instantiate(vfxPrefab, position, rotation);

        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Destroy(effect, 2f);
            return;
        }

        float duration = ps.main.duration + ps.main.startLifetime.constant;
        Destroy(effect, duration);
    }

    private void SetScoreText(int score)
    {
        textScore.text = $"SCORE\n{score:D4}";
    }

    public void LoseLife()
    {
        if (currentLife <= 0) return;
        currentLife--;
        SetLifeText(currentLife);

        if (currentLife <= 0)
        {
            HandleGameOver();
        }
    }

    private void SetLifeText(int life)
    {
        if (lifeText == null) return;
        lifeText.text = $"LIFE\n{life:D1}/{maxLife:D1}";
    }

    private void SetTimerText(double totalSeconds)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt((float)(totalSeconds / 60.0));
        int seconds = Mathf.FloorToInt((float)(totalSeconds % 60.0));
        timerText.text = $"TIME\n{minutes:00}:{seconds:00}";
    }

    private void UpdatePowerIndicator(float normalized)
    {
        if (powerImage == null) return;
        float clamped = Mathf.Clamp01(normalized);
        RectTransform rect = powerImage.rectTransform;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, powerImageMaxWidth * clamped);
        powerImage.color = Color.Lerp(powerLowColor, powerHighColor, clamped);
    }

    private void HandleGameOver()
    {
        if (gameOver) return;
        gameOver = true;

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }

        if (panelTimeText != null)
        {
            int minutes = Mathf.FloorToInt((float)(accumulatedTime / 60.0));
            int seconds = Mathf.FloorToInt((float)(accumulatedTime % 60.0));
            panelTimeText.text = $"{minutes:00}:{seconds:00}";
        }

        if (panelScoreText != null)
        {
            panelScoreText.text = totalScore.ToString("D4");
        }

        if (textScore != null)
        {
            textScore.gameObject.SetActive(false);
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        if (lifeText != null)
        {
            lifeText.gameObject.SetActive(false);
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene("Title");
    }
}
