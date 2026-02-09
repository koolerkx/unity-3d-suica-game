using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
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

    void Start()
    {
        ResetSpawnTimer();
    }

    void Update()
    {
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
}
