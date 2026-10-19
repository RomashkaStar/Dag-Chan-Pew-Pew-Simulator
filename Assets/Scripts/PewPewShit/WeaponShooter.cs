using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Unity проследит, чтобы на объекте
// вместе с этим скриптом был AudioSource.
[RequireComponent(typeof(AudioSource))]

public class WeaponShooter : MonoBehaviour
{
    [Header("UI")]

    // Image, в котором показываем спрайт оружия.
    [SerializeField] private Image _WeaponImage;


    [Header("Weapons")]

    // Пистолет по умолчанию.
    [SerializeField] private Pistol _Pistol;

    // Выбранное оружие.
    private Weapon _SelectedWeapon;


    [Header("Audio")]

    // Компонент, который будет воспроизводить звук.
    private AudioSource _AudioSource;


    // Идёт ли сейчас анимация выстрела.
    private bool _IsFiring;


    private void Awake()
    {
        // Получаем AudioSource с нашего объекта.
        _AudioSource = GetComponent<AudioSource>();
    }


    private void Start()
    {
        // Выбираем пистолет.
        _SelectedWeapon = _Pistol;

        // Ставим его обычный спрайт.
        if (_SelectedWeapon != null)
        {
            _WeaponImage.sprite = _SelectedWeapon.IdleSprite;
        }
    }


    private void Update()
    {
        // По нажатию ЛКМ пытаемся выстрелить.
        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }
    }


    private void Fire()
    {
        // Не стреляем, если предыдущий выстрел
        // ещё не закончился.
        if (_IsFiring)
        {
            return;
        }

        // Не стреляем, если оружие не выбрано.
        if (_SelectedWeapon == null)
        {
            return;
        }

        // Запускаем анимацию выстрела.
        StartCoroutine(FireAnimation());
    }


    private IEnumerator FireAnimation()
    {
        _IsFiring = true;

        // Запоминаем оружие, из которого стреляем.
        Weapon weapon = _SelectedWeapon;

        // Запрашиваем у оружия спрайты стрельбы.
        Sprite[] fireSprites = weapon.FireSprites;

        // Запрашиваем у оружия звук выстрела.
        AudioClip fireSound = weapon.FireSound;


        // ВОСПРОИЗВОДИМ ЗВУК

        // Если у выбранного оружия есть звук...
        if (fireSound != null)
        {
            // ...проигрываем его один раз.
            _AudioSource.PlayOneShot(fireSound);
        }


        // ПРОИГРЫВАЕМ АНИМАЦИЮ

        foreach (Sprite sprite in fireSprites)
        {
            // Меняем изображение оружия.
            _WeaponImage.sprite = sprite;

            // Ждём указанное оружием время.
            yield return new WaitForSeconds(weapon.FrameTime);
        }


        // Возвращаем оружие в обычное положение.
        _WeaponImage.sprite = weapon.IdleSprite;

        // Разрешаем следующий выстрел.
        _IsFiring = false;
    }
}