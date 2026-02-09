using UnityEngine;

public class DestroyWhenExit : MonoBehaviour
{
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || other.gameObject == gameObject) return;
        if (triggerCollider != null && triggerCollider.bounds.Contains(other.bounds.center)) return;
        Destroy(other.gameObject);
    }
}
