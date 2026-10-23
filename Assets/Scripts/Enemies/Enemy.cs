
using UnityEngine;

public enum EnemyViewDirection
{
    Front,
    Back,
    Left,
    Right
}

// Enemy теперь реализует интерфейс IDamageable.
// То есть обязан содержать метод TakeDamage(float).
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health")]

    // Максимальное здоровье противника.
    [SerializeField, Min(1f)]
    private float _MaxHealth = 100f;

    // Сколько секунд показываем окровавленный спрайт.
    [SerializeField, Min(0f)]
    private float _HurtDuration = 0.4f;

    // Через сколько секунд после смерти удаляем врага.
    [SerializeField, Min(0f)]
    private float _DeathDelay = 0.4f;


    [Header("Front Sprites")]

    [SerializeField] protected Sprite _FrontSprite;
    [SerializeField] protected Sprite _FrontStepSprite;

    [Header("Back Sprites")]

    [SerializeField] protected Sprite _BackSprite;
    [SerializeField] protected Sprite _BackStepSprite;

    [Header("Left Sprites")]

    [SerializeField] protected Sprite _LeftSprite;
    [SerializeField] protected Sprite _LeftStepSprite;

    [Header("Right Sprites")]

    [SerializeField] protected Sprite _RightSprite;
    [SerializeField] protected Sprite _RightStepSprite;


    // НОВОЕ: ОКРОВАВЛЕННЫЕ СПРАЙТЫ

    [Header("Front Hurt Sprites")]

    [SerializeField] protected Sprite _FrontHurtSprite;
    [SerializeField] protected Sprite _FrontStepHurtSprite;

    [Header("Back Hurt Sprites")]

    [SerializeField] protected Sprite _BackHurtSprite;
    [SerializeField] protected Sprite _BackStepHurtSprite;

    [Header("Left Hurt Sprites")]

    [SerializeField] protected Sprite _LeftHurtSprite;
    [SerializeField] protected Sprite _LeftStepHurtSprite;

    [Header("Right Hurt Sprites")]

    [SerializeField] protected Sprite _RightHurtSprite;
    [SerializeField] protected Sprite _RightStepHurtSprite;


    [Header("Walking Animation")]

    [SerializeField] protected float _FrameTime = 0.15f;
    [SerializeField] protected float _MovementThreshold = 0.05f;


    [Header("Death Animation")]

    // Массив всех кадров смерти.
    // Количество кадров задаётся в Inspector.
    [SerializeField] private Sprite[] _DeathSprites;

    // Сколько секунд показывается каждый кадр.
    [SerializeField, Min(0.01f)]
    private float _DeathFrameTime = 0.2f;

    // Оставить труп после завершения анимации?
    [SerializeField] private bool _LeaveCorpse = true;

    // Если труп не оставляем, сколько времени
    // он ещё пролежит после окончания анимации.
    [SerializeField, Min(0f)]
    private float _CorpseLifetime = 2f;


    // Момент времени, когда враг умер.
    private float _DeathStartTime;


    // Разрешаем DoomEnemySprite читать эти данные.
    public Sprite[] DeathSprites => _DeathSprites;

    public float DeathFrameTime =>
        Mathf.Max(0.01f, _DeathFrameTime);

    public bool IsDead => _IsDead;

    public float DeathStartTime => _DeathStartTime;





    // Текущее здоровье врага.
    private float _CurrentHealth;

    // Время, до которого показываем повреждения.
    private float _HurtUntil;

    // Умер ли противник.
    private bool _IsDead;


    // Свойства для других скриптов.
    public float CurrentHealth => _CurrentHealth;
    public float MaxHealth => _MaxHealth;

    public float FrameTime => Mathf.Max(0.01f, _FrameTime);
    public float MovementThreshold => Mathf.Max(0f, _MovementThreshold);


    protected virtual void Awake()
    {
        // При появлении врага выдаём ему полное здоровье.
        _CurrentHealth = Mathf.Max(1f, _MaxHealth);
    }


    // --------------------------
    // ПОЛУЧЕНИЕ УРОНА
    // --------------------------

    public virtual void TakeDamage(float damage)
    {
        // Мёртвого врага больше не повреждаем.
        // Отрицательный и нулевой урон игнорируем.
        if (_IsDead || damage <= 0f)
        {
            return;
        }

        // Отнимаем здоровье.
        _CurrentHealth = Mathf.Max(
            0f,
            _CurrentHealth - damage
        );

        // Запоминаем время, до которого враг
        // должен показывать окровавленный спрайт.
        //
        // Например:
        // Time.time = 10
        // HurtDuration = 0.2
        //
        // HurtUntil = 10.2
        //
        // До времени 10.2 враг будет выглядеть раненым.
        _HurtUntil = Time.time + _HurtDuration;

        Debug.Log(
            $"{name} получил {damage} урона. " +
            $"Осталось HP: {_CurrentHealth}"
        );

        // Если здоровье закончилось — умираем.
        if (_CurrentHealth <= 0f)
        {
            Die();
        }
    }


    // --------------------------
    // СМЕРТЬ
    // --------------------------


    protected virtual void Die()
    {
        // Противник теперь официально мёртв.
        _IsDead = true;

        // Запоминаем время смерти.
        // Оно понадобится для вычисления
        // текущего кадра анимации.
        _DeathStartTime = Time.time;


        // Останавливаем движение врага.
        EnemyPatrol patrol = GetComponent<EnemyPatrol>();

        if (patrol != null)
        {
            patrol.enabled = false;
        }


        // Отключаем физические коллайдеры врага.
        // Теперь по его трупу нельзя наносить
        // новые попадания через Physics.Raycast.
        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }


        // Если труп должен остаться навсегда,
        // ничего больше не делаем.
        //
        // DoomEnemySprite самостоятельно
        // проиграет анимацию до последнего кадра.
        if (_LeaveCorpse)
        {
            return;
        }


        // Если труп оставлять не нужно,
        // вычисляем полную длительность анимации.
        //
        // Например:
        // 6 кадров * 0.12 секунды = 0.72 секунды.
        int frameCount =
            _DeathSprites != null ? _DeathSprites.Length : 0;

        float animationDuration =
            frameCount * DeathFrameTime;


        // Удаляем противника только после
        // завершения всей анимации
        // и дополнительного времени лежания трупа.
        Destroy(
            gameObject,
            animationDuration + _CorpseLifetime
        );
    }



    // --------------------------
    // ПОЛУЧЕНИЕ НУЖНОГО СПРАЙТА
    // --------------------------

    public Sprite GetSprite(
        EnemyViewDirection direction,
        bool isStep
    )
    {
        switch (direction)
        {
            case EnemyViewDirection.Front:

                return ChooseSprite(
                    _FrontSprite,
                    _FrontStepSprite,
                    _FrontHurtSprite,
                    _FrontStepHurtSprite,
                    isStep
                );


            case EnemyViewDirection.Back:

                return ChooseSprite(
                    _BackSprite,
                    _BackStepSprite,
                    _BackHurtSprite,
                    _BackStepHurtSprite,
                    isStep
                );


            case EnemyViewDirection.Left:

                return ChooseSprite(
                    _LeftSprite,
                    _LeftStepSprite,
                    _LeftHurtSprite,
                    _LeftStepHurtSprite,
                    isStep
                );


            case EnemyViewDirection.Right:

                return ChooseSprite(
                    _RightSprite,
                    _RightStepSprite,
                    _RightHurtSprite,
                    _RightStepHurtSprite,
                    isStep
                );
        }

        return null;
    }


    // --------------------------
    // ВЫБОР ОБЫЧНОГО ИЛИ РАНЕНОГО СПРАЙТА
    // --------------------------

    private Sprite ChooseSprite(
        Sprite idleSprite,
        Sprite stepSprite,
        Sprite hurtIdleSprite,
        Sprite hurtStepSprite,
        bool isStep
    )
    {
        // Вычисляем, нужно ли сейчас показывать кровь.
        //
        // Если враг умер — оставляем окровавленный
        // спрайт до самого удаления объекта.
        //
        // Если ещё жив — проверяем время.
        bool isHurt = _IsDead || Time.time < _HurtUntil;


        if (isHurt)
        {
            // Если сейчас кадр шага,
            // пытаемся показать окровавленный кадр шага.
            if (isStep && hurtStepSprite != null)
            {
                return hurtStepSprite;
            }

            // Иначе показываем обычную
            // окровавленную позу.
            if (hurtIdleSprite != null)
            {
                return hurtIdleSprite;
            }
        }


        // Если повреждения больше не показываем,
        // возвращаем обычную анимацию ходьбы.
        if (isStep && stepSprite != null)
        {
            return stepSprite;
        }

        // Обычная стоячая поза.
        return idleSprite;
    }
}
