
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DoomEnemySprite : MonoBehaviour
{
    [Header("References")]

    // Ссылка на конкретного противника.
    //
    // Здесь может быть DachshundEnemy,
    // DemonEnemy или любой другой наследник Enemy.
    [SerializeField] private Enemy _Enemy;

    // Камера игрока.
    [SerializeField] private Transform _PlayerCamera;


    // Компонент для отображения картинки.
    private SpriteRenderer _SpriteRenderer;

    // Позиция противника в предыдущем кадре.
    private Vector3 _PreviousPosition;

    // Таймер анимации ходьбы.
    private float _WalkTimer;

    // Был ли показан кадр шага в предыдущем обновлении?
    private bool _PreviousStepFrame;


    private void Awake()
    {
        _SpriteRenderer = GetComponent<SpriteRenderer>();

        // Если врага не назначили вручную,
        // находим компонент Enemy у родителя.
        if (_Enemy == null)
        {
            _Enemy = GetComponentInParent<Enemy>();
        }

        // Автоматически получаем Main Camera.
        if (_PlayerCamera == null && Camera.main != null)
        {
            _PlayerCamera = Camera.main.transform;
        }
    }


    private void Start()
    {
        if (_Enemy != null)
        {
            // Запоминаем начальную позицию врага.
            _PreviousPosition = _Enemy.transform.position;
        }
    }



    private void LateUpdate()
    {
        if (_Enemy == null || _PlayerCamera == null)
        {
            return;
        }

        // Спрайт по-прежнему поворачивается к игроку.
        FaceCamera();


        // НОВОЕ!
        // Если противник умер,
        // полностью переключаемся на анимацию смерти.
        if (_Enemy.IsDead)
        {
            UpdateDeathAnimation();

            // return не позволяет выполнять
            // анимацию ходьбы мёртвого врага.
            return;
        }


        // Если противник жив,
        // вся предыдущая логика остаётся прежней.

        bool isMoving = CheckMovement();

        bool isStepFrame =
            UpdateWalkingAnimation(isMoving);


        // Если враг движется,
        // сейчас показывается кадр шага,
        // а в прошлом обновлении кадра шага не было...
        if (isMoving && isStepFrame && !_PreviousStepFrame)
        {
            // Воспроизводим звук одного шага.
            _Enemy.PlayWalkSound();
        }

        // Запоминаем текущий кадр.
        //
        // Если враг остановился, записываем false,
        // чтобы после возобновления ходьбы
        // шаги снова корректно воспроизводились.
        _PreviousStepFrame = isMoving && isStepFrame;


        EnemyViewDirection direction =
            GetViewDirection();

        Sprite targetSprite =
            _Enemy.GetSprite(direction, isStepFrame);

        if (targetSprite != null &&
            _SpriteRenderer.sprite != targetSprite)
        {
            _SpriteRenderer.sprite = targetSprite;
        }
    }


    private void UpdateDeathAnimation()
    {
        // Запрашиваем массив кадров смерти
        // у конкретного противника.
        Sprite[] deathSprites = _Enemy.DeathSprites;


        // Если кадров смерти нет,
        // оставляем окровавленную стоячую позу.
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


        // Вычисляем, сколько секунд прошло
        // после смерти противника.
        //
        // Например:
        // Time.time = 15.5
        // DeathStartTime = 15.0
        //
        // elapsed = 0.5 секунды.
        float elapsed =
            Mathf.Max(0f, Time.time - _Enemy.DeathStartTime);


        // Вычисляем номер нужного кадра.
        //
        // Например:
        // elapsed = 0.5
        // DeathFrameTime = 0.1
        //
        // 0.5 / 0.1 = 5
        //
        // Значит показываем кадр с индексом 5.
        int frameIndex = Mathf.FloorToInt(
            elapsed / _Enemy.DeathFrameTime
        );


        // Не позволяем индексу выйти за массив.
        //
        // Если у нас 6 кадров,
        // допустимые индексы: 0, 1, 2, 3, 4, 5.
        //
        // Даже если прошло 100 секунд,
        // будем продолжать показывать кадр 5.
        frameIndex = Mathf.Clamp(
            frameIndex,
            0,
            deathSprites.Length - 1
        );


        // Получаем нужный кадр смерти.
        Sprite targetSprite =
            deathSprites[frameIndex];


        // Показываем его на противнике.
        if (targetSprite != null &&
            _SpriteRenderer.sprite != targetSprite)
        {
            _SpriteRenderer.sprite = targetSprite;
        }
    }




    private void FaceCamera()
    {
        // Вектор от спрайта до камеры.
        Vector3 direction =
            _PlayerCamera.position - transform.position;

        // Игнорируем разницу по высоте,
        // чтобы спрайт не заваливался назад или вперёд.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // Разворачиваем плоскость спрайта к игроку.
        transform.rotation =
            Quaternion.LookRotation(direction, Vector3.up);
    }


    private bool CheckMovement()
    {
        // Узнаём, насколько враг переместился
        // относительно предыдущего кадра.
        Vector3 movement =
            _Enemy.transform.position - _PreviousPosition;

        // Игнорируем вертикальное движение.
        movement.y = 0f;

        // Переводим пройденное расстояние
        // в скорость за секунду.
        float speed =
            movement.magnitude /
            Mathf.Max(Time.deltaTime, 0.0001f);

        // Сохраняем позицию для следующего кадра.
        _PreviousPosition = _Enemy.transform.position;

        // Запрашиваем у конкретного врага
        // минимальную скорость для анимации.
        return speed > _Enemy.MovementThreshold;
    }


    private bool UpdateWalkingAnimation(bool isMoving)
    {
        // Узнаём у противника,
        // сколько должен длиться один кадр.
        float frameTime = _Enemy.FrameTime;


        if (isMoving)
        {
            // Увеличиваем таймер.
            //
            // Один полный цикл содержит два кадра.
            _WalkTimer = Mathf.Repeat(
                _WalkTimer + Time.deltaTime,
                frameTime * 2f
            );
        }
        else
        {
            // Если враг остановился,
            // возвращаемся к первому кадру.
            _WalkTimer = 0f;
        }


        // Если сейчас вторая половина цикла,
        // значит необходимо показать кадр шага.
        return isMoving && _WalkTimer >= frameTime;
    }


    private EnemyViewDirection GetViewDirection()
    {
        // Вычисляем направление от врага до игрока.
        Vector3 toPlayer =
            _PlayerCamera.position - _Enemy.transform.position;

        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return EnemyViewDirection.Front;
        }

        toPlayer.Normalize();


        // Находим, насколько игрок находится
        // впереди или позади врага.
        float forwardDot =
            Vector3.Dot(_Enemy.transform.forward, toPlayer);

        // Находим, насколько игрок находится
        // справа или слева от врага.
        float rightDot =
            Vector3.Dot(_Enemy.transform.right, toPlayer);


        // Определяем, спереди или сзади игрок.
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

        // Иначе игрок находится сбоку.
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
