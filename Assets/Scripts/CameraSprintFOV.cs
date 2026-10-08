using UnityEngine;
using Cinemachine; // same using line as your other camera script

[RequireComponent(typeof(CinemachineFreeLook))]
public class CameraSprintFov : MonoBehaviour
{
    public CharacterController player;
    public float fovBoost = 12f;
    public float smoothing = 6f;

    CinemachineFreeLook freeLook;
    float baseFov;

    void Awake()
    {
        freeLook = GetComponent<CinemachineFreeLook>();
        baseFov = freeLook.m_Lens.FieldOfView;
    }

    void LateUpdate()
    {
        Vector3 v = player.velocity;
        v.y = 0f;
        float t = Mathf.InverseLerp(2f, 4.5f, v.magnitude);
        float target = baseFov + fovBoost * t;
        freeLook.m_Lens.FieldOfView = Mathf.Lerp(freeLook.m_Lens.FieldOfView, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
    }
}