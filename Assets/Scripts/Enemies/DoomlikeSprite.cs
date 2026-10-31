
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DoomEnemySprite : MonoBehaviour
{
    [Header("References")]

    // Ссылка на основную логику противника.
    // Подойдёт любой класс, наследующийся от Enemy.
    // Отсюда получаем спрайты, звуки и состояние смерти.
    [SerializeField] private Enemy _Enemy;

    [SerializeField] private Transform _PlayerCamera;


    private SpriteRenderer _SpriteRenderer;

    // Предыдущая позиция врага нужна,
    // чтобы определять движение по изменению координат.
    private Vector3 _PreviousPosition;

    // Таймер переключения кадров ходьбы.
    private float _WalkTimer;

    // Запоминаем предыдущий кадр шага,
    // чтобы звук воспроизводился один раз
    // при переходе на него, а не каждый кадр игры.
    private bool _PreviousStepFrame;


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Awake()
    {
        _SpriteRenderer = GetComponent<SpriteRenderer>();

        // Спрайт обычно находится на дочернем объекте,
        // а Enemy — на родительском.
        if (_Enemy == null)
        {
            _Enemy = GetComponentInParent<Enemy>();
        }

        if (_PlayerCamera == null && Camera.main != null)
        {
            _PlayerCamera = Camera.main.transform;
        }
    }


    private void Start()
    {
        if (_Enemy != null)
        {
            // Запоминаем стартовую позицию,
            // относительно которой определим первое движение.
            _PreviousPosition = _Enemy.transform.position;
        }
    }


    // ------------------------------------
    // ОБНОВЛЕНИЕ СПРАЙТА
    // ------------------------------------

    private void LateUpdate()
    {
        if (_Enemy == null || _PlayerCamera == null)
        {
            return;
        }

        // Billboard: спрайт врага должен
        // постоянно смотреть в сторону камеры.
        FaceCamera();


        // Если противник умер, больше не обновляем
        // обычные кадры ходьбы и направления.
        // Теперь показываем только анимацию смерти.
        if (_Enemy.IsDead)
        {
            UpdateDeathAnimation();
            return;
        }


        // Проверяем, перемещается ли враг,
        // и определяем текущий кадр ходьбы.
        bool isMoving = CheckMovement();

        bool isStepFrame =
            UpdateWalkingAnimation(isMoving);


        // Звук шага срабатывает только в момент
        // перехода с обычного кадра на кадр шага.
        //
        // Без проверки _PreviousStepFrame звук
        // воспроизводился бы каждый кадр,
        // пока показывается спрайт шага.
        if (isMoving && isStepFrame && !_PreviousStepFrame)
        {
            _Enemy.PlayWalkSound();
        }

        _PreviousStepFrame = isMoving && isStepFrame;


        // Определяем, какой стороной противник
        // повернут к игроку: Front, Back, Left, Right.
        EnemyViewDirection direction =
            GetViewDirection();


        // Enemy сам хранит наборы спрайтов.
        // Передаём ему направление и кадр ходьбы,
        // а он возвращает подходящую картинку.
        Sprite targetSprite =
            _Enemy.GetSprite(direction, isStepFrame);


        // Меняем спрайт только при необходимости.
        if (targetSprite != null &&
            _SpriteRenderer.sprite != targetSprite)
        {
            _SpriteRenderer.sprite = targetSprite;
        }
    }


    // ------------------------------------
    // АНИМАЦИЯ СМЕРТИ
    // ------------------------------------

    private void UpdateDeathAnimation()
    {
        Sprite[] deathSprites = _Enemy.DeathSprites;


        // Если анимации смерти нет,
        // оставляем обычный спрайт противника.
        if (deathSprites == null || deathSprites.Length == 0)
        {
            Sprite fallbackSprite = _Enemy.GetSprite(
                GetViewDirection(),
                false
            );

            if (fallbackSprite != null)
            {
                _SpriteRenderer.sprite = fallbackSprite;
            }

            return;
        }


        // Вычисляем, сколько времени прошло
        // с момента смерти противника.
        //
        // DeathStartTime задаёт Enemy,
        // поэтому DoomEnemySprite не нужно
        // самостоятельно запускать таймер смерти.
        float elapsed =
            Mathf.Max(0f, Time.time - _Enemy.DeathStartTime);


        // Рассчитываем номер кадра по времени.
        //
        // Например:
        // прошло 0.35 секунды,
        // длительность кадра 0.1 секунды.
        //
        // 0.35 / 0.1 = 3.5
        // FloorToInt(3.5) = 3.
        //
        // Значит, показываем кадр с индексом 3.
        int frameIndex = Mathf.FloorToInt(
            elapsed / _Enemy.DeathFrameTime
        );


        // После завершения анимации оставляем
        // её последний кадр (труп врага).
        //
        // Clamp не позволит индексу выйти
        // за пределы массива спрайтов.
        frameIndex = Mathf.Clamp(
            frameIndex,
            0,
            deathSprites.Length - 1
        );


        Sprite targetSprite =
            deathSprites[frameIndex];


        if (targetSprite != null &&
            _SpriteRenderer.sprite != targetSprite)
        {
            _SpriteRenderer.sprite = targetSprite;
        }
    }


    // ------------------------------------
    // BILLBOARD
    // ------------------------------------

    private void FaceCamera()
    {
        Vector3 direction =
            _PlayerCamera.position - transform.position;


        // Игнорируем разницу по высоте.
        // Спрайт поворачивается только вокруг оси Y,
        // чтобы не наклоняться при взгляде сверху/снизу.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }


        // Поворачиваем плоскость спрайта
        // в направлении камеры.
        //
        // Это не меняет направление самого Enemy,
        // если DoomEnemySprite находится
        // на отдельном дочернем объекте.
        transform.rotation =
            Quaternion.LookRotation(direction, Vector3.up);
    }


    // ------------------------------------
    // ОПРЕДЕЛЕНИЕ ДВИЖЕНИЯ
    // ------------------------------------

    private bool CheckMovement()
    {
        // Разница между текущей и прошлой
        // позицией показывает, насколько враг
        // переместился за последний кадр.
        Vector3 movement =
            _Enemy.transform.position - _PreviousPosition;


        // Учитываем только движение по земле.
        movement.y = 0f;


        // Переводим пройденное за кадр расстояние
        // в скорость (единиц Unity в секунду).
        //
        // Mathf.Max защищает от деления
        // на слишком маленький deltaTime.
        float speed =
            movement.magnitude /
            Mathf.Max(Time.deltaTime, 0.0001f);


        // Сохраняем позицию для следующего кадра.
        _PreviousPosition = _Enemy.transform.position;


        // MovementThreshold позволяет игнорировать
        // совсем небольшие перемещения и дрожание.
        return speed > _Enemy.MovementThreshold;
    }


    // ------------------------------------
    // АНИМАЦИЯ ХОДЬБЫ
    // ------------------------------------

    private bool UpdateWalkingAnimation(bool isMoving)
    {
        float frameTime = _Enemy.FrameTime;


        if (isMoving)
        {
            // Mathf.Repeat зацикливает таймер
            // в диапазоне от 0 до frameTime * 2.
            //
            // Первая половина цикла — обычный кадр.
            // Вторая половина — кадр шага.
            //
            // Получается простая двухкадровая
            // анимация ходьбы.
            _WalkTimer = Mathf.Repeat(
                _WalkTimer + Time.deltaTime,
                frameTime * 2f
            );
        }
        else
        {
            // Когда противник остановился,
            // сбрасываем анимацию в обычную позу.
            _WalkTimer = 0f;
        }


        // true означает, что сейчас нужно
        // показывать второй кадр ходьбы.
        return isMoving && _WalkTimer >= frameTime;
    }


    // ------------------------------------
    // ВЫБОР СТОРОНЫ ПЕРСОНАЖА
    // ------------------------------------

    private EnemyViewDirection GetViewDirection()
    {
        // Определяем направление от противника
        // к камере игрока в горизонтальной плоскости.
        Vector3 toPlayer =
            _PlayerCamera.position - _Enemy.transform.position;

        toPlayer.y = 0f;


        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return EnemyViewDirection.Front;
        }

        toPlayer.Normalize();


        // Vector3.Dot — скалярное произведение.
        //
        // Для нормализованных направлений:
        //  1 = направления совпадают;
        //  0 = перпендикулярны;
        // -1 = противоположны.
        //
        // forwardDot показывает, находится ли
        // камера спереди или сзади противника.
        float forwardDot =
            Vector3.Dot(_Enemy.transform.forward, toPlayer);


        // rightDot показывает, находится ли
        // камера справа или слева от противника.
        float rightDot =
            Vector3.Dot(_Enemy.transform.right, toPlayer);


        // Сравниваем абсолютные значения,
        // чтобы определить, с какой стороны
        // камера находится сильнее:
        // спереди/сзади или справа/слева.
        if (Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot))
        {
            if (forwardDot >= 0f)
            {
                return EnemyViewDirection.Front;
            }
            else
            {
                return EnemyViewDirection.Back;
            }
        }


        if (rightDot >= 0f)
        {
            return EnemyViewDirection.Right;
        }
        else
        {
            return EnemyViewDirection.Left;
        }
    }
}
