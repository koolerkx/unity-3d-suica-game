using UnityEngine;

public class Throw : MonoBehaviour
{
    private Rigidbody rb_;
    public float power = 2.5f;

    [SerializeField] private bool isThrown = false;
    public void SetIsThrown(bool isThrown) => this.isThrown = isThrown;

    private float startDepth;
    private float startScreenY;

    private float shakeIntensity = 0f;
    [SerializeField] private float maxShakeAmount = 0.1f;

    public void SetShakeIntensity(float percentage)
    {
        shakeIntensity = percentage;
    }

    void Reset()
    {
        if (rb_ == null) rb_ = GetComponent<Rigidbody>();
        if (rb_ == null) return;

        rb_.isKinematic = true;
        if (!rb_.isKinematic)
        {
            rb_.linearVelocity = Vector3.zero;
            rb_.angularVelocity = Vector3.zero;
        }
    }

    void Awake()
    {
        rb_ = GetComponent<Rigidbody>();
        Reset();
    }

    void Start()
    {

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        startDepth = sp.z;
        startScreenY = sp.y;
    }

    public void StartThrow(float _power)
    {
        if (isThrown) return;
        if (rb_ == null) return;
        isThrown = true;
        shakeIntensity = 0f;
        rb_.isKinematic = false;

        Camera cam = Camera.main;
        Vector3 finalDirection = Vector3.forward;

        if (cam != null)
        {
            Vector3 camForward = cam.transform.forward;

            Vector3 forwardXZ = new Vector3(camForward.x, 0, camForward.z).normalized;

            float upWeight = Mathf.Lerp(0.15f, 0.45f, (_power / GameManager.Instance.powerUpperBound));

            finalDirection = (forwardXZ + Vector3.up * upWeight).normalized;
        }

        rb_.AddForce(finalDirection * _power, ForceMode.Impulse);
    }

    void Update()
    {
        if (isThrown) return;

        SetObjectTransformWithMouse();
    }

    void SetObjectTransformWithMouse()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Keep: depth + screen Y fixed; only screen X follows mouse
        Vector3 targetScreen = new Vector3(Input.mousePosition.x, startScreenY, startDepth);
        Vector3 basePosition = cam.ScreenToWorldPoint(targetScreen);

        Vector3 shakeOffset = Vector3.zero;
        if (shakeIntensity > 0)
        {
            float currentAmount = shakeIntensity * maxShakeAmount;
            shakeOffset = Random.insideUnitSphere * currentAmount;
        }

        transform.position = basePosition + shakeOffset;
    }
}
