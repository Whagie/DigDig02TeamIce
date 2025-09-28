using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Timeline.TimelinePlaybackControls;

public class Player : Entity
{

    public LayerMask layermask;

    public Collider DetectionCollider;

    private ParryManager parryManager;

    CharacterController controller;
    private Transform camera1;

    public bool Sprinting = false;

    public float Speed;
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float turnSpeed = 8f;
    public float jumpHeight = 1f;

    private Vector2 verticalVelocity;
    public const float gravity = 9.82f;

    private Vector3 moveDir;
    private Vector3 moveInput;

    public int Health = 15;

    private Coroutine invisibilityRoutine;
    public float InvisibilityLength = 0.6f;
    public float InvisibilityTimer = 0f;
    public bool Invisible;

    public bool Parrying;

    private Material material;
    public GameObject body;

    protected override void OnEntityEnable()
    {
        Player existing = TrackerHost.Current.Get<Player>();
        if (existing != null && existing != this)
        {
            Debug.Log("Player already exists, cancelling spawn.");
            Destroy(gameObject);
            return;
        }

        base.OnEntityEnable();
    }
    protected override void OnStart()
    {
        controller = GetComponent<CharacterController>();
        material = body.GetComponent<MeshRenderer>().material;

        camera1 = Camera.main.transform;

        parryManager = TrackerHost.Current.Get<ParryManager>();
        if (parryManager == null)
            return;
        parryManager.OnParryStart += HandleParryStart;
        parryManager.OnParryEnd += HandleParryEnd;
        parryManager.OnParryCooldownEnd += HandleParryCooldownEnd;
        parryManager.OnParried += Parried;
    }

    protected override void OnUpdate()
    {
        MovementHandler();
        Turn();
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0)
        {
            Sprinting = true;
        }
        else
        {
            Sprinting = false;
        }
    }

    void MovementHandler()
    {
        if (Sprinting && controller.isGrounded)
        {
            Speed = sprintSpeed;
        }
        else
        {
            Speed = walkSpeed;
        }

        moveDir = camera1.forward * moveInput.y + camera1.right * moveInput.x;

        moveDir *= Speed;
        moveDir.y = verticalVelocity.y;

        controller.Move(moveDir * Time.deltaTime);

        if (controller.isGrounded)
        {
            verticalVelocity.y = -1f;
        }
        else
        {
            verticalVelocity.y -= gravity * Time.deltaTime;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Turn()
    {
        if (Mathf.Abs(moveInput.x) > 0 || Mathf.Abs(moveInput.y) > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0, moveDir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (controller.isGrounded && context.performed)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * gravity * 2);
        }
    }

    public virtual void TakeDamage(int amount)
    {
        if (Invisible || Parrying)
        {
            return;
        }

        Health -= amount;
        if (Health <= 0)
        {
            Die();
            return;
        }

        StartInvisible(InvisibilityLength, true);

        CameraActions.Main.Shake(0.15f, 0.3f, 0.2f);
    }

    public void StartInvisible(float length = 0.6f, bool changeColor = false)
    {
        InvisibilityTimer = length;

        if (invisibilityRoutine != null)
            StopCoroutine(invisibilityRoutine);

        invisibilityRoutine = StartCoroutine(InvisibilityRoutine(changeColor));
    }
    private IEnumerator InvisibilityRoutine(bool changeColor)
    {
        Invisible = true;

        if (changeColor)
        {
            material.SetColor("_BaseColor", new Color(0.5f, 0.5f, 1f, 0.25f));
        }

        while (InvisibilityTimer > 0f)
        {
            InvisibilityTimer -= Time.deltaTime;
            yield return null;
        }

        Invisible = false;
        if (changeColor)
        {
            material.SetColor("_BaseColor", Color.blue);
        }

        invisibilityRoutine = null;
    }

    protected virtual void Die()
    {
        material.SetColor("_BaseColor", Color.magenta);
    }

    private void Parried()
    {
        StartInvisible(parryManager.parryLength);

        ParticleSpawner.Spawn(Particles.P_spark, transform.position);
        CameraActions.Main.Punch(-2f, 0.15f);
    }
    private void HandleParryStart()
    {
        material.SetColor("_BaseColor", Color.red);
        //Debug.Log("Parry Start!");
    }
    private void HandleParryEnd()
    {
        material.SetColor("_BaseColor", Color.green);
        //Debug.Log("Parry End!");
    }
    private void HandleParryCooldownEnd()
    {
        material.SetColor("_BaseColor", Color.blue);
        //Debug.Log("Parry Cooldown End!");
    }
}
