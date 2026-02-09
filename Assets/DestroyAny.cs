using UnityEngine;

public class DestroyAny : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == gameObject) return;
        Destroy(other.gameObject);
    }
}
