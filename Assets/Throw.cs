using UnityEngine;

public class Throw : MonoBehaviour
{
    private Rigidbody rb_;
    public float power = 2.5f;

    private bool isThrown = false;

    private float startDepth;
    private float startScreenY;

    void Reset()
    {
        if (rb_ == null) rb_ = GetComponent<Rigidbody>();
        if (rb_ == null) return;

        rb_.linearVelocity = Vector3.zero;
        rb_.angularVelocity = Vector3.zero;
        rb_.isKinematic = true;
    }

    void Start()
    {
        rb_ = GetComponent<Rigidbody>();
        Reset();

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        startDepth = sp.z;
        startScreenY = sp.y;
    }

    public void StartThrow(float _power = 2.5f)
    {
        if (isThrown) return;
        isThrown = true;
        rb_.isKinematic = false;

        Camera cam = Camera.main;
        Vector3 direction = cam != null ? cam.transform.forward : Vector3.forward;
        rb_.AddForce((direction + Vector3.up * 0.2f).normalized * _power, ForceMode.Impulse);
        return;
    }

    void Update()
    {
        if (isThrown) return;

        if (rb_ != null && rb_.isKinematic && Input.GetMouseButtonDown(0))
        {
            StartThrow(power);
        }

        SetObjectTransformWithMouse();
    }

    void SetObjectTransformWithMouse()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Keep: depth + screen Y fixed; only screen X follows mouse
        Vector3 targetScreen = new Vector3(
            Input.mousePosition.x,
            startScreenY,
            startDepth
        );

        transform.position = cam.ScreenToWorldPoint(targetScreen);
    }
}
