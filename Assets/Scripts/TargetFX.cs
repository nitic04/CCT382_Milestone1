using UnityEngine;

public class TargetFX : MonoBehaviour
{
    public GameObject burstPrefab;
    public Animator animator;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void OnKilled(bool stealth)
    {
        if (burstPrefab) Instantiate(burstPrefab, transform.position + Vector3.up, Quaternion.identity);
        animator.SetTrigger(stealth ? "DieStealth" : Random.value < 0.5f ? "DieForward" : "DieRight");
    }
}