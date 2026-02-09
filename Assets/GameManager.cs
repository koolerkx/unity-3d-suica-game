using System.Collections;
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
    [SerializeField] private Image overlayImage;
    [SerializeField] private TMP_Text pingpongAlphaText;

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

    [Header("Audio")]
    [SerializeField] private AudioSource effectAudioSource;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip mergeClip;
    [SerializeField] private AudioClip throwClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private float bgmFadeInDuration = 0.5f;
    [SerializeField] private float sceneFadeDuration = 0.5f;

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
    private Coroutine bgmFadeCoroutine;
    private float bgmTargetVolume;
    private Coroutine overlayFadeCoroutine;
    private Coroutine pingpongTextCoroutine;
    private Coroutine sceneTransitionCoroutine;

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
        StartBgmFadeIn();
        StartOverlayFadeOut();
        StartPingpongText();
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
            PlayThrowSound();
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
        PlayMergeSound();
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
        PlayGameOverSound();

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
        StartSceneTransition(SceneManager.GetActiveScene().name);
    }

    public void BackToTitle()
    {
        StartSceneTransition("Title");
    }

    public void RestartWithFade()
    {
        StartSceneTransition(SceneManager.GetActiveScene().name);
    }

    public void BackToTitleWithFade()
    {
        StartSceneTransition("Title");
    }

    public void PlayClickSound()
    {
        if (effectAudioSource != null && clickClip != null)
        {
            effectAudioSource.PlayOneShot(clickClip);
        }
    }

    private void PlayMergeSound()
    {
        if (effectAudioSource != null && mergeClip != null)
        {
            effectAudioSource.PlayOneShot(mergeClip);
        }
    }

    private void PlayThrowSound()
    {
        if (effectAudioSource != null && throwClip != null)
        {
            effectAudioSource.PlayOneShot(throwClip);
        }
    }

    private void PlayGameOverSound()
    {
        if (effectAudioSource != null && gameOverClip != null)
        {
            effectAudioSource.PlayOneShot(gameOverClip);
        }
    }

    private void StartBgmFadeIn()
    {
        if (bgmAudioSource == null) return;

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        bgmTargetVolume = bgmAudioSource.volume;
        bgmAudioSource.volume = 0f;
        if (!bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Play();
        }

        bgmFadeCoroutine = StartCoroutine(FadeBgmVolume(0f, bgmTargetVolume, bgmFadeInDuration));
    }

    private void StartOverlayFadeOut()
    {
        if (overlayImage == null) return;

        overlayImage.gameObject.SetActive(true);
        overlayImage.enabled = true;
        Color startColor = overlayImage.color;
        startColor.a = 1f;
        overlayImage.color = startColor;

        if (overlayFadeCoroutine != null)
        {
            StopCoroutine(overlayFadeCoroutine);
        }

        overlayFadeCoroutine = StartCoroutine(FadeOverlayAlpha(1f, 0f, bgmFadeInDuration, true));
    }

    private IEnumerator FadeOverlayAlpha(float fromAlpha, float toAlpha, float duration, bool disableAfter)
    {
        if (overlayImage == null) yield break;

        if (duration <= 0f)
        {
            Color instant = overlayImage.color;
            instant.a = toAlpha;
            overlayImage.color = instant;
            if (disableAfter)
            {
                overlayImage.gameObject.SetActive(false);
            }
            yield break;
        }

        float elapsed = 0f;
        Color color = overlayImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            overlayImage.color = color;
            yield return null;
        }

        if (disableAfter)
        {
            overlayImage.gameObject.SetActive(false);
        }
    }

    private void StartSceneTransition(string sceneName)
    {
        if (sceneTransitionCoroutine != null)
        {
            StopCoroutine(sceneTransitionCoroutine);
        }

        sceneTransitionCoroutine = StartCoroutine(SceneTransitionRoutine(sceneName));
    }

    private IEnumerator SceneTransitionRoutine(string sceneName)
    {
        if (overlayImage != null)
        {
            overlayImage.gameObject.SetActive(true);
            overlayImage.enabled = true;
        }

        float startAlpha = overlayImage != null ? overlayImage.color.a : 0f;
        Coroutine fadeOverlay = null;
        if (overlayImage != null)
        {
            fadeOverlay = StartCoroutine(FadeOverlayAlpha(startAlpha, 1f, sceneFadeDuration, false));
        }

        Coroutine fadeBgm = null;
        if (bgmAudioSource != null)
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }
            fadeBgm = StartCoroutine(FadeBgmVolume(bgmAudioSource.volume, 0f, sceneFadeDuration));
        }

        if (fadeOverlay != null)
        {
            yield return fadeOverlay;
        }
        if (fadeBgm != null)
        {
            yield return fadeBgm;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void StartPingpongText()
    {
        if (pingpongAlphaText == null) return;

        if (pingpongTextCoroutine != null)
        {
            StopCoroutine(pingpongTextCoroutine);
        }

        pingpongAlphaText.gameObject.SetActive(true);
        pingpongTextCoroutine = StartCoroutine(PingpongTextAlpha(bgmFadeInDuration));
    }

    private IEnumerator PingpongTextAlpha(float duration)
    {
        if (pingpongAlphaText == null) yield break;

        if (duration <= 0f)
        {
            pingpongAlphaText.gameObject.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        Color color = pingpongAlphaText.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(0f, 1f, t);
            pingpongAlphaText.color = color;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(1f, 0f, t);
            pingpongAlphaText.color = color;
            yield return null;
        }

        pingpongAlphaText.gameObject.SetActive(false);
    }

    private IEnumerator FadeBgmVolume(float fromVolume, float toVolume, float duration)
    {
        if (bgmAudioSource == null) yield break;

        if (duration <= 0f)
        {
            bgmAudioSource.volume = toVolume;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bgmAudioSource.volume = Mathf.Lerp(fromVolume, toVolume, t);
            yield return null;
        }
    }
}
