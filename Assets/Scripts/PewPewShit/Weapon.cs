
using UnityEngine;

// Абстрактный класс — общий шаблон для всех видов оружия.
//
// Сам Weapon нельзя добавить как компонент на GameObject.
// От него должны наследоваться конкретные классы,
// например Pistol и Shotgun.
public abstract class Weapon : MonoBehaviour
{
    [Header("Sprites")]

    [SerializeField] protected Sprite _IdleSprite;

    // Массив кадров анимации выстрела.
    [SerializeField] protected Sprite[] _FireSprites;

    // Промежуточный кадр при смене оружия.
    [SerializeField] protected Sprite _SwitchSprite;

    [SerializeField] protected float _FrameTime = 0.05f;


    [Header("Audio")]

    [SerializeField] protected AudioClip _FireSound;


    [Header("Ammo")]

    [SerializeField, Min(0)]
    protected int _AmmoCount = 12;

    [SerializeField] protected Sprite _AmmoSprite;


    // ------------------------------------
    // ПУБЛИЧНЫЕ СВОЙСТВА
    // ------------------------------------

    // Предоставляем другим скриптам доступ
    // к данным оружия, но только для чтения.
    //
    // Например, WeaponShooter может получить
    // FireSprites, но не может напрямую
    // заменить массив через это свойство.
    public Sprite IdleSprite => _IdleSprite;
    public Sprite[] FireSprites => _FireSprites;
    public Sprite SwitchSprite => _SwitchSprite;
    public float FrameTime => _FrameTime;
    public AudioClip FireSound => _FireSound;

    public int AmmoCount => _AmmoCount;
    public Sprite AmmoSprite => _AmmoSprite;


    // Абстрактные свойства не имеют реализации
    // в базовом классе.
    //
    // Каждый конкретный вид оружия ОБЯЗАН
    // определить собственный MaxAmmo и Damage.
    //
    // Например, у пистолета может быть
    // один максимум патронов, а у дробовика — другой.
    public abstract int MaxAmmo { get; }
    public abstract float Damage { get; }


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    // virtual позволяет дочерним классам
    // переопределить Awake() через override.
    //
    // Если наследник переопределяет Awake(),
    // ему следует вызвать base.Awake(),
    // чтобы сохранить эту инициализацию патронов.
    protected virtual void Awake()
    {
        // Ограничиваем стартовое количество патронов
        // максимумом конкретного оружия.
        _AmmoCount = Mathf.Clamp(_AmmoCount, 0, MaxAmmo);
    }


    // ------------------------------------
    // РАСХОД ПАТРОНОВ
    // ------------------------------------

    // Пытаемся потратить один патрон.
    //
    // Возвращаем true, если получилось,
    // или false, если боеприпасы закончились.
    //
    // WeaponShooter может использовать результат,
    // чтобы решить, разрешён ли выстрел.
    public bool TrySpendAmmo()
    {
        if (_AmmoCount <= 0)
        {
            return false;
        }

        _AmmoCount--;

        return true;
    }


    // ------------------------------------
    // ПОПОЛНЕНИЕ ПАТРОНОВ
    // ------------------------------------

    public void AddAmmo(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Добавляем патроны, но не позволяем
        // превысить максимальный запас оружия.
        //
        // Например, если максимум 30, сейчас 25,
        // а подняли ещё 10 — результат будет 30.
        _AmmoCount = Mathf.Clamp(
            _AmmoCount + amount,
            0,
            MaxAmmo
        );
    }
}
