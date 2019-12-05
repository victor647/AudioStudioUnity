using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    public float gravity = 20.0f;
    public float jumpSpeed = 8.0f;

    private Vector3 moveDirection = Vector3.zero;
    public float speed = 6.0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (controller.isGrounded)
        {
            // We are grounded, so recalculate
            // move direction directly from axes

            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection = moveDirection * speed;

            if (Input.GetButton("Jump")) moveDirection.y = jumpSpeed;
        }

        // Apply gravity
        moveDirection.y = moveDirection.y - gravity * Time.deltaTime;

        // Move the controller
        controller.Move(moveDirection * Time.deltaTime);
    }
}