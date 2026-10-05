using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    //Через CharacterController персонажа мы узнаем стоит ли персонаж на земле
    //и с какой скоростью он сейчас движется
    [SerializeField] private CharacterController _CharacterController;

    [Header("Walking")]
    //Частота покачиваний камеры при ходьбе
    [SerializeField] private float _BobFrequency = 10f;
    //Разброс покачиваний влево-вправо
    [SerializeField] private float _BobHorizontal = 0.025f;
    //Разброс покачиваний вверх-вниз
    [SerializeField] private float _BobVertical = 0.035f;
    //Как сильно камера заваливаетсяя набок
    [SerializeField] private float _BobRoll = 0.6f;

    [Header("Jump")]
    //Как сильно камера сдвинется вверх в прыжке
    [SerializeField] private float _JumpKickY = 0.03f;
    //Как сильно камера наклонится по оси X в прыжке (то есть чуть поднимет язгляд вверх)
    [SerializeField] private float _JumpKickPitch = -1f;

    [Header("Landing")]
    //Как сильно камера провалится вниз во время приземления
    [SerializeField] private float _LandingKickY = 0.08f;
    //Как сильно камера дернется по оси X в приземелении
    [SerializeField] private float _LandingKickPitch = 2.5f;

    [Header("Smoothing")]
    //Скорость, с которость с которой камера плавно приходит к нужному положению во время покачивания
    //Чем больше значение, тем быстрее камера двигается
    [SerializeField] private float _BobSmoothness = 12f;
    //Скорость возвращения камеры к стандартному положению после прыжка или приземления
    [SerializeField] private float _KickReturnSpeed = 8f;

    //Здесь задается стандартное положение камеры
    private Vector3 _StartPosition;

    //Текущее смещение камеры во время ходьбы
    private Vector3 _CurrentBob;
    //Текущий наклон камеры во времяя ходьбы
    private float _CurrentRoll;

    //Дополнительное смещение камеры по Y из-за прыжка или приземления
    private float _KickY;
    //Дополнительный поворот камеры по оси X из-за прыжка или приземления
    private float _KickPitch;

    //Таймер для анимации покачивания камеры
    private float _BobTimer;

    private bool _WasGrounded;
    private float _LowestAirVelocity;

    private void Start()
    {
        _StartPosition = transform.localPosition;

        if (_CharacterController == null)
        {
            _CharacterController =
                GetComponentInParent<CharacterController>();
        }

        _WasGrounded = _CharacterController.isGrounded;
    }

    private void LateUpdate()
    {
        bool isGrounded = _CharacterController.isGrounded;

        Vector3 velocity = _CharacterController.velocity;

        float horizontalSpeed =
            new Vector3(velocity.x, 0f, velocity.z).magnitude;

        HandleJumpAndLanding(isGrounded, velocity.y);

        HandleWalkingBob(isGrounded, horizontalSpeed);

        ApplyEffects();

        _WasGrounded = isGrounded;
    }

    private void HandleWalkingBob(bool isGrounded, float speed)
    {
        Vector3 targetBob = Vector3.zero;
        float targetRoll = 0f;

        if (isGrounded && speed > 0.1f)
        {
            _BobTimer = Mathf.Repeat(_BobTimer + Time.deltaTime * _BobFrequency, Mathf.PI * 2f);

            // Влево / вправо
            targetBob.x =
                Mathf.Sin(_BobTimer) * _BobHorizontal;

            // Вверх / вниз
            targetBob.y =
                Mathf.Sin(_BobTimer * 2f) * _BobVertical;

            // Маленький наклон камеры
            targetRoll =
                -Mathf.Sin(_BobTimer) * _BobRoll;
        }

        float smoothing =
            1f - Mathf.Exp(-_BobSmoothness * Time.deltaTime);

        _CurrentBob =
            Vector3.Lerp(_CurrentBob, targetBob, smoothing);

        _CurrentRoll =
            Mathf.Lerp(_CurrentRoll, targetRoll, smoothing);
    }

    private void HandleJumpAndLanding(bool isGrounded, float verticalVelocity)
    {
        // Пока летим, запоминаем максимальную скорость падения.
        if (!isGrounded)
        {
            _LowestAirVelocity =
                Mathf.Min(_LowestAirVelocity, verticalVelocity);

            // Только что оторвались от земли.
            if (_WasGrounded && verticalVelocity > 0f)
            {
                _KickY += _JumpKickY;
                _KickPitch += _JumpKickPitch;
            }
        }

        // Только что приземлились.
        if (isGrounded && !_WasGrounded)
        {
            float fallSpeed = -_LowestAirVelocity;

            // Чем сильнее падение, тем сильнее эффект.
            float impact =
                Mathf.InverseLerp(2f, 12f, fallSpeed);

            _KickY -= _LandingKickY * impact;
            _KickPitch += _LandingKickPitch * impact;

            _LowestAirVelocity = 0f;
        }

        // Постепенно возвращаем камеру назад.
        float returnAmount =
            1f - Mathf.Exp(-_KickReturnSpeed * Time.deltaTime);

        _KickY =
            Mathf.Lerp(_KickY, 0f, returnAmount);

        _KickPitch =
            Mathf.Lerp(_KickPitch, 0f, returnAmount);
    }

    private void ApplyEffects()
    {
        // Позиция камеры.
        transform.localPosition =
            _StartPosition +
            _CurrentBob +
            Vector3.up * _KickY;

        // Базовый поворот уже поставил твой Look().
        Quaternion normalRotation = transform.localRotation;

        Quaternion effectRotation =
            Quaternion.Euler(
                _KickPitch,
                0f,
                _CurrentRoll
            );

        transform.localRotation =
            normalRotation * effectRotation;
    }
}