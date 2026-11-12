using UnityEngine;

[CreateAssetMenu(menuName = "Lab5/Health")]
public class HealthSO : ScriptableObject
{
    public float currentHealth = 100f;

    public void Subtract(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log("Health: " + currentHealth);
    }
}