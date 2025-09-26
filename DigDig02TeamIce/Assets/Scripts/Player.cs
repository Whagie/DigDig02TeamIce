using System;
using UnityEngine;

public class Player : Entity
{
    CharacterController controller;
    public Transform camera1;

    float moveX;
    float moveY;
    public float Speed;
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float turnSpeed = 8f;
    public float jumpHeight = 1f;

    float verticalVelocity;
    public float gravity = 9.82f;

    private Vector3 moveDir;

    public bool Sprinting => Input.GetKey(KeyCode.LeftShift);

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
        base.OnUpdate();

        InputManagement();
        BeforeMovement();
        Move();
        Turn();
    }

    void InputManagement()
    {
        moveX = Input.GetAxis("Horizontal");
        moveY = Input.GetAxis("Vertical");
    }

    void BeforeMovement()
    {
        if (Sprinting)
        {
            Speed = sprintSpeed;
        }
        else
        {
            Speed = walkSpeed;
        }
    }

    void Move()
    {
        moveDir = camera1.forward * moveY + camera1.right * moveX;

        moveDir.y = VerticalForce();
        moveDir *= Speed;
        controller.Move(moveDir * Time.deltaTime);
    }

    void Turn()
    {
        if (Mathf.Abs(moveX) > 0 || Mathf.Abs(moveY) > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0, moveDir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    float VerticalForce()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -1f;
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * gravity * 2);
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        return verticalVelocity;
    }
}
