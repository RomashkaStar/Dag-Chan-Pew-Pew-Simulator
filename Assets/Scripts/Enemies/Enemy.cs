
using System.Collections.Generic;
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


    [Header("Enemy Sounds")]

    // Список звуков получения урона.
    // Можно указать сколько угодно AudioClip.
    [SerializeField]
    private List<AudioClip> _HurtSounds =
        new List<AudioClip>();

    // Отдельный звук смерти противника.
    [SerializeField] private AudioClip _DeathSound;


    // Громкость звуков получения урона.
    [SerializeField, Range(0f, 1f)]
    private float _HurtVolume = 1f;

    // Громкость звука смерти.
    [SerializeField, Range(0f, 1f)]
    private float _DeathVolume = 1f;


    [Header("Walking Sounds")]

    // Список звуков шагов.
    // Можно назначить любое количество AudioClip.
    [SerializeField]
    private List<AudioClip> _WalkSounds =
        new List<AudioClip>();

    // Громкость шагов.
    [SerializeField, Range(0f, 1f)]
    private float _WalkVolume = 0.6f;

    // Отдельный AudioSource для шагов.
    // Назначим его через Inspector.
    [SerializeField] private AudioSource _WalkAudioSource;


    // Компонент, который воспроизводит звуки.
    private AudioSource _AudioSource;


    [Header("Idle Sounds")]

    // Звуки, которые враг издаёт,
    // пока он ещё не получал урон.
    [SerializeField]
    private List<AudioClip> _IdleSounds =
        new List<AudioClip>();

    // Громкость случайных звуков.
    [SerializeField, Range(0f, 1f)]
    private float _IdleVolume = 0.65f;

    // Минимальное время между звуками.
    [SerializeField, Min(0f)]
    private float _MinIdleSoundInterval = 5f;

    // Максимальное время между звуками.
    [SerializeField, Min(0f)]
    private float _MaxIdleSoundInterval = 12f;


    [Header("Fire Animation")]

    // Спрайт противника во время выстрела.
    [SerializeField] private Sprite _FireSprite;

    // Сколько секунд показываем спрайт выстрела.
    [SerializeField, Min(0.01f)]
    private float _FireSpriteDuration = 0.2f;

    // До какого момента показываем спрайт выстрела.
    private float _FireUntil;


    // Получал ли этот противник урон хотя бы один раз?
    private bool _HasBeenHit;
    public bool HasBeenHit => _HasBeenHit;

    // Время, когда можно проиграть следующий звук.
    private float _NextIdleSoundTime;


    private bool _IsAlerted;

    public void SetAlerted()
    {
        if (_IsAlerted)
        {
            return;
        }

        _IsAlerted = true;

        // Если врага ещё не ранили,
        // прекращаем обычное гавканье.
        //
        // Но не прерываем звук ранения,
        // если он уже получил пулю.
        if (!_HasBeenHit && _AudioSource != null)
        {
            _AudioSource.Stop();
        }
    }





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
        // Изначально у врага полное здоровье.
        _CurrentHealth = Mathf.Max(1f, _MaxHealth);

        // Получаем AudioSource для звуков
        // гавканья, получения урона и смерти.
        _AudioSource = GetComponent<AudioSource>();

        // Пока враг никем не атакован.
        _HasBeenHit = false;

        // Назначаем время первого случайного звука.
        _NextIdleSoundTime = Time.time + GetRandomIdleDelay();
    }


    // --------------------------
    // ПОЛУЧЕНИЕ УРОНА
    // --------------------------



    public virtual void TakeDamage(float damage)
    {
        // Мёртвому врагу и при нулевом уроне
        // ничего не делаем.
        if (_IsDead || damage <= 0f)
        {
            return;
        }


        // НОВОЕ!
        // Противник теперь знает, что в него попали.
        //
        // С этого момента случайные звуки
        // больше никогда не будут проигрываться.
        _HasBeenHit = true;


        // НОВОЕ!
        // Прерываем любой текущий звук
        // на основном AudioSource.
        //
        // Это может быть гавканье или
        // предыдущий звук получения урона.
        if (_AudioSource != null)
        {
            _AudioSource.Stop();
        }


        // Вычитаем полученный урон.
        _CurrentHealth = Mathf.Max(
            0f,
            _CurrentHealth - damage
        );


        // Проверяем, умер ли противник.
        if (_CurrentHealth <= 0f)
        {
            // Die() сам запустит звук смерти.
            Die();
            return;
        }


        // Если враг выжил, запускаем реакцию
        // окровавленных спрайтов.
        _HurtUntil = Time.time + _HurtDuration;


        // Проигрываем случайный звук получения урона.
        PlayRandomHurtSound();


        Debug.Log(
            $"{name} получил {damage} урона. " +
            $"Осталось HP: {_CurrentHealth}"
        );
    }





    private void PlayRandomHurtSound()
    {
        // Если AudioSource отсутствует,
        // воспроизводить звук нечем.
        if (_AudioSource == null)
        {
            return;
        }

        // Если список пустой,
        // проигрывать тоже нечего.
        if (_HurtSounds == null || _HurtSounds.Count == 0)
        {
            return;
        }

        // Выбираем случайный индекс массива.
        //
        // Например, если у нас 4 звука:
        //
        // Element 0
        // Element 1
        // Element 2
        // Element 3
        //
        // Random.Range(0, 4) выберет
        // случайное целое число 0, 1, 2 или 3.
        int randomIndex = Random.Range(
            0,
            _HurtSounds.Count
        );

        // Получаем случайный AudioClip.
        AudioClip selectedSound =
            _HurtSounds[randomIndex];

        // Если файл существует,
        // воспроизводим его.
        if (selectedSound != null)
        {
            _AudioSource.PlayOneShot(
                selectedSound,
                _HurtVolume
            );
        }
    }



    public void PlayFireAnimation()
    {
        // Мёртвый враг стрелять не должен.
        if (_IsDead)
        {
            return;
        }

        // Если спрайт не назначен,
        // запускать нечего.
        if (_FireSprite == null)
        {
            return;
        }

        // Запоминаем время окончания анимации.
        //
        // Например:
        // Time.time = 10
        // FireSpriteDuration = 0.2
        //
        // FireUntil = 10.2
        //
        // До 10.2 секунды показываем спрайт выстрела.
        _FireUntil =
            Time.time + _FireSpriteDuration;
    }




    // --------------------------
    // СМЕРТЬ
    // --------------------------



    protected virtual void Die()
    {
        // Противник умер.
        _IsDead = true;

        // Запоминаем момент смерти.
        _DeathStartTime = Time.time;

        // Останавливаем звук шагов, если он ещё играет.
        if (_WalkAudioSource != null)
        {
            _WalkAudioSource.Stop();
        }

        // НОВОЕ!
        // Проигрываем звук смерти.
        PlayDeathSound();


        // Останавливаем патрулирование.
        EnemyPatrol patrol = GetComponent<EnemyPatrol>();

        if (patrol != null)
        {
            patrol.enabled = false;
        }


        // Отключаем коллайдеры,
        // чтобы труп больше не получал урон.
        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }


        // Если труп должен остаться,
        // объект не уничтожаем.
        if (_LeaveCorpse)
        {
            return;
        }


        // Вычисляем продолжительность анимации смерти.
        int frameCount =
            _DeathSprites != null ? _DeathSprites.Length : 0;

        float animationDuration =
            frameCount * DeathFrameTime;


        // Не удаляем объект до завершения анимации
        // и воспроизведения звука смерти.
        float soundDuration =
            _DeathSound != null ? _DeathSound.length : 0f;

        float destroyDelay = Mathf.Max(
            animationDuration + _CorpseLifetime,
            soundDuration
        );

        Destroy(gameObject, destroyDelay);
    }




    private void PlayDeathSound()
    {
        // Если AudioSource отсутствует,
        // выходим из метода.
        if (_AudioSource == null)
        {
            return;
        }

        // Останавливаем предыдущие звуки урона,
        // чтобы они не накладывались на звук смерти.
        _AudioSource.Stop();

        // Если звук смерти назначен,
        // воспроизводим его.
        if (_DeathSound != null)
        {
            _AudioSource.PlayOneShot(
                _DeathSound,
                _DeathVolume
            );
        }
    }



    public void PlayWalkSound()
    {
        // Мёртвые противники ходить не умеют.
        if (_IsDead)
        {
            return;
        }

        // Проверяем, назначен ли AudioSource.
        if (_WalkAudioSource == null)
        {
            return;
        }

        // Если звуков нет, ничего не воспроизводим.
        if (_WalkSounds == null || _WalkSounds.Count == 0)
        {
            return;
        }

        // Выбираем случайный звук из списка.
        int randomIndex = Random.Range(
            0,
            _WalkSounds.Count
        );

        AudioClip selectedSound =
            _WalkSounds[randomIndex];

        // Проверяем, что AudioClip не пустой.
        if (selectedSound == null)
        {
            return;
        }

        // Воспроизводим звук шага.
        _WalkAudioSource.PlayOneShot(
            selectedSound,
            _WalkVolume
        );
    }



    private float GetRandomIdleDelay()
    {
        // Защищаемся от отрицательных значений.
        float minDelay = Mathf.Max(
            0f,
            _MinIdleSoundInterval
        );

        // Максимальный интервал не должен быть
        // меньше минимального.
        float maxDelay = Mathf.Max(
            minDelay,
            _MaxIdleSoundInterval
        );

        // Возвращаем случайное время ожидания.
        return Random.Range(minDelay, maxDelay);
    }


    // Проверка необходимости проиграть случайный звук.
    // Это новый Update() в классе Enemy.
    protected virtual void Update()
    {
        UpdateIdleSounds();
    }


    public bool TryPlayAttackCue(AudioClip clip, float volume)
    {
        // Если враг мёртв, атаковать нельзя.
        if (_IsDead)
        {
            return false;
        }

        // Если враг прямо сейчас получает урон,
        // не разрешаем ему начинать гавканье.
        if (Time.time < _HurtUntil)
        {
            return false;
        }

        // Если AudioSource уже воспроизводит звук,
        // не прерываем его подготовкой атаки.
        //
        // Например, это может быть звук ранения.
        if (_AudioSource != null && _AudioSource.isPlaying)
        {
            return false;
        }

        // Если AudioSource и звук существуют,
        // воспроизводим предупреждение об атаке.
        if (_AudioSource != null && clip != null)
        {
            _AudioSource.PlayOneShot(
                clip,
                Mathf.Clamp01(volume)
            );
        }

        // Сообщаем EnemyRocketLauncher,
        // что подготовку выстрела можно начинать.
        return true;
    }



    private void UpdateIdleSounds()
    {
        if (_IsDead || _HasBeenHit || _IsAlerted)
        {
            return;
        }

        // Проверяем наличие AudioSource и списка.
        if (_AudioSource == null ||
            _IdleSounds == null ||
            _IdleSounds.Count == 0)
        {
            return;
        }

        // Если время следующего звука ещё
        // не наступило, ничего не делаем.
        if (Time.time < _NextIdleSoundTime)
        {
            return;
        }

        // ВАЖНО!
        // Если источник уже что-то воспроизводит,
        // не прерываем существующий звук.
        if (_AudioSource.isPlaying)
        {
            return;
        }

        // Выбираем случайный индекс из списка.
        int randomIndex = Random.Range(
            0,
            _IdleSounds.Count
        );

        // Получаем случайный звук.
        AudioClip selectedSound =
            _IdleSounds[randomIndex];

        // Длительность воспроизведения.
        float soundDuration = 0f;

        if (selectedSound != null)
        {
            // Запускаем случайный звук.
            _AudioSource.PlayOneShot(
                selectedSound,
                _IdleVolume
            );

            soundDuration = selectedSound.length;
        }

        // Назначаем время следующего звука.
        //
        // Учитываем продолжительность самого звука,
        // чтобы пауза начиналась после его окончания.
        _NextIdleSoundTime =
            Time.time +
            soundDuration +
            GetRandomIdleDelay();
    }






    // --------------------------
    // ПОЛУЧЕНИЕ НУЖНОГО СПРАЙТА
    // --------------------------

    public Sprite GetSprite(
        EnemyViewDirection direction,
        bool isStep
    )
    {

        // Проверяем, показываем ли сейчас спрайт выстрела.
        //
        // Условия:
        // 1. Враг жив.
        // 2. Спрайт выстрела назначен.
        // 3. Время анимации ещё не закончилось.
        // 4. Враг сейчас НЕ показывает получение урона.
        //
        // Последнее условие важно:
        // спрайт ранения имеет приоритет над выстрелом.
        if (!_IsDead &&
            _FireSprite != null &&
            Time.time < _FireUntil &&
            Time.time >= _HurtUntil)
        {
            return _FireSprite;
        }


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
