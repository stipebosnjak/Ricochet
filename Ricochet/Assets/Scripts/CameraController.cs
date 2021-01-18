using System;
using Assets;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private Camera _camera;
    public float turnSpeed = 5;
    public float moveSpeed;
    public float jumpHeight = 2;
    public float jumpSpeed = 2;
    public float cameraHeight = 2;
    private float jumpHangTimer;
    public float maxJumpHangTime = 0.2f;
    public float MaxJumpHeight => cameraHeight + jumpHeight;
    public Vector2 MouseDelta { get; set; }
    public Vector2 MoveDelta { get; set; }
    public float Sprint { get; set; }

    private bool jumped;
    private bool inJump;
    public bool IsSprinting => Sprint > 0;
    public bool IsJumping => Jump > 0;

    public float Jump { get; set; }

    public float speed = 3;
    public float ground = 2;
    public float height;


    public Camera Camera
    {
        get
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();

            return _camera;
        }
    }


    private float yaw = 0.0f;
    private float pitch = 0.0f;

    private void Start()
    {
        jumpHangTimer = maxJumpHangTime;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();

        HandleRotation();
    }

    private void HandleMovement()
    {
        var playerPosition = transform.position;

        var speed = moveSpeed;
        if (Sprint > 0)
        {
            speed += 4;
        }

        var frame = GetGroundFrame();
        var offset = new Vector3(MoveDelta.x.Delta(speed), 0, MoveDelta.y.Delta(speed));

        // Multiplying by frame transforms the movement-space offset
        // into worldspace.
        var moveDirection = frame * offset;

        // handle gravity, no gravity while in jump
        if (playerPosition.y > cameraHeight + 0.05f && !inJump)
        {
            moveDirection.y -= 9.807f * Time.deltaTime;
        }
        else if (IsJumping && !inJump)
        {
            inJump = true;
        }

        // move player in the air and back with gravity
        if (inJump)
        {
//            moveDirection.y = Mathf.SmoothStep(moveDirection.y, MaxJumpHeight + 0.1f, Time.deltaTime * jumpSpeed);
            if (playerPosition.y > MaxJumpHeight)
            {
                if ((jumpHangTimer -= Time.deltaTime) < 0)
                {
                    inJump = false;
                    jumpHangTimer = maxJumpHangTime;
                }
            }
            else
            {
                moveDirection.y = Mathf.Lerp(moveDirection.y, MaxJumpHeight + 0.1f, Time.deltaTime * jumpSpeed);
            }
        }

        transform.Translate(moveDirection, Space.World);
    }


    private void HandleRotation()
    {
        yaw += MouseDelta.x / 2;
        pitch -= MouseDelta.y / 2;

        // Clamp pitch:
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        // Wrap yaw:
        while (yaw < 0f)
        {
            yaw += 360f;
        }

        while (yaw >= 360f)
        {
            yaw -= 360f;
        }

        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }

    Quaternion GetGroundFrame()
    {
        // You can replace this with a raycast if you have more complex levels,
        // to get the direction of the collision plane under the character.
        var groundNormal = Vector3.up;


        // Construct a rotation that points your nose away from the ground,
        // and your chin roughly toward the _camera's forward direction.
        var frame = Quaternion.LookRotation(groundNormal, -Camera.transform.up);

        // Note that this will need a little special handling if your _camera
        // can ever look nearly straight up/down along the ground normal.

        // Rotate the coordinate frame 90 degrees forward.
        frame = frame * Quaternion.AngleAxis(90f, Vector3.right);

        return frame;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveDelta = context.ReadValue<Vector2>();
    }

    public void OnMouseDelta(InputAction.CallbackContext context)
    {
        MouseDelta = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        Sprint = context.ReadValue<float>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        Jump = context.ReadValue<float>();
    }
}