using UnityEngine;

public class FlashHide : MonoBehaviour
{
    void OnEnable()
    {
        Invoke(nameof(Hide), 0.2f);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}