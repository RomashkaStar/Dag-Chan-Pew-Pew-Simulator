using System.ComponentModel;
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

    //Эта переменная показывает, стояли мы на земле в прошлом кадре или нет
    private bool _WasGrounded;
    
    //Эта переменная показывает, стоим ли мы на земле прямо сейчас
    private bool isGrounded;

    //Самая большая скорость падения, которую персонаж набрал, пока летел вниз
    //Чем число меньше, тем быстрее мы падаем (Так как скорость падения отрицательная (минус по Y))
    private float _LowestAirVelocity;

    //Скорость ходьбы по земле
    private float horizontalSpeed;

    //Желаемый наклон камеры при передвижении
    private float targetRoll = 0f;

    //Скорость возвращения камеры к стандартному положению при ходьбе
    private float smoothing;

    //Скорость падения
    private float fallSpeed;

    //Сила толчка камеры при приземлении, зависящая от скорости падения
    private float impact;

    //Скорость возвращения камеры при приземлении
    private float returnAmount;

    private void Start()
    {
        //Запоминаем положение камеры, которое она имела в начале игры
        //localPosition означает положение относительно Даг-чан
        _StartPosition = transform.localPosition;

        //Ну тут просто на всякий. Если в инспекторе не указан персонаж, то код выбирает родителя CharacterController
        if (_CharacterController == null)
        {
            _CharacterController =
                GetComponentInParent<CharacterController>();
        }
        //Запоминает, стояли мы на земле или нет в моменте запуска игры
        _WasGrounded = _CharacterController.isGrounded;
    }

    //LateUpdate() вызывается после обычного Update(). 
    //Мы ставим эффекты камеры сюда, чтобы оно выполнялось ПОСЛЕ методов передвижения в другом скрипте
    private void LateUpdate()
    {
        //Узнаем, стоит персонаж на земле или нет
        isGrounded = _CharacterController.isGrounded;

        //Берем текущую скорость персонажа в виде вектора
        Vector3 velocity = _CharacterController.velocity;

        //Берем скорость ходьбы по земле. Поэтому в векторе приравниваем Y к нулю.
        horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        //Обрабатываем эффекты прыжка и приземления. Передаем стоит персонаж на земле или нет, его скорость вверх/вниз.
        HandleJumpAndLanding(isGrounded, velocity.y);

        //Обрабатываем покачивание камеры при ходьбе. Передаем стоит персонаж на земле или нет, его скорость ходьбы.
        HandleWalkingBob(isGrounded, horizontalSpeed);

        //После расчетов применяем их к transform камеры.
        ApplyEffects();

        //В конце кадра запоминаем текущее состояние игрока (стоял он на земле в этом кадре или нет)
        _WasGrounded = isGrounded;
    }

    //Создаем желаемое смещение камеры во время ходьбы.
    private void HandleWalkingBob(bool isGrounded, float speed)
    {
        //Сейчас желаемое смещение = (0, 0, 0), т.к. по умолчанию наклона нет.
        Vector3 targetBob = Vector3.zero;
        //Сейчас желаемый наклон камеры = 0f, т.к. по умолчанию наклона нет.
        targetRoll = 0f;

        //Смещение должно происходить только если мы находимся на земле и у нас есть какое-то движение
        if (isGrounded && speed > 0.1f)
        {
            //Таймер покачивания камеры, которое работает через функцию синусоида.
            //Так как y = sin 0 = sin 2Pi, можно обнулять таймер на значении Pi*2, вместо того, чтобы бесконечно его увеличивать.
            _BobTimer = Mathf.Repeat(_BobTimer + Time.deltaTime * _BobFrequency, Mathf.PI * 2f);

            //Движение камеры влево/вправо
            targetBob.x = Mathf.Sin(_BobTimer) * _BobHorizontal;

            //Движение камеры вверх/вниз. Умножается на два, так как должно происходить в два раза быстрее, чем  движение влево/вправо
            targetBob.y = Mathf.Sin(_BobTimer * 2f) * _BobVertical;

            //Маленький наклон камеры влево/вправо. Минус меняет направление наклона
            targetRoll = -Mathf.Sin(_BobTimer) * _BobRoll;
        }

        //Рассчитываем, насколько быстро камера должна приближаться к нужному положению
        //Эта формула делает сглаживание, которое работает исправно вне зависимости от FPS
        smoothing = 1f - Mathf.Exp(-_BobSmoothness * Time.deltaTime);

        //Плавно двигаем текущее смещение к желаемому смещению
        _CurrentBob = Vector3.Lerp(_CurrentBob, targetBob, smoothing);

        //Плавно двигаем текущий наклон к желаемому наклону
        _CurrentRoll = Mathf.Lerp(_CurrentRoll, targetRoll, smoothing);
    }

    //Создаем желаемое смещение камеры во время прыжка и приземления
    private void HandleJumpAndLanding(bool isGrounded, float verticalVelocity)
    {
        //Если персонаж не на земеле, то он либо прыгает вверх, либо падает вниз
        if (!isGrounded)
        {
            //Пока летим, запоминаем максимальную скорость падения
            _LowestAirVelocity = Mathf.Min(_LowestAirVelocity, verticalVelocity);

            //Проверка момента самого прыжка. В прошлом кадре мы еще стояли на земле, так как _WasGrounded = True. При этом мы летим ввехр, так как verticalVelocity > 0f.
            if (_WasGrounded && verticalVelocity > 0f)
            {
                //Немного сдвигаем камеру вверх
                _KickY += _JumpKickY;

                //Немного изменяем угол камеры по X
                _KickPitch += _JumpKickPitch;
            }
        }

        //Проверяем момент приземления
        if (isGrounded && !_WasGrounded)
        {
            //Так как скорость падения отрицательная, для удобства переводим ее в положительную
            fallSpeed = -_LowestAirVelocity;

            //Превращает скорость падение в число от 0 до 1. Чем быстрее падал, тем сильнее значение impact
            impact = Mathf.InverseLerp(2f, 12f, fallSpeed);

            //Толкаем камеру вниз при приземлении
            _KickY -= _LandingKickY * impact;
            //Также немного наклоняем камеру по оси X
            _KickPitch += _LandingKickPitch * impact;

            //После приземления старая скорость падения больше не нужна, так что мы ее обнуляем
            _LowestAirVelocity = 0f;
        }

        //Постепенно возвращаем камеру назад
        //Рассчитываем скорость возвращения назад
        returnAmount = 1f - Mathf.Exp(-_KickReturnSpeed * Time.deltaTime);

        //Постепенно возвращаем вертикальное смещение камеры к нулю
        _KickY = Mathf.Lerp(_KickY, 0f, returnAmount);
        //Постепенно возвращаем поворот камеры
        _KickPitch = Mathf.Lerp(_KickPitch, 0f, returnAmount);
    }

    //Изменяем положение камеры, применяя расчитанные ранее эффекты
    private void ApplyEffects()
    {
        //Позиция камеры.
        transform.localPosition = _StartPosition + _CurrentBob + Vector3.up * _KickY;

        //Базовый поворот уже поставил Look()
        Quaternion normalRotation = transform.localRotation;

        Quaternion effectRotation = Quaternion.Euler(_KickPitch, 0f, _CurrentRoll);

        transform.localRotation = normalRotation * effectRotation;
    }
}