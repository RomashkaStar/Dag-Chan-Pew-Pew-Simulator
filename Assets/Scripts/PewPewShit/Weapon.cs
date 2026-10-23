
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("Sprites")]

    // Обычный спрайт оружия.
    [SerializeField] protected Sprite _IdleSprite;

    // Кадры анимации выстрела.
    [SerializeField] protected Sprite[] _FireSprites;

    // НОВОЕ!
    // Промежуточный спрайт смены оружия.
    // Например, руки героини, отпускающие пистолет.
    [SerializeField] protected Sprite _SwitchSprite;

    // Длительность одного кадра стрельбы.
    [SerializeField] protected float _FrameTime = 0.05f;


    [Header("Audio")]

    [SerializeField] protected AudioClip _FireSound;


    [Header("Ammo")]

    // Текущее количество патронов.
    [SerializeField, Min(0)] protected int _AmmoCount = 12;

    // Иконка патронов для интерфейса.
    [SerializeField] protected Sprite _AmmoSprite;


    // Свойства для получения данных оружия.
    public Sprite IdleSprite => _IdleSprite;
    public Sprite[] FireSprites => _FireSprites;
    public Sprite SwitchSprite => _SwitchSprite;
    public float FrameTime => _FrameTime;
    public AudioClip FireSound => _FireSound;

    public int AmmoCount => _AmmoCount;
    public Sprite AmmoSprite => _AmmoSprite;

    // Максимум патронов задаётся конкретным оружием.
    public abstract int MaxAmmo { get; }

    // Каждое конкретное оружие обязано сообщать,
    // сколько урона наносит один его выстрел.
    public abstract float Damage { get; }


    protected virtual void Awake()
    {
        _AmmoCount = Mathf.Clamp(_AmmoCount, 0, MaxAmmo);
    }


    public bool TrySpendAmmo()
    {
        if (_AmmoCount <= 0)
        {
            return false;
        }

        _AmmoCount--;

        return true;
    }


    public void AddAmmo(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _AmmoCount = Mathf.Clamp(
            _AmmoCount + amount,
            0,
            MaxAmmo
        );
    }
}
