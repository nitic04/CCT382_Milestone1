using UnityEngine;
using UnityEngine.UI;

public class DiveHUD : MonoBehaviour
{
    public DiveEventsSO events;
    public Image cooldownBar;
    public Text comboText;

    float timeLeft, length, punch;

    void OnEnable()
    {
        events.CooldownStarted += OnCooldown;
        events.TargetKilled += OnKill;
        events.ChainEnded += OnChainEnd;
    }

    void OnDisable()
    {
        events.CooldownStarted -= OnCooldown;
        events.TargetKilled -= OnKill;
        events.ChainEnded -= OnChainEnd;
    }

    void Start()
    {
        cooldownBar.fillAmount = 1f;
        comboText.text = "";
    }

    void OnCooldown(float seconds)
    {
        length = timeLeft = seconds;
    }

    void OnKill(Vector3 pos, int combo)
    {
        comboText.text = "x" + combo;
        punch = 0.5f;
    }

    void OnChainEnd()
    {
        comboText.text = "";
    }

    void Update()
    {
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            cooldownBar.fillAmount = 1f - Mathf.Clamp01(timeLeft / length);
        }
        punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 2f);
        comboText.transform.localScale = Vector3.one * (1f + punch);
    }
}