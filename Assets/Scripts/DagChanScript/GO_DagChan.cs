
using UnityEngine;
using UnityEngine.InputSystem;

// Скрипт требует компоненты CharacterController
// и PlayerInput на том же GameObject.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class GO_DagChan : MonoBehaviour
{
    [Header("Движение")]

    [SerializeField] private float _MoveSpeed = 10f;
    [SerializeField] private float _JumpHeight = 1.5f;
    [SerializeField] private float _Gravity = -9.8f;


    [Header("Обзор мышью")]

    // Mouse Delta передаёт смещение за кадр,
    // поэтому чувствительность здесь небольшая.
    [SerializeField] private float _MouseSensitivity = 0.1f;
    [SerializeField] private Transform _Camera;


    [Header("Обзор геймпадом")]

    // Общая скорость поворота для геймпада и руля.
    [SerializeField] private float _GamepadLookSpeed = 100f;


    private CharacterController _CharacterController;
    private PlayerInput _PlayerInput;


    // Получаем действия из Input Actions.
    // Конкретные кнопки назначаются в DagChanControls,
    // а не прописываются непосредственно в этом скрипте.
    private InputAction _MoveAction;
    private InputAction _LookAction;
    private InputAction _JumpAction;


    // CharacterController не рассчитывает гравитацию
    // автоматически, поэтому вертикальную скорость
    // прыжка и падения храним самостоятельно.
    private float _VerticalVelocity;

    // Угол наклона камеры по вертикали.
    // Хранится отдельно от поворота самого игрока.
    private float _CameraRotationX;

    // Ссылка на текущую лестницу.
    // Если null — используется обычное движение.
    private LadderZone _CurrentLadder;


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Start()
    {
        _CharacterController =
            GetComponent<CharacterController>();

        _PlayerInput =
            GetComponent<PlayerInput>();


        if (_PlayerInput.actions == null)
        {
            Debug.LogError(
                "У PlayerInput не назначен Input Actions Asset!",
                this
            );

            enabled = false;
            return;
        }


        // Берём действия из PlayerInput игрока.
        // Это позволяет использовать одни и те же
        // действия для клавиатуры, геймпада и руля.
        _MoveAction =
            _PlayerInput.actions.FindAction("Move");

        _LookAction =
            _PlayerInput.actions.FindAction("Look");

        _JumpAction =
            _PlayerInput.actions.FindAction("Jump");


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


        // Запоминаем первоначальный наклон камеры.
        // DeltaAngle переводит угол из формата 0–360
        // в диапазон -180...180.
        //
        // Это нужно, чтобы дальнейшие ограничения
        // наклона камеры работали корректно.
        _CameraRotationX = Mathf.DeltaAngle(
            0f,
            _Camera.localEulerAngles.x
        );


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
        // Move возвращает Vector2:
        // X — движение влево/вправо;
        // Y — движение вперёд/назад.
        //
        // Неважно, пришли значения от WASD,
        // стика геймпада или другого устройства.
        Vector2 moveInput =
            _MoveAction.ReadValue<Vector2>();


        // Преобразуем двумерный ввод в направление
        // движения в трёхмерном пространстве.
        //
        // transform.right — направление вправо
        // относительно самого персонажа.
        //
        // transform.forward — направление вперёд
        // относительно персонажа.
        //
        // Поэтому WASD всегда двигают игрока
        // относительно того, куда он смотрит.
        Vector3 movement =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;


        // Ограничиваем длину вектора единицей.
        // Иначе движение по диагонали
        // могло бы быть быстрее обычного.
        movement = Vector3.ClampMagnitude(
            movement,
            1f
        );

        movement *= _MoveSpeed;


        // ==================================
        // ДВИЖЕНИЕ ПО ЛЕСТНИЦЕ
        // ==================================

        if (_CurrentLadder != null)
        {
            // Пока игрок на лестнице, отключаем
            // накопленную вертикальную скорость.
            // Иначе после падения он мог бы
            // продолжать двигаться вниз.
            _VerticalVelocity = 0f;


            // LadderZone передаёт свою скорость.
            // Подъём автоматический: игроку
            // не нужно удерживать кнопку.
            movement.y = _CurrentLadder.ClimbSpeed;


            _CharacterController.Move(
                movement * Time.deltaTime
            );


            // Выходим из Move(), чтобы не применять
            // обычную гравитацию и прыжок.
            return;
        }


        // ==================================
        // ОБЫЧНОЕ ДВИЖЕНИЕ
        // ==================================

        // CharacterController сообщает,
        // соприкасается ли игрок с землёй.
        bool isGrounded =
            _CharacterController.isGrounded;


        // Когда персонаж стоит на земле,
        // удерживаем небольшую отрицательную
        // вертикальную скорость.
        //
        // Это помогает сохранять контакт
        // с поверхностью вместо накопления падения.
        if (isGrounded && _VerticalVelocity < 0f)
        {
            _VerticalVelocity = -2f;
        }


        // ==================================
        // ПРЫЖОК
        // ==================================

        if (_JumpAction.WasPressedThisFrame() &&
            isGrounded)
        {
            // Рассчитываем стартовую скорость прыжка
            // по заданной высоте и силе гравитации.
            //
            // Формула взята из кинематики:
            // v = sqrt(2 * g * h)
            //
            // Поскольку _Gravity отрицательная,
            // умножаем её на -2.
            //
            // Например, JumpHeight = 1.5
            // означает желаемую высоту прыжка
            // примерно 1.5 единицы Unity.
            _VerticalVelocity = Mathf.Sqrt(
                _JumpHeight * -2f * _Gravity
            );
        }


        // ==================================
        // ГРАВИТАЦИЯ
        // ==================================

        // Каждый кадр изменяем вертикальную скорость
        // под действием ускорения свободного падения.
        //
        // Чем дольше падаем, тем больше становится
        // отрицательная вертикальная скорость.
        _VerticalVelocity +=
            _Gravity * Time.deltaTime;


        // Соединяем горизонтальное движение
        // с вертикальной скоростью прыжка/падения.
        movement.y = _VerticalVelocity;


        // CharacterController.Move перемещает
        // персонажа с учётом столкновений.
        //
        // Move принимает смещение, а не скорость,
        // поэтому умножаем movement на deltaTime.
        //
        // CharacterController сам по себе
        // не применяет гравитацию — мы уже
        // рассчитали её выше.
        _CharacterController.Move(
            movement * Time.deltaTime
        );
    }


    // ------------------------------------
    // ЛЕСТНИЦЫ
    // ------------------------------------

    // Этот метод вызывает LadderZone,
    // когда персонаж входит в её Trigger.
    public void EnterLadder(LadderZone ladder)
    {
        if (_CurrentLadder == ladder)
        {
            return;
        }

        // Запоминаем лестницу.
        // Теперь Move() переключится на
        // специальную логику подъёма.
        _CurrentLadder = ladder;

        _VerticalVelocity = 0f;
    }


    // LadderZone вызывает этот метод,
    // когда игрок покидает её Trigger.
    public void ExitLadder(LadderZone ladder)
    {
        // Проверяем, что вышли именно
        // из текущей лестницы.
        //
        // Это полезно, если триггеры
        // нескольких лестниц пересекаются.
        if (_CurrentLadder == ladder)
        {
            _CurrentLadder = null;

            // Теперь Move() снова будет
            // использовать обычную гравитацию.
            _VerticalVelocity = 0f;
        }
    }


    // ------------------------------------
    // ОБЗОР КАМЕРОЙ
    // ------------------------------------

    private void Look()
    {
        Vector2 lookInput =
            _LookAction.ReadValue<Vector2>();


        // Определяем устройство, которое сейчас
        // управляет действием Look.
        //
        // Наш Logitech MOMO Racing определяется
        // системой как Joystick.
        bool isWheel =
            _LookAction.activeControl?.device is Joystick;

        bool isGamepad =
            _LookAction.activeControl?.device is Gamepad;


        // Мышь возвращает delta — смещение
        // за конкретный кадр.
        //
        // Геймпад и руль возвращают положение
        // органа управления (например, стика).
        //
        // Поэтому для геймпада и руля используем
        // скорость поворота с учётом deltaTime,
        // а для мыши — отдельную чувствительность
        // без дополнительного deltaTime.
        float sensitivity = (isGamepad || isWheel)
            ? _GamepadLookSpeed * Time.deltaTime
            : _MouseSensitivity;


        float mouseX = lookInput.x * sensitivity;


        // При управлении рулём используем
        // только горизонтальный поворот.
        float mouseY = isWheel
            ? 0f
            : lookInput.y * sensitivity;


        // ==================================
        // ГОРИЗОНТАЛЬНЫЙ ПОВОРОТ
        // ==================================

        // Поворачиваем весь объект игрока
        // вокруг его вертикальной оси Y.
        //
        // Поэтому направление движения
        // тоже меняется вместе с поворотом.
        transform.Rotate(Vector3.up * mouseX);


        // ==================================
        // ВЕРТИКАЛЬНЫЙ ПОВОРОТ
        // ==================================

        if (isWheel)
        {
            // Для руля фиксируем наклон камеры
            // в горизонтальном положении.
            _CameraRotationX = 0f;
        }
        else
        {
            // Для мыши и геймпада меняем
            // вертикальный наклон камеры.
            //
            // Угол сохраняется между кадрами,
            // потому что движение мыши передаёт
            // не положение, а изменение положения.
            _CameraRotationX -= mouseY;


            // Запрещаем поворачивать камеру
            // дальше 90 градусов вверх и вниз.
            //
            // Без ограничения можно было бы
            // перевернуть её через голову.
            _CameraRotationX = Mathf.Clamp(
                _CameraRotationX,
                -90f,
                90f
            );
        }


        // Quaternion.Euler создаёт поворот
        // по указанным углам.
        //
        // Здесь используем localRotation,
        // потому что камера является дочерним
        // объектом игрока.
        //
        // Игрок отвечает за горизонтальный поворот,
        // а камера — за вертикальный.
        _Camera.localRotation = Quaternion.Euler(
            _CameraRotationX,
            0f,
            0f
        );
    }
}
