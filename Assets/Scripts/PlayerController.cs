using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4.5f;
    public float acceleration = 25f;
    public float turnSpeed = 720f;

    [Header("Jump")]
    public float jumpHeight = 1.2f;
    public float gravity = -25f;
    public float fallMultiplier = 1.5f;

    [Header("Slide")]
    public float slideStartSpeed = 6f;
    public float slideEndSpeed = 2f;
    public float slideDuration = 0.9f;
    public float slideHeight = 0.8f;
    public float standHeight = 1.8f;

    [Header("Landing Shake")]
    public float shakeFallThreshold = 1.6f;

    [Header("Hard Landing")]
    public float hardLandThreshold = 2f;
    public float landRecoveryTime = 0.4f;
    [Range(0f, 1f)] public float landRecoverySpeed = 0.2f;

    [Header("Dust")]
    public ParticleSystem dust;
    public float runDustRate = 12f;
    public float softLandFall = 0.8f;
    public int softLandDustCount = 8;
    public int hardLandDustCount = 35;
    public float slideDustMax = 70f;
    public float slideDustMin = 15f;
    public float slideDustSize = 0.9f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip softLandClip;
    public AudioClip hardLandClip;
    public AudioClip slideClip;
    [Range(0f, 1f)] public float slideVolume = 0.5f;

    [Header("Footsteps")]
    public AudioClip footstepClip;
    public float walkStepDistance = 0.8f;
    public float runStepDistance = 1.6f;
    public float footstepMaxLength = 0.25f;
    [Range(0f, 1f)] public float footstepVolume = 0.6f;

    CharacterController controller;
    Animator animator;
    Transform cam;
    CinemachineImpulseSource impulse;
    AudioSource footstepSource;

    Vector2 moveInput;
    bool sprinting;
    Vector3 velocity;
    float verticalVelocity;

    bool sliding;
    float slideTimer;
    Vector3 slideDirection;
    float slideDustAccum;

    bool wasGrounded;
    float peakY;
    float recoverUntil;
    float stepAccum;

    bool Recovering => Time.time < recoverUntil;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        impulse = GetComponent<CinemachineImpulseSource>();
        cam = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;

        // separate source so each step can be cut short
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.clip = footstepClip;
        footstepSource.volume = footstepVolume;
    }

    public void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); }
    public void OnSprint(InputValue value) { sprinting = value.isPressed; }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed || sliding || !controller.isGrounded || Recovering) return;
        animator.SetTrigger("Jump");
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    public void OnSlide(InputValue value)
    {
        if (!value.isPressed || sliding || !controller.isGrounded || Recovering) return;
        if (velocity.magnitude < 1f) return;
        StartSlide();
    }

    void Update()
    {
        if (sliding) UpdateSlide();
        else UpdateWalking();

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * (verticalVelocity < 0f ? fallMultiplier : 1f) * Time.deltaTime;

        controller.Move((velocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        bool grounded = controller.isGrounded;
        animator.SetBool("Grounded", grounded);

        float y = transform.position.y;
        if (grounded && !wasGrounded) OnLand(peakY - y);
        peakY = grounded ? y : Mathf.Max(peakY, y);
        wasGrounded = grounded;

        float speed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
        HandleFootsteps(grounded, speed);

        var emission = dust.emission;
        emission.rateOverTime = grounded && !sliding && speed > runSpeed * 0.7f ? runDustRate : 0f;

        if (!sliding && velocity.magnitude > 0.1f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(velocity), turnSpeed * Time.deltaTime);

        animator.SetFloat("Speed", velocity.magnitude / runSpeed, 0.1f, Time.deltaTime);
    }

    void UpdateWalking()
    {
        Vector3 forward = cam.forward; forward.y = 0; forward.Normalize();
        Vector3 right = cam.right; right.y = 0; right.Normalize();
        Vector3 direction = forward * moveInput.y + right * moveInput.x;

        float maxSpeed = sprinting ? runSpeed : walkSpeed;
        if (Recovering) maxSpeed *= landRecoverySpeed;

        Vector3 target = direction.normalized * (Mathf.Clamp01(moveInput.magnitude) * maxSpeed);
        velocity = Vector3.MoveTowards(velocity, target, acceleration * Time.deltaTime);
    }

    void UpdateSlide()
    {
        slideTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(slideTimer / slideDuration);
        EmitSlideDust(t);
        velocity = slideDirection * Mathf.Lerp(slideEndSpeed, slideStartSpeed, t);

        if (slideTimer <= 0f && !HeadBlocked()) EndSlide();
    }

    void StartSlide()
    {
        sliding = true;
        slideTimer = slideDuration;
        slideDirection = velocity.normalized;
        SetCapsule(slideHeight);
        animator.SetBool("Sliding", true);
        PlaySound(slideClip, slideVolume);
    }

    void EndSlide()
    {
        sliding = false;
        SetCapsule(standHeight);
        animator.SetBool("Sliding", false);
    }

    void SetCapsule(float height)
    {
        controller.height = height;
        controller.center = new Vector3(0f, height / 2f, 0f);
    }

    bool HeadBlocked()
    {
        Vector3 origin = transform.position + Vector3.up * (slideHeight - 0.1f);
        return Physics.Raycast(origin, Vector3.up, standHeight - slideHeight + 0.1f, ~0, QueryTriggerInteraction.Ignore);
    }

    void OnLand(float fall)
    {
        if (fall > shakeFallThreshold)
            impulse.GenerateImpulse(Vector3.down * Mathf.Clamp01((fall - shakeFallThreshold) / 2f + 0.2f));

        if (fall > hardLandThreshold)
        {
            recoverUntil = Time.time + landRecoveryTime;
            animator.SetTrigger("HardLand");
            PlaySound(hardLandClip);
            dust.Emit(hardLandDustCount);
        }
        else if (fall > softLandFall)
        {
            PlaySound(softLandClip, Mathf.Clamp(0.5f + fall * 0.2f, 0.5f, 1f));
            dust.Emit(softLandDustCount);
        }
    }

    // more and bigger puffs while the slide is fast
    void EmitSlideDust(float speed01)
    {
        slideDustAccum += Mathf.Lerp(slideDustMin, slideDustMax, speed01) * Time.deltaTime;

        var p = new ParticleSystem.EmitParams();
        for (; slideDustAccum >= 1f; slideDustAccum -= 1f)
        {
            p.startSize = slideDustSize * Mathf.Lerp(1.3f, 0.6f, speed01) * Random.Range(0.7f, 1f);
            p.startLifetime = Random.Range(0.6f, 1f);
            p.position = transform.position + Vector3.up * 0.05f + Random.insideUnitSphere * 0.25f;
            p.velocity = -slideDirection * Mathf.Lerp(0.3f, 2.5f, speed01) + Random.insideUnitSphere * 0.3f + Vector3.up * 0.15f;
            dust.Emit(p, 1);
        }
    }

    void PlaySound(AudioClip clip, float volume = 1f)
    {
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clip, volume);
    }

    void HandleFootsteps(bool grounded, float speed)
    {
        if (!grounded || sliding || speed < 0.5f)
        {
            stepAccum = walkStepDistance * 0.5f;
            return;
        }

        float stride = sprinting ? runStepDistance : walkStepDistance;
        stepAccum += speed * Time.deltaTime;
        if (stepAccum < stride) return;
        stepAccum -= stride;

        footstepSource.pitch = Random.Range(0.92f, 1.08f);
        footstepSource.PlayScheduled(AudioSettings.dspTime);
        footstepSource.SetScheduledEndTime(AudioSettings.dspTime + footstepMaxLength);
    }
}