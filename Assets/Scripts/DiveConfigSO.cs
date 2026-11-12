using UnityEngine;

[CreateAssetMenu(menuName = "Milestone2/Dive Config", fileName = "DiveConfig")]
public class DiveConfigSO : ScriptableObject
{
    [Header("Targeting")]
    [Tooltip("Layer the dive can lock onto")]
    public LayerMask targetLayer;
    [Tooltip("How far away a target can be")]
    public float range = 25f;
    [Tooltip("Thickness of the aim ray, bigger is more forgiving")]
    public float aimRadius = 1f;

    [Header("Dive")]
    public float diveSpeed = 28f;
    [Tooltip("How close to the target counts as a hit")]
    public float impactDistance = 1.2f;
    public float stealthOffset = 0.8f;

    [Header("Chain")]
    [Tooltip("You must be this much higher than the target's base to dive. Otherwise you lunge.")]
    public float minHeight = 2f;
    public float gravity = -20f;
    [Tooltip("Seconds after a kill to dive or lunge onto the next target")]
    public float chainWindow = 1.5f;
    [Tooltip("Seconds before you can dive again after a chain ends")]
    public float cooldown = 3f;

    [Header("Feel")]
    public float shakeStrength = 1f;

    [Header("Audio Variation")]
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
    public Vector2 volumeRange = new Vector2(0.8f, 1f);

    public float stabYaw = 0f;
}