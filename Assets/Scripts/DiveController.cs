using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine; // use Unity.Cinemachine if this errors

[RequireComponent(typeof(CharacterController))]
public class DiveController : MonoBehaviour
{
    public DiveConfigSO config;
    public DiveEventsSO events;

    enum State { Idle, Diving, Chain }
    State state;

    CharacterController controller;
    PlayerController player;
    Animator animator;
    CinemachineImpulseSource impulse;
    Transform cam;

    Collider aimed, target;
    int combo;
    float readyAt, chainTimer, diveTimeout, fallSpeed;
    bool lunging, landPending;

    bool DivePressed =>
        (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        player = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        impulse = GetComponent<CinemachineImpulseSource>();
        cam = Camera.main.transform;
    }

    void Update()
    {
        aimed = FindTarget();

        if (state == State.Idle) UpdateIdle();
        else if (state == State.Diving) UpdateDive();
        else UpdateChain();

        if (landPending && state != State.Diving && controller.isGrounded)
        {
            landPending = false;
            events.RaiseLanded();
        }
    }

    void UpdateIdle()
    {
        if (!DivePressed || aimed == null || Time.time < readyAt) return;
        if (HeightAbove(aimed) < config.minHeight) return;

        player.enabled = false;
        combo = 0;
        StartDive();
    }

    float HeightAbove(Collider c) => transform.position.y - c.bounds.min.y;

    void StartDive()
    {
        target = aimed;
        lunging = HeightAbove(target) < config.minHeight;
        if (!lunging) landPending = true;

        state = State.Diving;
        diveTimeout = Time.time + config.range / config.diveSpeed + 1f;
        animator.SetBool("Grounded", lunging);
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Lunging", lunging);
        events.RaiseDiveStarted(transform.position);
    }

    void UpdateDive()
    {
        Vector3 center = transform.position + Vector3.up * controller.height / 2f;
        Vector3 toTarget = target.bounds.center - center;
        if (lunging) toTarget.y = 0f;

        if (toTarget.magnitude <= config.impactDistance) { Impact(); return; }
        if (Time.time > diveTimeout) { EndChain(); return; }

        Vector3 dir = toTarget.normalized;
        Vector3 move = dir * config.diveSpeed;
        if (lunging) move.y = config.gravity;
        controller.Move(move * Time.deltaTime);

        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);
    }

    void Impact()
    {
        combo++;
        Vector3 point = target.bounds.center;
        bool stealth = combo == 1;

        if (stealth)
        {
            Vector3 spot = transform.position + transform.forward * config.stealthOffset;
            spot.y = target.transform.position.y;
            target.transform.SetPositionAndRotation(spot, transform.rotation);
        }

        target.enabled = false;
        target.SendMessage("OnKilled", stealth, SendMessageOptions.DontRequireReceiver);
        animator.SetTrigger(stealth ? "Assassinate" : "Kill");
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Lunging", false);
        if (!stealth) transform.Rotate(0f, config.stabYaw, 0f);
        impulse.GenerateImpulse(Vector3.down * config.shakeStrength);
        events.RaiseTargetKilled(point, combo);

        state = State.Chain;
        fallSpeed = 0f;
        chainTimer = config.chainWindow;
    }

    void UpdateChain()
    {
        fallSpeed = controller.isGrounded ? -2f : fallSpeed + config.gravity * Time.deltaTime;
        controller.Move(Vector3.up * fallSpeed * Time.deltaTime);
        animator.SetBool("Grounded", controller.isGrounded);
        chainTimer -= Time.deltaTime;

        if (DivePressed && aimed != null) StartDive();
        else if (chainTimer <= 0f) EndChain();
    }

    void EndChain()
    {
        state = State.Idle;
        player.ResetMotion();
        player.enabled = true;
        readyAt = Time.time + config.cooldown;
        animator.SetBool("Lunging", false);
        events.RaiseChainEnded();
        events.RaiseCooldownStarted(config.cooldown);
    }

    Collider FindTarget()
    {
        bool hit = Physics.SphereCast(cam.position, config.aimRadius, cam.forward,
            out RaycastHit info, config.range, config.targetLayer);
        return hit ? info.collider : null;
    }
}