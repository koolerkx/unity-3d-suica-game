using UnityEngine;

public class BallTest : MonoBehaviour
{
    private Rigidbody _rb;
    public int power;
    Vector3 direction = new Vector3(0, 0.2f, -1.0f);

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        _rb.AddForce(direction * power);
    }
}
