using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Milestone2/Dive Events", fileName = "DiveEvents")]
public class DiveEventsSO : ScriptableObject
{
    public event Action<Vector3> DiveStarted;
    public event Action<Vector3, int> TargetKilled;
    public event Action<float> CooldownStarted;
    public event Action ChainEnded;
    public event Action Landed;

    public void RaiseDiveStarted(Vector3 position) => DiveStarted?.Invoke(position);
    public void RaiseTargetKilled(Vector3 position, int combo) => TargetKilled?.Invoke(position, combo);
    public void RaiseCooldownStarted(float seconds) => CooldownStarted?.Invoke(seconds);
    public void RaiseChainEnded() => ChainEnded?.Invoke();
    public void RaiseLanded() => Landed?.Invoke();
}