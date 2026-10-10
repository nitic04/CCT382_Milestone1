using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineFreeLook))]
public class CameraSprintFov : MonoBehaviour
{
    public CharacterController player;
    public float fovBoost = 12f;
    public float smoothing = 6f;

    CinemachineFreeLook cam;
    float baseFov;

    void Awake()
    {
        cam = GetComponent<CinemachineFreeLook>();
        baseFov = cam.m_Lens.FieldOfView;
    }

    void LateUpdate()
    {
        Vector3 v = player.velocity;
        v.y = 0f;
        float target = baseFov + (v.magnitude > 3f ? fovBoost : 0f); 
        cam.m_Lens.FieldOfView = Mathf.Lerp(cam.m_Lens.FieldOfView, target, smoothing * Time.deltaTime);
    }
}