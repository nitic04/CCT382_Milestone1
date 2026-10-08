using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine; // if this shows an error, use: using Unity.Cinemachine;

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
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.1f;

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
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    public AudioClip jumpClip;
    public AudioClip softLandClip;
    public AudioClip hardLandClip;
    public AudioClip slideClip;
    public float walkStepDistance = 0.8f;
    public float runStepDistance = 1.6f;
    [Range(0f, 1f)] public float footstepVolume = 0.6f;
    public float footstepMaxLength = 0.3f;
    [Range(0f, 1f)] public float slideVolume = 0.5f;

    CharacterController controller;
    Animator animator;
    Transform cam;
    CinemachineImpulseSource impulse;

    Vector2 moveInput;
    bool sprinting;
    Vector3 velocity;
    float verticalVelocity;
    float coyoteTimer;
    float jumpBufferTimer;

    bool sliding;
    float slideTimer;
    Vector3 slideDirection;
    float slideDustAccum;

    bool wasGrounded;
    float peakY;
    float recoveryTimer;

    float stepAccum;
    int lastStepIndex = -1;
    float footstepStartTime;
    float footstepBaseVolume;
    const float FootstepFade = 0.08f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        impulse = GetComponent<CinemachineImpulseSource>();
        cam = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        sprinting = value.isPressed;
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed) jumpBufferTimer = jumpBuffer;
    }

    public void OnSlide(InputValue value)
    {
        if (!value.isPressed || sliding || !controller.isGrounded || recoveryTimer > 0f) return;
        if (velocity.magnitude < 1f) return;
        StartSlide();
    }

    void StartSlide()
    {
        sliding = true;
        slideTimer = slideDuration;
        slideDirection = velocity.normalized;
        slideDustAccum = 0f;
        controller.height = slideHeight;
        controller.center = new Vector3(0f, slideHeight / 2f, 0f);
        animator.SetBool("Sliding", true);
        if (footstepSource != null) footstepSource.Stop();
        PlaySound(slideClip, slideVolume, 0.03f);
    }

    void EndSlide()
    {
        sliding = false;
        controller.height = standHeight;
        controller.center = new Vector3(0f, standHeight / 2f, 0f);
        animator.SetBool("Sliding", false);
    }

    bool HeadBlocked()
    {
        Vector3 origin = transform.position + Vector3.up * (slideHeight - 0.1f);
        return Physics.Raycast(origin, Vector3.up, standHeight - slideHeight + 0.1f, ~0, QueryTriggerInteraction.Ignore);
    }

    void PlaySound(AudioClip clip, float volume = 1f, float pitchVariance = 0.05f)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        audioSource.PlayOneShot(clip, volume);
    }

    void PlayFootstep(AudioClip clip, float volume)
    {
        if (footstepSource == null || clip == null) return;
        footstepSource.Stop();
        footstepSource.clip = clip;
        footstepSource.pitch = 1f + Random.Range(-0.06f, 0.06f);
        footstepBaseVolume = volume;
        footstepSource.volume = volume;
        footstepStartTime = Time.time;
        footstepSource.Play();
    }

    void UpdateFootstepFade()
    {
        if (footstepSource == null || !footstepSource.isPlaying) return;
        float age = Time.time - footstepStartTime;
        float fadeStart = footstepMaxLength - FootstepFade;
        if (age >= footstepMaxLength) footstepSource.Stop();
        else if (age > fadeStart) footstepSource.volume = footstepBaseVolume * (1f - (age - fadeStart) / FootstepFade);
    }

    void HandleFootsteps(bool isGrounded, float planarSpeed)
    {
        if (!isGrounded || sliding || planarSpeed < 0.5f)
        {
            stepAccum = walkStepDistance * 0.5f;
            return;
        }
        if (footstepClips == null || footstepClips.Length == 0) return;

        float threshold = Mathf.Lerp(walkStepDistance, runStepDistance, Mathf.InverseLerp(walkSpeed, runSpeed, planarSpeed));
        stepAccum += planarSpeed * Time.deltaTime;
        if (stepAccum < threshold) return;
        stepAccum -= threshold;

        int index = Random.Range(0, footstepClips.Length);
        if (footstepClips.Length > 1 && index == lastStepIndex) index = (index + 1) % footstepClips.Length;
        lastStepIndex = index;
        PlayFootstep(footstepClips[index], footstepVolume * Random.Range(0.85f, 1f));
    }

    void EmitSlideDust(float speed01)
    {
        if (dust == null) return;
        slideDustAccum += Mathf.Lerp(slideDustMin, slideDustMax, speed01) * Time.deltaTime;
        int count = (int)slideDustAccum;
        slideDustAccum -= count;

        var p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            float size = slideDustSize * Mathf.Lerp(1.3f, 0.6f, speed01) * Random.Range(0.7f, 1f);
            p.startSize = size;
            p.startLifetime = Random.Range(0.6f, 1.0f);
            p.position = transform.position + Vector3.up * 0.05f + Random.insideUnitSphere * 0.25f;
            p.velocity = -slideDirection * Mathf.Lerp(0.3f, 2.5f, speed01) + Random.insideUnitSphere * 0.3f + Vector3.up * 0.15f;
            dust.Emit(p, 1);
        }
    }

    void Update()
    {
        bool grounded = controller.isGrounded;
        recoveryTimer -= Time.deltaTime;

        if (sliding)
        {
            slideTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(slideTimer / slideDuration);
            EmitSlideDust(t);
            float slideSpeed = Mathf.Lerp(slideEndSpeed, slideStartSpeed, t);
            velocity = slideDirection * slideSpeed;
            if (slideTimer <= 0f && !HeadBlocked()) EndSlide();
        }
        else
        {
            Vector3 forward = cam.forward; forward.y = 0; forward.Normalize();
            Vector3 right = cam.right; right.y = 0; right.Normalize();
            Vector3 direction = forward * moveInput.y + right * moveInput.x;

            float maxSpeed = sprinting ? runSpeed : walkSpeed;
            if (recoveryTimer > 0f) maxSpeed *= landRecoverySpeed;
            float magnitude = Mathf.Clamp01(moveInput.magnitude);
            Vector3 targetVelocity = direction.normalized * (magnitude * maxSpeed);
            velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);
        }

        if (grounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;

        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

        if (!sliding && recoveryTimer <= 0f && jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            animator.SetTrigger("Jump");
            PlaySound(jumpClip, 0.8f);
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        float g = gravity * (verticalVelocity < 0f ? fallMultiplier : 1f);
        verticalVelocity += g * Time.deltaTime;

        Vector3 move = velocity + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);

        bool nowGrounded = controller.isGrounded;
        animator.SetBool("Grounded", nowGrounded);

        float y = transform.position.y;
        if (!nowGrounded)
        {
            peakY = Mathf.Max(peakY, y);
        }
        else
        {
            if (!wasGrounded)
            {
                float fall = peakY - y;
                if (fall > shakeFallThreshold)
                    impulse.GenerateImpulse(Vector3.down * Mathf.Clamp01((fall - shakeFallThreshold) / 2f + 0.2f));
                if (fall > hardLandThreshold)
                {
                    recoveryTimer = landRecoveryTime;
                    animator.SetTrigger("HardLand");
                    PlaySound(hardLandClip, 1f, 0.03f);
                }
                else if (fall > softLandFall)
                {
                    PlaySound(softLandClip, Mathf.Clamp(0.5f + fall * 0.2f, 0.5f, 1f), 0.05f);
                }

                if (dust != null)
                {
                    int burst = fall > hardLandThreshold ? hardLandDustCount : (fall > softLandFall ? softLandDustCount : 0);
                    if (burst > 0) dust.Emit(burst);
                }
            }
            peakY = y;
        }
        wasGrounded = nowGrounded;

        float planarSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
        HandleFootsteps(nowGrounded, planarSpeed);
        UpdateFootstepFade();

        if (dust != null)
        {
            bool kickUpDust = nowGrounded && !sliding && planarSpeed > runSpeed * 0.7f;
            var emission = dust.emission;
            emission.rateOverTime = kickUpDust ? runDustRate : 0f;
        }

        if (!sliding && velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        animator.SetFloat("Speed", velocity.magnitude / runSpeed, 0.1f, Time.deltaTime);
    }
}