using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [Header("Skybox Camera Parallax")]
    [SerializeField] private Camera[] parallaxCameras;
    [SerializeField] private float maxYawOffset = 15f;
    [SerializeField] private float maxPitchOffset = 15f;
    [SerializeField] private float followSmoothTime = 0.08f;

    [Header("Random Spawn")]
    [SerializeField] private GameObject[] spawnObjects;
    [SerializeField] private float xSpread = 1.0f;
    [SerializeField] private float spawnRate = 2.0f;
    [SerializeField] private float spawnRateNoise = 0.5f;
    [SerializeField] private Transform spawnPoint;

    [Header("Spawn Force")]
    [SerializeField] private Vector3 throwDirection = Vector3.down;
    [SerializeField] private float throwPowerMin = 0.4f;
    [SerializeField] private float throwPowerMax = 0.8f;
    [SerializeField] private float directionVarianceDegrees = 10f;
    [SerializeField] private float torqueStrength = 0.3f;

    [Header("UI Audio")]
    [SerializeField] private AudioSource effectAudioSource;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip clickClip;

    [Header("Intro Fade")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private float introFadeDuration = 0.5f;

    [Header("Start Transition")]
    [SerializeField] private float bgmFadeOutDuration = 0.5f;

    private float spawnTimer = 0f;
    private Vector3[] baseCameraEulers;
    private float currentYawOffset = 0f;
    private float currentPitchOffset = 0f;
    private float yawVelocity = 0f;
    private float pitchVelocity = 0f;
    private Coroutine overlayFadeCoroutine;
    private Coroutine bgmFadeCoroutine;
    private float bgmTargetVolume;
    private Coroutine startGameCoroutine;

    void Start()
    {
        CacheSkyboxCameraBaseRotation();
        ResetSpawnTimer();
        StartOverlayFadeOut();
        StartBgmFadeIn();
    }

    void Update()
    {
        UpdateSkyboxCameraParallax();
        HandleRandomSpawn();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void PlayStartGameTransition()
    {
        StartOverlayFadeInAndLoad();
        StartBgmFadeOut();
    }

    public void PlayClickSound()
    {
        if (effectAudioSource != null && clickClip != null)
        {
            effectAudioSource.PlayOneShot(clickClip);
        }
    }

    public void EndGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void HandleGameOver(int score, double totalSeconds)
    {
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene("Title");
    }

    private void HandleRandomSpawn()
    {
        if (spawnObjects == null || spawnObjects.Length == 0 || spawnPoint == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        ResetSpawnTimer();

        Vector3 randomOffset = new Vector3(Random.Range(-xSpread, xSpread), 0f, 0f);
        Vector3 spawnPosition = spawnPoint.position + randomOffset;
        int randomIndex = Random.Range(0, spawnObjects.Length);
        GameObject spawned = Instantiate(spawnObjects[randomIndex], spawnPosition, Quaternion.identity);

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 baseDirection = throwDirection.sqrMagnitude > 0.0001f ? throwDirection.normalized : Vector3.down;
            float minPower = Mathf.Min(throwPowerMin, throwPowerMax);
            float maxPower = Mathf.Max(throwPowerMin, throwPowerMax);
            float power = Random.Range(minPower, maxPower);

            Vector3 randomAxis = Random.onUnitSphere;
            float variance = Mathf.Max(0f, directionVarianceDegrees);
            Vector3 finalDirection = variance > 0f
                ? Quaternion.AngleAxis(Random.Range(-variance, variance), randomAxis) * baseDirection
                : baseDirection;

            rb.AddForce(finalDirection * power, ForceMode.Impulse);

            Vector3 torque = new Vector3(
                Random.Range(-torqueStrength, torqueStrength),
                Random.Range(-torqueStrength, torqueStrength),
                Random.Range(-torqueStrength, torqueStrength)
            );
            rb.AddTorque(torque, ForceMode.Impulse);
        }
    }

    private void ResetSpawnTimer()
    {
        float noise = Random.Range(-spawnRateNoise, spawnRateNoise);
        spawnTimer = Mathf.Max(0.01f, spawnRate + noise);
    }

    private void CacheSkyboxCameraBaseRotation()
    {
        if (parallaxCameras == null || parallaxCameras.Length == 0) return;

        baseCameraEulers = new Vector3[parallaxCameras.Length];
        for (int i = 0; i < parallaxCameras.Length; i++)
        {
            Camera cam = parallaxCameras[i];
            baseCameraEulers[i] = cam != null ? cam.transform.localEulerAngles : Vector3.zero;
        }
    }

    private void UpdateSkyboxCameraParallax()
    {
        if (parallaxCameras == null || parallaxCameras.Length == 0) return;

        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector3 mousePos = Input.mousePosition;
        float normalizedX = (mousePos.x / Screen.width) * 2f - 1f;
        float normalizedY = (mousePos.y / Screen.height) * 2f - 1f;
        float targetYaw = -normalizedX * maxYawOffset;
        float targetPitch = normalizedY * maxPitchOffset;

        currentYawOffset = Mathf.SmoothDampAngle(currentYawOffset, targetYaw, ref yawVelocity, followSmoothTime);
        currentPitchOffset = Mathf.SmoothDampAngle(currentPitchOffset, targetPitch, ref pitchVelocity, followSmoothTime);

        if (baseCameraEulers == null || baseCameraEulers.Length != parallaxCameras.Length)
        {
            CacheSkyboxCameraBaseRotation();
        }

        for (int i = 0; i < parallaxCameras.Length; i++)
        {
            Camera cam = parallaxCameras[i];
            if (cam == null) continue;

            Vector3 baseEuler = baseCameraEulers != null && i < baseCameraEulers.Length
                ? baseCameraEulers[i]
                : cam.transform.localEulerAngles;

            cam.transform.localRotation = Quaternion.Euler(
                baseEuler.x + currentPitchOffset,
                baseEuler.y + currentYawOffset,
                baseEuler.z
            );
        }
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

        overlayFadeCoroutine = StartCoroutine(FadeOverlayAlpha(1f, 0f, introFadeDuration, true));
    }

    private void StartOverlayFadeIn()
    {
        if (overlayImage == null) return;
        if (!overlayImage.IsActive())
        {
            overlayImage.gameObject.SetActive(true);
        }
        overlayImage.enabled = true;

        if (overlayFadeCoroutine != null)
        {
            StopCoroutine(overlayFadeCoroutine);
        }

        float startAlpha = overlayImage.color.a;
        overlayFadeCoroutine = StartCoroutine(FadeOverlayAlpha(startAlpha, 1f, bgmFadeOutDuration, false));
    }

    private void StartOverlayFadeInAndLoad()
    {
        if (startGameCoroutine != null)
        {
            StopCoroutine(startGameCoroutine);
        }

        startGameCoroutine = StartCoroutine(FadeOverlayInThenLoad());
    }

    private IEnumerator FadeOverlayInThenLoad()
    {
        if (overlayImage == null)
        {
            StartGame();
            yield break;
        }

        if (!overlayImage.gameObject.activeSelf)
        {
            overlayImage.gameObject.SetActive(true);
        }
        overlayImage.enabled = true;

        yield return StartCoroutine(FadeOverlayAlpha(overlayImage.color.a, 1f, bgmFadeOutDuration, false));
        StartGame();
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

        bgmFadeCoroutine = StartCoroutine(FadeBgmVolume(0f, bgmTargetVolume, introFadeDuration));
    }

    private void StartBgmFadeOut()
    {
        if (bgmAudioSource == null) return;

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        bgmFadeCoroutine = StartCoroutine(FadeBgmVolume(bgmAudioSource.volume, 0f, bgmFadeOutDuration));
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
