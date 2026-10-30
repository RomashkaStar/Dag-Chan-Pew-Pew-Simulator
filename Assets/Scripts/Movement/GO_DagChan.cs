
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class GO_DagChan : MonoBehaviour
{
    [Header("Движение")]

    [SerializeField] private float _MoveSpeed = 10f;
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

    // Лестница, на которой сейчас находится игрок.
    // null означает, что игрок не на лестнице.
    private LadderZone _CurrentLadder;


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
        // Получаем ввод с клавиатуры, геймпада
        // или другого привязанного устройства.
        Vector2 moveInput =
            _MoveAction.ReadValue<Vector2>();


        // Рассчитываем направление движения.
        Vector3 movement =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        movement = Vector3.ClampMagnitude(
            movement,
            1f
        );

        movement *= _MoveSpeed;


        // ==================================
        // ЕСЛИ ИГРОК НА ЛЕСТНИЦЕ
        // ==================================

        if (_CurrentLadder != null)
        {
            // Полностью убираем накопленную
            // вертикальную скорость падения.
            _VerticalVelocity = 0f;


            // Автоматически двигаемся вверх.
            //
            // Игроку не нужно ничего нажимать!
            movement.y = _CurrentLadder.ClimbSpeed;


            // Передвигаем CharacterController.
            _CharacterController.Move(
                movement * Time.deltaTime
            );

            // Не выполняем обычную гравитацию
            // и прыжок, пока находимся на лестнице.
            return;
        }


        // ==================================
        // ОБЫЧНОЕ ДВИЖЕНИЕ
        // ==================================

        bool isGrounded =
            _CharacterController.isGrounded;


        if (isGrounded && _VerticalVelocity < 0f)
        {
            _VerticalVelocity = -2f;
        }


        // Прыжок.
        if (_JumpAction.WasPressedThisFrame() &&
            isGrounded)
        {
            _VerticalVelocity = Mathf.Sqrt(
                _JumpHeight * -2f * _Gravity
            );
        }


        // Гравитация.
        _VerticalVelocity +=
            _Gravity * Time.deltaTime;


        // Добавляем вертикальное движение.
        movement.y = _VerticalVelocity;


        // Перемещаем игрока.
        _CharacterController.Move(
            movement * Time.deltaTime
        );
    }



    public void EnterLadder(LadderZone ladder)
    {
        // Если игрок уже на этой лестнице,
        // ничего повторно делать не нужно.
        if (_CurrentLadder == ladder)
        {
            return;
        }

        _CurrentLadder = ladder;

        // Отключаем накопленную скорость падения.
        // Иначе гравитация мешала бы подъёму.
        _VerticalVelocity = 0f;
    }


    public void ExitLadder(LadderZone ladder)
    {
        // Сбрасываем только ту лестницу,
        // с которой действительно вышли.
        if (_CurrentLadder == ladder)
        {
            _CurrentLadder = null;

            // После выхода снова будет
            // применяться обычная гравитация.
            _VerticalVelocity = 0f;
        }
    }



    // ------------------------------------
    // ОБЗОР МЫШЬЮ
    // ------------------------------------



    private void Look()
    {
        // Получаем движение мыши, геймпада или руля.
        Vector2 lookInput =
            _LookAction.ReadValue<Vector2>();


        // Проверяем, пришёл ли ввод от руля.
        // Logitech MOMO определяется как Joystick.
        bool isWheel =
            _LookAction.activeControl?.device is Joystick;


        // Проверяем, пришёл ли ввод от геймпада.
        bool isGamepad =
            _LookAction.activeControl?.device is Gamepad;


        // Для руля и геймпада используем
        // ОДНУ И ТУ ЖЕ чувствительность.
        float sensitivity = (isGamepad || isWheel)
            ? _GamepadLookSpeed * Time.deltaTime
            : _MouseSensitivity;


        float mouseX = lookInput.x * sensitivity;

        // Рулю запрещаем поворачивать камеру
        // вверх и вниз.
        float mouseY = isWheel
            ? 0f
            : lookInput.y * sensitivity;


        // Поворачиваем персонажа по горизонтали.
        transform.Rotate(Vector3.up * mouseX);


        if (isWheel)
        {
            // При управлении рулём
            // камера всегда смотрит прямо.
            _CameraRotationX = 0f;
        }
        else
        {
            // Мышь и геймпад могут смотреть вверх-вниз.
            _CameraRotationX -= mouseY;

            _CameraRotationX = Mathf.Clamp(
                _CameraRotationX,
                -90f,
                90f
            );
        }


        _Camera.localRotation = Quaternion.Euler(
            _CameraRotationX,
            0f,
            0f
        );
    }


}
