using System;
using UnityEngine;

public class Seed : MonoBehaviour
{
    private Rigidbody _rb;
    public bool isMergeFlag = false;
    public int seedNo;
    public float spawnDelay = 0.2f;
    private bool isFirstTouch = true;
    public int score = 10;
    private bool isScored = false;

    public void SetIsScored() => isScored = true;

    void Awake()
    {
        this._rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        this.isMergeFlag = false;
    }

    void Update()
    {
    }

    public void SetAllowMerge()
    {
        this.isMergeFlag = false;
        this._rb.isKinematic = false;
    }

    void SetMergeState()
    {
        this.isMergeFlag = true;
        this._rb.isKinematic = true;
    }

    public void SetIsFirstTouch(bool isFirstTouch = true)
    {
        this.isFirstTouch = isFirstTouch;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isFirstTouch)
        {
            GameManager.Instance.ProceedNext();
            isFirstTouch = false;
        }

        GameObject colobj = collision.gameObject;

        if (colobj.CompareTag("Seed"))
        {
            Seed colseed = colobj.GetComponent<Seed>();
            if (colseed == null) return;
            colseed.SetIsFirstTouch(false);

            if (seedNo == colseed.seedNo && !isMergeFlag && !colseed.isMergeFlag && seedNo < GameManager.Instance.MaxSeedNo - 1)
            {
                this.SetMergeState();
                colseed.SetMergeState();
                Timer.SetTimeout(() =>
                {
                    Vector3 spawnPos = Vector3.Lerp(transform.position, colseed.transform.position, 0.5f);

                    GameManager.Instance.MergeNext(spawnPos, seedNo, new GameObject[] { gameObject, colseed.gameObject });
                    // Destroy(gameObject);
                    // Destroy(colseed.gameObject);
                }, spawnDelay);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isBoxOrSeed = other.CompareTag("Scoreable") || other.CompareTag("Seed");
        if (isBoxOrSeed && !isScored)
        {
            GameManager.Instance.AddScore(score);
            isScored = true;
        }
    }
}
