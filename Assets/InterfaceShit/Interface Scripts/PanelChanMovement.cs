using UnityEngine;

public class UIJumpEffect : MonoBehaviour
{
    // CharacterController персонажа.
    [SerializeField] private CharacterController _CharacterController;

    [Header("Jump")]
    // Насколько UI-объект поднимается,
    // пока персонаж летит вверх.
    [SerializeField] private float _JumpOffsetY = 10f;

    [Header("Landing")]
    // Насколько UI-объект дёрнется вниз при приземлении.
    [SerializeField] private float _LandingKickY = 15f;

    [Header("Smoothing")]
    // Насколько быстро объект двигается к нужной позиции.
    [SerializeField] private float _MoveSmoothness = 8f;

    // Насколько быстро объект возвращается
    // после толчка при приземлении.
    [SerializeField] private float _KickReturnSpeed = 8f;


    // RectTransform UI-объекта.
    private RectTransform _RectTransform;

    // Его обычная позиция.
    private Vector2 _StartPosition;

    // Текущее плавное смещение от прыжка.
    private float _CurrentJumpOffset;

    // Отдельное смещение от удара при приземлении.
    private float _LandingKick;

    // Стояли ли мы на земле в прошлом кадре.
    private bool _WasGrounded;

    private bool isGrounded;

    private float verticalVelocity;

    private float targetJumpOffset;

    private float moveAmount;

    private float returnAmount;


    private void Start()
    {
        // Получаем RectTransform объекта,
        // потому что работаем с UI.
        _RectTransform = GetComponent<RectTransform>();

        // Запоминаем его обычную позицию.
        _StartPosition = _RectTransform.anchoredPosition;

        // Запоминаем состояние земли в начале игры.
        _WasGrounded = _CharacterController.isGrounded;
    }


    private void LateUpdate()
    {
        isGrounded =
            _CharacterController.isGrounded;

        verticalVelocity =
            _CharacterController.velocity.y;


        // По умолчанию объект хочет вернуться
        // в свою обычную позицию.
        targetJumpOffset = 0f;


        // Если персонаж находится в воздухе
        // и летит ВВЕРХ...
        if (!isGrounded && verticalVelocity > 0f)
        {
            // ...немного поднимаем UI-объект.
            targetJumpOffset = _JumpOffsetY;
        }


        // Плавно двигаем текущее смещение
        // к нужному значению.
        moveAmount =
            1f - Mathf.Exp(
                -_MoveSmoothness * Time.deltaTime
            );

        _CurrentJumpOffset =
            Mathf.Lerp(
                _CurrentJumpOffset,
                targetJumpOffset,
                moveAmount
            );


        // Если в прошлом кадре были в воздухе,
        // а сейчас уже стоим на земле,
        // значит произошло приземление.
        if (isGrounded && !_WasGrounded)
        {
            // Дёргаем объект вниз.
            _LandingKick -= _LandingKickY;
        }


        // После приземления постепенно
        // возвращаем толчок обратно к нулю.
        returnAmount =
            1f - Mathf.Exp(
                -_KickReturnSpeed * Time.deltaTime
            );

        _LandingKick =
            Mathf.Lerp(
                _LandingKick,
                0f,
                returnAmount
            );


        // Применяем итоговое положение.
        _RectTransform.anchoredPosition =
            _StartPosition
            + Vector2.up *
            (_CurrentJumpOffset + _LandingKick);


        // Сохраняем текущее состояние земли,
        // чтобы в следующем кадре оно стало "прошлым".
        _WasGrounded = isGrounded;
    }
}