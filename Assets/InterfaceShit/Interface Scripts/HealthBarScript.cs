using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float _MaxHealth = 100f;
    [SerializeField] private float _CurrentHealth = 100f;


    [Header("HP Bar")]
    // Картинка самой полоски здоровья.
    // У неё Image Type должен быть Filled.
    [SerializeField] private Image _HealthBar;


    [Header("Character Image")]
    // Это ДРУГОЙ Image.
    // Здесь будет меняться картинка персонажа.
    [SerializeField] private Image _CharacterImage;

    // HP больше 75%.
    [SerializeField] private Sprite _HealthySprite;

    // HP от 50% до 75%.
    [SerializeField] private Sprite _HurtSprite;

    // HP от 25% до 50%.
    [SerializeField] private Sprite _BadlyHurtSprite;

    // HP 25% и ниже.
    [SerializeField] private Sprite _NearDeathSprite;


    [Header("HP Text")]
    // Тут ссылка на TMP Text, отображающий наше HP
    [SerializeField] private TMP_Text _HealthText;


    //Процент от общего HP для заполнения полоски HP
    private float healthPercent;


    private void Start()
    {
        // При запуске сразу отображаем
        // правильное количество HP и правильную картинку.
        UpdateHealthUI();
    }

    private void Update()
{
    if (Input.GetKeyDown(KeyCode.H))
    {
        TakeDamage(20f);
    }
}


    public void TakeDamage(float damage)
    {
        // Отнимаем здоровье.
        _CurrentHealth -= damage;

        // Не позволяем HP стать меньше 0
        // или больше максимального значения.
        _CurrentHealth = Mathf.Clamp(
            _CurrentHealth,
            0f,
            _MaxHealth
        );

        // После изменения HP обновляем весь интерфейс.
        UpdateHealthUI();
    }


    public void Heal(float healAmount)
    {
        // Прибавляем здоровье.
        _CurrentHealth += healAmount;

        // Ограничиваем значение.
        _CurrentHealth = Mathf.Clamp(
            _CurrentHealth,
            0f,
            _MaxHealth
        );

        // Обновляем интерфейс.
        UpdateHealthUI();
    }


    private void UpdateHealthUI()
    {
        // Получаем процент здоровья от 0 до 1.
        healthPercent =
            _CurrentHealth / _MaxHealth;


        // Эта штука обновляет надпись со здоровьем
        _HealthText.text = Mathf.RoundToInt(_CurrentHealth).ToString();


        // -------------------------
        // ОБНОВЛЯЕМ HP BAR
        // -------------------------

        // fillAmount принимает число от 0 до 1.
        //
        // Поэтому healthPercent подходит идеально.
        _HealthBar.fillAmount =
            healthPercent;


        // -------------------------
        // ОБНОВЛЯЕМ КАРТИНКУ
        // -------------------------

        if (healthPercent > 0.75f)
        {
            // Больше 75% HP.
            _CharacterImage.sprite =
                _HealthySprite;
        }
        else if (healthPercent > 0.50f)
        {
            // От 50% до 75%.
            _CharacterImage.sprite =
                _HurtSprite;
        }
        else if (healthPercent > 0.25f)
        {
            // От 25% до 50%.
            _CharacterImage.sprite =
                _BadlyHurtSprite;
        }
        else
        {
            // 25% и меньше.
            _CharacterImage.sprite =
                _NearDeathSprite;
        }
    }
}