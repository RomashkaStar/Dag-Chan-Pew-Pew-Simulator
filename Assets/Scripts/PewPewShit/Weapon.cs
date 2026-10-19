using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("Sprites")]

    // Обычный спрайт оружия.
    [SerializeField] protected Sprite _IdleSprite;

    // Спрайты анимации выстрела.
    [SerializeField] protected Sprite[] _FireSprites;

    // Продолжительность одного кадра анимации.
    [SerializeField] protected float _FrameTime = 0.05f;


    [Header("Audio")]

    // Звуковой файл выстрела.
    [SerializeField] protected AudioClip _FireSound;

    [Header ("Ammo")]

    // Сколько патронов сейчас у этого оружия
    [SerializeField, Min(0)] private int _AmmoCount = 12;

    // Спрайт типа боеприпаса
    [SerializeField] private Sprite _AmmoSprite;


    // Позволяем другим скриптам получать данные выбранного оружия.
    public Sprite IdleSprite => _IdleSprite;

    public Sprite[] FireSprites => _FireSprites;

    public float FrameTime => _FrameTime;

    public AudioClip FireSound => _FireSound;

    public int AmmoCount => _AmmoCount;

    public Sprite AmmoSprite => _AmmoSprite;

    // Максимум патронов. 
    // abstract значит, что Weapon сам не задает  максимум.
    // Каждый конкретный тип оружия обязан предоставить его.
    public abstract int MaxAmmo { get; }


    protected virtual void Awake() {
        // При запуске проверяем, что текущее кол-во патронов укладывается в рамки дозволенного
        _AmmoCount = Mathf.Clamp(_AmmoCount, 0 , MaxAmmo);
    }


    // Пробуем потратить один патрон. Если патрон потрачен - можно стрелять.
    // Если патрон не тратится, то стрелять нельзя, патронов нет.
    public bool TrySpendAmmo() {
        if (_AmmoCount <= 0) {
            return false;
        }

        // Тратим патрон
        _AmmoCount--;

        // Сообщаем, что патрон потрачен, выстрел разрешен
        return true;
    }


    public void AddAmmo(int amount) {
        if (amount <= 0) {
            return;
        }

        _AmmoCount = Mathf.Clamp(_AmmoCount + amount, 0, MaxAmmo);
    }
}