
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]

    [SerializeField] private float _MaxHealth = 100f;
    [SerializeField] private float _CurrentHealth = 100f;


    [Header("Death")]

    [SerializeField]
    private DeathScreenController _DeathScreenController;


    [Header("Fall Death")]

    [SerializeField] private float _DeathHeight = -3f;


    // Флаг, показывающий, умер ли персонаж.
    private bool _IsDead;


    // ------------------------------------
    // ПУБЛИЧНЫЕ СВОЙСТВА
    // ------------------------------------

    // Другие скрипты могут УЗНАТЬ здоровье,
    // но не могут напрямую его изменить.
    //
    // Например:
    // float health = playerHealth.CurrentHealth;
    //
    // А вот так уже нельзя:
    // playerHealth.CurrentHealth = 999f;
    public float CurrentHealth => _CurrentHealth;

    public float MaxHealth => _MaxHealth;

    public bool IsDead => _IsDead;


    // ------------------------------------
    // СОБЫТИЕ ИЗМЕНЕНИЯ ЗДОРОВЬЯ
    // ------------------------------------

    // Action<float, float> означает:
    //
    // Событие передаёт ДВА числа типа float.
    //
    // Первое — текущее HP.
    // Второе — максимальное HP.
    //
    // Другие скрипты могут подписаться
    // на событие через +=.
    //
    // Сам PlayerHealth НЕ ЗНАЕТ,
    // кто именно подписался.
    public event Action<float, float> OnHealthChanged;


    private void Awake()
    {
        // Максимальное здоровье
        // не должно быть нулевым.
        _MaxHealth = Mathf.Max(1f, _MaxHealth);

        // Убеждаемся, что текущее HP
        // находится в допустимых пределах.
        _CurrentHealth = Mathf.Clamp(
            _CurrentHealth,
            0f,
            _MaxHealth
        );
    }


    private void Start()
    {
        // Если в Inspector выставили HP = 0
        // ещё до запуска игры,
        // персонаж должен умереть.
        if (_CurrentHealth <= 0f)
        {
            Die();
        }
    }


    private void Update()
    {
        if (_IsDead)
        {
            return;
        }


        // Тестовый урон по клавише H.
        // Уже через НОВУЮ Input System.
        if (Keyboard.current != null &&
            Keyboard.current.hKey.wasPressedThisFrame)
        {
            TakeDamage(20f);
        }


        // Если игрок упал ниже DeathHeight,
        // обнуляем здоровье.
        if (transform.position.y < _DeathHeight)
        {
            SetHealth(0f);
        }
    }


    // ------------------------------------
    // ПОЛУЧЕНИЕ УРОНА
    // ------------------------------------

    public void TakeDamage(float damage)
    {
        // Мёртвого игрока больше не раним.
        // Отрицательный и нулевой урон игнорируем.
        if (_IsDead || damage <= 0f)
        {
            return;
        }

        // Уменьшаем здоровье.
        SetHealth(_CurrentHealth - damage);
    }


    // ------------------------------------
    // ЛЕЧЕНИЕ
    // ------------------------------------

    public void Heal(float healAmount)
    {
        // Не лечим мёртвого персонажа.
        // Некорректное количество тоже игнорируем.
        if (_IsDead || healAmount <= 0f)
        {
            return;
        }

        // Увеличиваем здоровье.
        SetHealth(_CurrentHealth + healAmount);
    }


    // ------------------------------------
    // ЕДИНАЯ ТОЧКА ИЗМЕНЕНИЯ HP
    // ------------------------------------

    private void SetHealth(float newHealth)
    {
        if (_IsDead)
        {
            return;
        }

        // Ограничиваем значение:
        // не меньше 0 и не больше MaxHealth.
        newHealth = Mathf.Clamp(
            newHealth,
            0f,
            _MaxHealth
        );


        // Если здоровье вообще не изменилось,
        // нет необходимости уведомлять UI.
        if (Mathf.Approximately(_CurrentHealth, newHealth))
        {
            return;
        }


        // Сохраняем новое значение HP.
        _CurrentHealth = newHealth;


        // ВОТ САМОЕ ГЛАВНОЕ!
        //
        // Вызываем событие изменения здоровья.
        //
        // ?. означает:
        // "Вызови событие, если на него
        // хотя бы кто-нибудь подписался".
        //
        // PlayerHealthUI получит новые значения
        // и самостоятельно обновит интерфейс.
        OnHealthChanged?.Invoke(
            _CurrentHealth,
            _MaxHealth
        );


        // После уведомления интерфейса
        // проверяем, не умер ли игрок.
        if (_CurrentHealth <= 0f)
        {
            Die();
        }
    }


    // ------------------------------------
    // СМЕРТЬ
    // ------------------------------------

    private void Die()
    {
        if (_IsDead)
        {
            return;
        }

        _IsDead = true;

        Debug.Log(
            "ИГРОК УМЕР! HP = " + _CurrentHealth,
            this
        );


        // Отключаем передвижение и поворот камеры.
        GO_DagChan movement =
            GetComponent<GO_DagChan>();

        if (movement != null)
        {
            movement.enabled = false;
        }


        // Отключаем стрельбу.
        WeaponShooter shooter =
            GetComponentInChildren<WeaponShooter>(true);

        if (shooter != null)
        {
            shooter.enabled = false;
        }


        // Запускаем существующую анимацию смерти.
        //
        // Сам DeathScreenController отвечает
        // за заморозку времени, звуки смерти,
        // анимацию и параллакс.
        if (_DeathScreenController != null)
        {
            _DeathScreenController.PlayDeathSequence();
        }
        else
        {
            Debug.LogError(
                "НЕ НАЗНАЧЕН DEATH SCREEN CONTROLLER!",
                this
            );
        }
    }
}
