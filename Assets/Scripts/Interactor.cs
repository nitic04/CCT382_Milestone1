using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour

{
    public float range = 3f;
    public float rayHeight = 1f;
    public LayerMask interactableLayer;

    Vector3 Origin => transform.position + Vector3.up * rayHeight;

    void Update()
    {
        if (!Keyboard.current.rKey.wasPressedThisFrame) return;

        if (Physics.Raycast(Origin, transform.forward, out RaycastHit hit, range, interactableLayer))
            Debug.Log("Object in range:" + hit.collider.name);
        else
            Debug.Log("Nothing in range");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Origin, Origin + transform.forward * range);
        Gizmos.DrawWireSphere(Origin + transform.forward * range, 0.15f);
    }
}