
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Player Health")]

    // Ссылка на компонент здоровья игрока.
    // UI не изменяет здоровье, а только получает
    // его значения и следит за изменениями.
    [SerializeField] private PlayerHealth _PlayerHealth;


    [Header("HP Bar")]

    // Полоска здоровья. Image Type = Filled.
    [SerializeField] private Image _HealthBar;


    [Header("Character Image")]

    [SerializeField] private Image _CharacterImage;


    [Header("Character Sprites")]

    [SerializeField] private Sprite _HealthySprite;
    [SerializeField] private Sprite _HurtSprite;
    [SerializeField] private Sprite _BadlyHurtSprite;
    [SerializeField] private Sprite _NearDeathSprite;


    [Header("HP Text")]

    [SerializeField] private TMP_Text _HealthText;


    // ------------------------------------
    // ПОДПИСКА НА СОБЫТИЕ
    // ------------------------------------

    private void OnEnable()
    {
        // OnEnable вызывается при включении компонента
        // или активации его GameObject.
        //
        // Если PlayerHealth не назначен в Inspector,
        // пытаемся найти его в сцене.
        if (_PlayerHealth == null)
        {
            _PlayerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }

        if (_PlayerHealth == null)
        {
            Debug.LogError(
                "PlayerHealthUI: не найден PlayerHealth!",
                this
            );

            return;
        }


        // Подписываем метод UpdateHealthUI на событие
        // OnHealthChanged из PlayerHealth.
        //
        // Когда здоровье изменится, PlayerHealth вызовет:
        // OnHealthChanged?.Invoke(currentHealth, maxHealth);
        //
        // И автоматически выполнится наш UpdateHealthUI
        // с переданными значениями здоровья.
        //
        // Благодаря этому не нужно постоянно
        // проверять HP в Update().
        _PlayerHealth.OnHealthChanged += UpdateHealthUI;


        // Событие вызывается только при изменении HP.
        // Поэтому при включении интерфейса обновляем
        // его вручную, чтобы сразу показать актуальное HP.
        UpdateHealthUI(
            _PlayerHealth.CurrentHealth,
            _PlayerHealth.MaxHealth
        );
    }


    // ------------------------------------
    // ОТПИСКА ОТ СОБЫТИЯ
    // ------------------------------------

    private void OnDisable()
    {
        if (_PlayerHealth != null)
        {
            // Отписываемся от события при отключении UI.
            //
            // += добавляет метод в список подписчиков,
            // -= убирает его из этого списка.
            //
            // Так отключённый интерфейс не будет
            // получать события, а при повторном включении
            // не возникнет лишних подписок.
            _PlayerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }


    // ------------------------------------
    // ОБНОВЛЕНИЕ ИНТЕРФЕЙСА
    // ------------------------------------

    // Этот метод вызывается при изменении здоровья
    // через событие, а также при включении UI.
    //
    // Получает два значения:
    // currentHealth — текущее здоровье;
    // maxHealth — максимальное здоровье.
    private void UpdateHealthUI(
        float currentHealth,
        float maxHealth)
    {
        // Переводим HP в число от 0 до 1.
        //
        // Например: 75 / 100 = 0.75.
        //
        // Mathf.Max(1f, maxHealth) защищает
        // от деления на ноль.
        // Clamp01 ограничивает результат от 0 до 1.
        float healthPercent = Mathf.Clamp01(
            currentHealth / Mathf.Max(1f, maxHealth)
        );


        // --------------------------------
        // ТЕКСТ ЗДОРОВЬЯ
        // --------------------------------

        if (_HealthText != null)
        {
            // Округляем HP до целого числа,
            // затем преобразуем его в строку для TMP.
            _HealthText.text =
                Mathf.RoundToInt(currentHealth).ToString();
        }


        // --------------------------------
        // ПОЛОСКА HP
        // --------------------------------

        if (_HealthBar != null)
        {
            // fillAmount работает с диапазоном 0–1.
            // Например, 0.5 означает заполнение на 50%.
            _HealthBar.fillAmount = healthPercent;
        }


        // --------------------------------
        // ПОРТРЕТ ПЕРСОНАЖА
        // --------------------------------

        if (_CharacterImage == null)
        {
            return;
        }


        // Выбираем состояние персонажа
        // в зависимости от процента оставшегося HP.
        //
        // > 75% — здоров.
        // > 50% — ранен.
        // > 25% — тяжело ранен.
        // <= 25% — при смерти.
        if (healthPercent > 0.75f)
        {
            _CharacterImage.sprite = _HealthySprite;
        }
        else if (healthPercent > 0.50f)
        {
            _CharacterImage.sprite = _HurtSprite;
        }
        else if (healthPercent > 0.25f)
        {
            _CharacterImage.sprite = _BadlyHurtSprite;
        }
        else
        {
            _CharacterImage.sprite = _NearDeathSprite;
        }
    }
}
