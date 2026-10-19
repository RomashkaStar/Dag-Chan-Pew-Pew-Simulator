using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

// Unity проследит, чтобы на объекте
// вместе с этим скриптом был AudioSource.
[RequireComponent(typeof(AudioSource))]

public class WeaponShooter : MonoBehaviour
{
    [Header("UI")]

    // Image, в котором показываем спрайт оружия.
    [SerializeField] private Image _WeaponImage;

    // Картинка типа боеприпаса
    [SerializeField] private Image _AmmoImage;

    // Текст, показывающий количество патронов
    [SerializeField] private TMP_Text _AmmoText;


    [Header("Weapons")]

    // Пистолет по умолчанию.
    [SerializeField] private Pistol _Pistol;

    // Дробовик
    [SerializeField] private Shotgun _Shotgun;

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

        // Выбрать пистолет.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectWeapon(_Pistol);
        }

        // Выбрать дробовик.
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectWeapon(_Shotgun);
        }
    }

    // Метод выбора нового оружия
    public void SelectWeapon(Weapon weapon) {
        // Нельзя переключать оружие посреди выстрела.
        // И нельзя выбирать соответствующее оружие.
        if (_IsFiring || weapon == null) {
            return;
        }

        // Запоминаем новое выбранное оружие.
        _SelectedWeapon = weapon;

        // Ставим обычный спрайт выбранного выбранного оружия.
        _WeaponImage.sprite = _SelectedWeapon.IdleSprite;

        // Обновляем информацию о его патронах.
        UpdateAmmoUI();
    }


    public bool TryPickUpAmmo(int amount) {
        // Проверяем, выбрано ли оружие и положительно ли кол-во получаемых патронов
        if (_SelectedWeapon == null || amount <= 0) {
            return false;
        }

        //Если патроны уже на максимуме, то мы ничего не подбираем
        if (_SelectedWeapon.AmmoCount >= _SelectedWeapon.MaxAmmo) {
            return false;
        }

        //Добавляем патроны выбранному оружию.
        _SelectedWeapon.AddAmmo(amount);

        //Обновляем текст с кол-вом патронов
        UpdateAmmoUI();

        //Возвращаем true, сообщая, что патроны подобраны
        return true;
    }


    private void Fire()
    {
        // Не стреляем, если предыдущий выстрел
        // ещё не закончился.
        if (_IsFiring || _SelectedWeapon == null)
        {
            return;
        }

        //Просим выбранное орудие потратит патрон
        if (!_SelectedWeapon.TrySpendAmmo()) {
            return;
        }

        // Патрон потрачен. Обновляем его количество на экране
        UpdateAmmoUI();

        //Блокируем новые выстрелы на время анимации
        _IsFiring = true;

        // Запускаем анимацию выстрела.
        StartCoroutine(FireAnimation());
    }


    private IEnumerator FireAnimation()
    {
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

    // Обновление интерфейса боеприпасов
    private void UpdateAmmoUI() {
        

        // Запрашиваем у оружия спрайт его боеприпасов
        _AmmoImage.sprite = _SelectedWeapon.AmmoSprite;

        // Показываем Image, только если спрайт назначен
        _AmmoImage.enabled = _SelectedWeapon.AmmoSprite != null;

        // Запрашиваем количество оставшихся патронов
        _AmmoText.text = _SelectedWeapon.AmmoCount.ToString();
    }
}