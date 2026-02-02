using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform origin;
    public float rotateSpeed = 90f;

    void Update()
    {
        if (origin == null) return;

        float dir = 0f;

        if (Input.GetKey(KeyCode.Q))
            dir = -1f;
        else if (Input.GetKey(KeyCode.E))
            dir = 1f;

        if (dir != 0f)
        {
            transform.RotateAround(
                origin.position,
                Vector3.up,
                dir * rotateSpeed * Time.deltaTime
            );
        }
    }
}
