
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Player Health")]

    // Ссылка на компонент здоровья игрока.
    //
    // Отсюда будем получать значения HP
    // и подписываться на изменения.
    [SerializeField] private PlayerHealth _PlayerHealth;


    [Header("HP Bar")]

    // Полоска здоровья.
    // Image Type должен быть Filled.
    [SerializeField] private Image _HealthBar;


    [Header("Character Image")]

    // Изображение персонажа в HUD.
    [SerializeField] private Image _CharacterImage;


    [Header("Character Sprites")]

    // Больше 75% HP.
    [SerializeField] private Sprite _HealthySprite;

    // От 50% до 75%.
    [SerializeField] private Sprite _HurtSprite;

    // От 25% до 50%.
    [SerializeField] private Sprite _BadlyHurtSprite;

    // 25% и меньше.
    [SerializeField] private Sprite _NearDeathSprite;


    [Header("HP Text")]

    // Текст с количеством здоровья.
    [SerializeField] private TMP_Text _HealthText;


    // ------------------------------------
    // ПОДПИСКА НА СОБЫТИЕ
    // ------------------------------------

    private void OnEnable()
    {
        // Если игрок не назначен в Inspector,
        // пытаемся найти его автоматически.
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


        // ПОДПИСЫВАЕМСЯ НА СОБЫТИЕ!
        //
        // += означает, что мы добавляем метод
        // UpdateHealthUI в список подписчиков.
        //
        // Когда PlayerHealth вызовет:
        //
        // OnHealthChanged?.Invoke(current, max);
        //
        // Unity/C# автоматически вызовет
        // наш метод UpdateHealthUI(current, max).
        _PlayerHealth.OnHealthChanged += UpdateHealthUI;


        // При включении UI сразу показываем
        // текущее количество здоровья.
        //
        // Не нужно ждать первого получения урона.
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
            // -= удаляет наш метод
            // из списка подписчиков.
            //
            // Это важно, чтобы отключённый UI
            // больше не получал события.
            _PlayerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }


    // ------------------------------------
    // ОБНОВЛЕНИЕ ИНТЕРФЕЙСА
    // ------------------------------------

    private void UpdateHealthUI(
        float currentHealth,
        float maxHealth)
    {
        // Превращаем текущее HP
        // в процент от максимального.
        //
        // Например:
        // CurrentHealth = 75
        // MaxHealth = 100
        // healthPercent = 0.75
        //
        // Clamp01 ограничивает результат
        // диапазоном от 0 до 1.
        float healthPercent = Mathf.Clamp01(
            currentHealth / Mathf.Max(1f, maxHealth)
        );


        // --------------------------------
        // ТЕКСТ ЗДОРОВЬЯ
        // --------------------------------

        if (_HealthText != null)
        {
            _HealthText.text =
                Mathf.RoundToInt(currentHealth).ToString();
        }


        // --------------------------------
        // ПОЛОСКА HP
        // --------------------------------

        if (_HealthBar != null)
        {
            _HealthBar.fillAmount = healthPercent;
        }


        // --------------------------------
        // ПОРТРЕТ ПЕРСОНАЖА
        // --------------------------------

        if (_CharacterImage == null)
        {
            return;
        }


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
