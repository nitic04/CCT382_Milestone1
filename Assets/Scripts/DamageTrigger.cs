using UnityEngine;
using UnityEngine.Events;

public class DamageTrigger : MonoBehaviour
{
    public HealthSO health;
    public float damage = 10f;
    public UnityEvent onHit;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        health.Subtract(damage);
        Debug.Log("DamageTrigger fired");
        onHit.Invoke();
    }
}