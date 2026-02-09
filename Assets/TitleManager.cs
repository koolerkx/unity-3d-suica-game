using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private float spawnTimer = 0f;
    private Vector3 baseSkyboxEuler;
    private float currentYawOffset = 0f;
    private float currentPitchOffset = 0f;
    private float yawVelocity = 0f;
    private float pitchVelocity = 0f;

    void Start()
    {
        CacheSkyboxCameraBaseRotation();
        ResetSpawnTimer();
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
}
