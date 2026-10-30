
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class GO_DagChan : MonoBehaviour
{
    [Header("Движение")]

    [SerializeField] private float _MoveSpeed = 5f;
    [SerializeField] private float _JumpHeight = 1.5f;
    [SerializeField] private float _Gravity = -9.8f;


    [Header("Обзор мышью")]

    // В НОВОЙ системе чувствительность другая!
    // Начни с 0.1 и настрой под себя.
    [SerializeField] private float _MouseSensitivity = 0.1f;
    [SerializeField] private Transform _Camera;


    [Header("Обзор геймпадом")]

    [SerializeField] private float _GamepadLookSpeed = 100f;


    // Компоненты игрока.
    private CharacterController _CharacterController;
    private PlayerInput _PlayerInput;


    // Ссылки на действия из Input Actions.
    //
    // Они НЕ хранят конкретные кнопки.
    // Например, Move знает, что ему нужно двигаться,
    // но физические клавиши заданы в DagChanControls.
    private InputAction _MoveAction;
    private InputAction _LookAction;
    private InputAction _JumpAction;


    // Вертикальная скорость для гравитации и прыжка.
    private float _VerticalVelocity;

    // Накопленный угол поворота камеры вверх-вниз.
    private float _CameraRotationX;


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Start()
    {
        // Получаем CharacterController.
        _CharacterController =
            GetComponent<CharacterController>();

        // Получаем новый компонент PlayerInput.
        _PlayerInput =
            GetComponent<PlayerInput>();


        // Проверяем, назначен ли Input Actions Asset.
        if (_PlayerInput.actions == null)
        {
            Debug.LogError(
                "У PlayerInput не назначен Input Actions Asset!",
                this
            );

            enabled = false;
            return;
        }


        // Получаем действия ИМЕННО из PlayerInput.
        //
        // Строки должны совпадать с названиями
        // действий внутри DagChanControls.
        _MoveAction =
            _PlayerInput.actions.FindAction("Move");

        _LookAction =
            _PlayerInput.actions.FindAction("Look");

        _JumpAction =
            _PlayerInput.actions.FindAction("Jump");


        // Проверяем, что всё найдено.
        if (_MoveAction == null ||
            _LookAction == null ||
            _JumpAction == null)
        {
            Debug.LogError(
                "Не найдены Move, Look или Jump! " +
                "Проверь названия действий.",
                this
            );

            enabled = false;
            return;
        }


        if (_Camera == null)
        {
            Debug.LogError(
                "Не назначена камера игрока!",
                this
            );

            enabled = false;
            return;
        }


        // Считываем первоначальный угол камеры.
        // DeltaAngle переводит его в удобный
        // диапазон от -180 до +180 градусов.
        _CameraRotationX = Mathf.DeltaAngle(
            0f,
            _Camera.localEulerAngles.x
        );


        // Захватываем курсор мыши.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void Update()
    {
        Move();
        Look();
    }


    // ------------------------------------
    // ДВИЖЕНИЕ
    // ------------------------------------

    private void Move()
    {
        // НОВАЯ СИСТЕМА!
        //
        // Вместо двух Input.GetAxisRaw()
        // получаем один Vector2.
        //
        // X = влево / вправо
        // Y = назад / вперёд
        Vector2 moveInput =
            _MoveAction.ReadValue<Vector2>();


        // Переводим 2D-ввод в 3D-направление.
        //
        // transform.right  — вправо от игрока.
        // transform.forward — вперёд от игрока.
        Vector3 movement =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;


        // Не позволяем двигаться по диагонали
        // быстрее обычного.
        //
        // ClampMagnitude ограничивает длину
        // вектора единицей, но сохраняет
        // аналоговое управление геймпада.
        movement = Vector3.ClampMagnitude(
            movement,
            1f
        );


        // Проверяем, находится ли игрок на земле.
        bool isGrounded =
            _CharacterController.isGrounded;


        // Если стоим на земле и скорость вниз
        // отрицательная, оставляем небольшое
        // прижатие к поверхности.
        if (isGrounded && _VerticalVelocity < 0f)
        {
            _VerticalVelocity = -2f;
        }


        // НОВАЯ СИСТЕМА!
        //
        // WasPressedThisFrame() — аналог
        // старого Input.GetKeyDown().
        //
        // Проверяем, нажали ли Jump
        // именно в текущем кадре.
        if (_JumpAction.WasPressedThisFrame() &&
            isGrounded)
        {
            _VerticalVelocity = Mathf.Sqrt(
                _JumpHeight * -2f * _Gravity
            );
        }


        // Применяем гравитацию.
        _VerticalVelocity +=
            _Gravity * Time.deltaTime;


        // Превращаем направление движения
        // в горизонтальную скорость.
        movement *= _MoveSpeed;


        // Добавляем вертикальную скорость.
        movement.y = _VerticalVelocity;


        // CharacterController.Move ожидает
        // ПЕРЕМЕЩЕНИЕ за кадр, а не скорость.
        //
        // Поэтому умножаем на deltaTime.
        _CharacterController.Move(
            movement * Time.deltaTime
        );
    }


    // ------------------------------------
    // ОБЗОР МЫШЬЮ
    // ------------------------------------


    private void Look()
    {
        // Получаем Vector2 из нашего действия Look.
        //
        // Input System сама выбирает,
        // от какого устройства поступает ввод.
        Vector2 lookInput =
            _LookAction.ReadValue<Vector2>();


        // Проверяем, какое устройство сейчас
        // управляет действием Look.
        //
        // Если это Gamepad, значит движение
        // пришло от правого стика.
        bool isGamepad =
            _LookAction.activeControl?.device is Gamepad;


        // Для геймпада скорость измеряется
        // в градусах за секунду.
        //
        // Для мыши — в градусах
        // на единицу перемещения мыши.
        float sensitivity = isGamepad
            ? _GamepadLookSpeed * Time.deltaTime
            : _MouseSensitivity;


        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;


        // Поворачиваем всего персонажа по горизонтали.
        transform.Rotate(Vector3.up * mouseX);


        // Поворачиваем камеру по вертикали.
        _CameraRotationX -= mouseY;

        _CameraRotationX = Mathf.Clamp(
            _CameraRotationX,
            -90f,
            90f
        );


        _Camera.localRotation = Quaternion.Euler(
            _CameraRotationX,
            0f,
            0f
        );
    }

}
