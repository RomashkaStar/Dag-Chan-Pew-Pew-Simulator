using UnityEngine;

public class WeaponUIBob : MonoBehaviour
{
    [SerializeField] private CharacterController _CharacterController;

    [Header("Walking")]
    [SerializeField] private float _BobFrequency = 10f;

    // Насколько руки ходят влево-вправо в пикселях.
    [SerializeField] private float _BobHorizontal = 12f;

    // Насколько руки ходят вверх-вниз в пикселях.
    [SerializeField] private float _BobVertical = 8f;

    // Наклон рук в градусах.
    [SerializeField] private float _BobRotation = 2f;


    [Header("Jump")]
    // Насколько руки проседают вниз при прыжке.
    [SerializeField] private float _JumpKickY = -10f;

    // Поворот при прыжке.
    [SerializeField] private float _JumpKickRotation = -2f;


    [Header("Landing")]
    // Насколько руки проваливаются вниз при приземлении.
    [SerializeField] private float _LandingKickY = -20f;

    // Поворот при приземлении.
    [SerializeField] private float _LandingKickRotation = 4f;


    [Header("Smoothing")]
    [SerializeField] private float _BobSmoothness = 12f;
    [SerializeField] private float _KickReturnSpeed = 8f;


    // RectTransform нашего UI-спрайта.
    private RectTransform _RectTransform;

    // Начальное положение рук на Canvas.
    private Vector2 _StartPosition;

    // Начальный поворот рук.
    private Quaternion _StartRotation;

    // Текущее смещение от ходьбы.
    private Vector2 _CurrentBob;

    // Текущий наклон от ходьбы.
    private float _CurrentRotation;

    // Смещение после прыжка/приземления.
    private float _KickY;

    // Поворот после прыжка/приземления.
    private float _KickRotation;

    // Таймер шагов.
    private float _BobTimer;

    // Стояли ли мы на земле в прошлом кадре.
    private bool _WasGrounded;

    // Максимальная скорость падения.
    private float _LowestAirVelocity;

    private bool isGrounded;

    private Vector3 velocity;

    private float horizontalSpeed;

    private Vector2 targetBob;

    private float targetRotation;

    private float smoothing;

    private float fallSpeed;

    private float impact;

    private float returnAmount;


    private void Start()
    {
        // Получаем RectTransform этого UI-объекта.
        _RectTransform = GetComponent<RectTransform>();

        // Запоминаем, где руки находились изначально.
        _StartPosition = _RectTransform.anchoredPosition;

        // Запоминаем изначальный поворот.
        _StartRotation = _RectTransform.localRotation;


        // Если CharacterController не указали вручную,
        // ищем его у родителей.
        if (_CharacterController == null)
        {
            _CharacterController =
                GetComponentInParent<CharacterController>();
        }


        _WasGrounded =
            _CharacterController.isGrounded;
    }


    private void LateUpdate()
    {
        isGrounded =
            _CharacterController.isGrounded;

        velocity =
            _CharacterController.velocity;


        // Вычисляем скорость только по земле.
        horizontalSpeed =
            new Vector3(
                velocity.x,
                0f,
                velocity.z
            ).magnitude;


        HandleWalkingBob(
            isGrounded,
            horizontalSpeed
        );

        HandleJumpAndLanding(
            isGrounded,
            velocity.y
        );

        ApplyEffects();


        _WasGrounded =
            isGrounded;
    }


    private void HandleWalkingBob(
        bool isGrounded,
        float speed
    )
    {
        targetBob =
            Vector2.zero;

        targetRotation = 0f;


        if (isGrounded && speed > 0.1f)
        {
            // Таймер постоянно идёт по кругу от 0 до 2π.
            _BobTimer = Mathf.Repeat(
                _BobTimer
                + Time.deltaTime * _BobFrequency,

                Mathf.PI * 2f
            );


            // Движение рук влево-вправо.
            targetBob.x =
                Mathf.Sin(_BobTimer)
                * _BobHorizontal;


            // Движение вверх-вниз.
            targetBob.y =
                Mathf.Sin(_BobTimer * 2f)
                * _BobVertical;


            // Небольшой наклон спрайта.
            targetRotation =
                -Mathf.Sin(_BobTimer)
                * _BobRotation;
        }


        smoothing =
            1f - Mathf.Exp(
                -_BobSmoothness * Time.deltaTime
            );


        _CurrentBob =
            Vector2.Lerp(
                _CurrentBob,
                targetBob,
                smoothing
            );


        _CurrentRotation =
            Mathf.Lerp(
                _CurrentRotation,
                targetRotation,
                smoothing
            );
    }


    private void HandleJumpAndLanding(
        bool isGrounded,
        float verticalVelocity
    )
    {
        // Сейчас находимся в воздухе.
        if (!isGrounded)
        {
            // Запоминаем максимальную скорость падения.
            _LowestAirVelocity =
                Mathf.Min(
                    _LowestAirVelocity,
                    verticalVelocity
                );


            // В прошлом кадре стояли на земле,
            // а сейчас летим вверх.
            // Значит только что прыгнули.
            if (_WasGrounded && verticalVelocity > 0f)
            {
                _KickY +=
                    _JumpKickY;

                _KickRotation +=
                    _JumpKickRotation;
            }
        }


        // В прошлом кадре были в воздухе,
        // сейчас на земле.
        // Значит приземлились.
        if (isGrounded && !_WasGrounded)
        {
            fallSpeed =
                -_LowestAirVelocity;


            // От 0 до 1 в зависимости
            // от силы падения.
            impact =
                Mathf.InverseLerp(
                    2f,
                    12f,
                    fallSpeed
                );


            _KickY +=
                _LandingKickY * impact;

            _KickRotation +=
                _LandingKickRotation * impact;


            _LowestAirVelocity = 0f;
        }


        // Плавно возвращаем руки обратно.
        float returnAmount =
            1f - Mathf.Exp(
                -_KickReturnSpeed * Time.deltaTime
            );


        _KickY =
            Mathf.Lerp(
                _KickY,
                0f,
                returnAmount
            );


        _KickRotation =
            Mathf.Lerp(
                _KickRotation,
                0f,
                returnAmount
            );
    }


    private void ApplyEffects()
    {
        // Здесь самое главное отличие от обычного объекта:
        //
        // используем anchoredPosition,
        // потому что это UI на Canvas.
        _RectTransform.anchoredPosition =
            _StartPosition
            + _CurrentBob
            + Vector2.up * _KickY;


        Quaternion effectRotation =
            Quaternion.Euler(
                0f,
                0f,
                _CurrentRotation + _KickRotation
            );


        _RectTransform.localRotation =
            _StartRotation * effectRotation;
    }
}