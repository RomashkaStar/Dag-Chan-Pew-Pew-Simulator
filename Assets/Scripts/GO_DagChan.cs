using UnityEngine;
using UnityEngine.Animations;

public class GO_DagChan : MonoBehaviour
{

    [Header("Движение")]
    [SerializeField] private float _MoveSpeed = 5f;
    [SerializeField] private float _JumpHeight = 1.5f;
    [SerializeField] private float _Gravity = -9.8f;

    [Header("Обзор мышью")]
    [SerializeField] private float _MouseSensitivity = 1500f;
    [SerializeField] private Transform _Camera;

    private CharacterController _CharacterController;

    private float _VerticalVelocity;
    private float _CameraRotationX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _CharacterController = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Look();
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement =
            transform.right * horizontal +
            transform.forward * vertical;

        movement = movement.normalized;

        bool isGrounded = _CharacterController.isGrounded;

        if (isGrounded && _VerticalVelocity < 0)
        {
            _VerticalVelocity = -2f;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            _VerticalVelocity =
                Mathf.Sqrt(_JumpHeight * -2f * _Gravity);
        }

        _VerticalVelocity += _Gravity * Time.deltaTime;

        movement *= _MoveSpeed;

        movement.y = _VerticalVelocity;

        _CharacterController.Move(
            movement * Time.deltaTime
        );
    }

    private void Look()
    {
        float mouseX =
            Input.GetAxis("Mouse X") * _MouseSensitivity * Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") * _MouseSensitivity * Time.deltaTime;

        // Влево-вправо крутим всего персонажа.
        transform.Rotate(Vector3.up * mouseX);

        // Вверх-вниз крутим только камеру.
        _CameraRotationX -= mouseY;

        // Чтобы нельзя было сломать шею на 360 градусов.
        _CameraRotationX = Mathf.Clamp(
            _CameraRotationX,
            -90f,
            90f
        );

        _Camera.localRotation =
            Quaternion.Euler(_CameraRotationX, 0f, 0f);
    }
}
