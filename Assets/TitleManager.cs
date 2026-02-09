using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [Header("Skybox Camera Parallax")]
    [SerializeField] private Camera skyboxCamera;
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
    [SerializeField] private float initialDownForce = 0.5f;
    [SerializeField] private float lateralForce = 0.3f;
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
    private Vector3 baseSkyboxEuler;
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
            Vector3 force = Vector3.down * initialDownForce;
            force.x += Random.Range(-lateralForce, lateralForce);
            force.z += Random.Range(-lateralForce, lateralForce);
            rb.AddForce(force, ForceMode.Impulse);

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
        if (skyboxCamera == null) return;
        baseSkyboxEuler = skyboxCamera.transform.localEulerAngles;
    }

    private void UpdateSkyboxCameraParallax()
    {
        if (skyboxCamera == null) return;

        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector3 mousePos = Input.mousePosition;
        float normalizedX = (mousePos.x / Screen.width) * 2f - 1f;
        float normalizedY = (mousePos.y / Screen.height) * 2f - 1f;
        float targetYaw = -normalizedX * maxYawOffset;
        float targetPitch = normalizedY * maxPitchOffset;

        currentYawOffset = Mathf.SmoothDampAngle(currentYawOffset, targetYaw, ref yawVelocity, followSmoothTime);
        currentPitchOffset = Mathf.SmoothDampAngle(currentPitchOffset, targetPitch, ref pitchVelocity, followSmoothTime);

        skyboxCamera.transform.localRotation = Quaternion.Euler(
            baseSkyboxEuler.x + currentPitchOffset,
            baseSkyboxEuler.y + currentYawOffset,
            baseSkyboxEuler.z
        );
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
