using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool isNext { get; set; }

    public float spawnDelay = 2f;

    [SerializeField] private Seed[] seedPrefab;
    [SerializeField] private Transform seedPosition;
    [SerializeField] private TMP_Text textScore;

    private int totalScore;

    public int MaxSeedNo => seedPrefab.Length;

    public void Start()
    {
        Instance = this;
        // isNext = true;
        // totalScore = 0;
        // SetScore(totalScore);

        CreateSeed();
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
        int i = Random.Range(0, seedPrefab.Length - 2);
        Seed seedIns = Instantiate(seedPrefab[i], seedPosition.position, seedPosition.rotation);
        // Seed seedIns = Instantiate(seedPrefab[i], seedPosition);
        seedIns.seedNo = i;
        seedIns.gameObject.SetActive(true);
    }

    public void ProceedNext()
    {
        Invoke("CreateSeed", spawnDelay);
    }

    public void MergeNext(Vector3 target, int seedNo)
    {
        Seed seedIns = Instantiate(seedPrefab[seedNo + 1], target, Quaternion.identity, seedPosition);
        seedIns.seedNo = seedNo + 1;
        seedIns.SetAllowMerge();
        seedIns.gameObject.SetActive(true);

        // totalScore += (int)Mathf.Pow(3, seedNo);
        // SetScore(totalScore);
    }

    private void SetScore(int score)
    {
        textScore.text = score.ToString();
    }
}