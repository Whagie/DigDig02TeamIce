using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Timeline.TimelinePlaybackControls;

public class Player : Entity
{
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
        camera1 = Camera.main.transform;
    }

    protected override void OnUpdate()
    {
        //base.OnUpdate();

        //BeforeMovement();
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
}
