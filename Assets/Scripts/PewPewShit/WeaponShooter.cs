
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class WeaponShooter : MonoBehaviour
{
    [Header("Weapon UI")]

    // Image, отображающий спрайт оружия.
    [SerializeField] private Image _WeaponImage;

    // Объект, который будем опускать и поднимать.
    // Это WeaponSwapContainer, НЕ HandContainer!
    [SerializeField] private RectTransform _WeaponSwapRect;


    [Header("Ammo UI")]

    // Картинка типа патронов.
    [SerializeField] private Image _AmmoImage;

    // Текст количества патронов.
    [SerializeField] private TMP_Text _AmmoText;


    [Header("Weapons")]

    // Пистолет.
    [SerializeField] private Pistol _Pistol;

    // Дробовик.
    [SerializeField] private Shotgun _Shotgun;

    // Выбранное оружие.
    private Weapon _SelectedWeapon;


    [Header("Damage")]

    // Камера, из центра которой стреляем.
    [SerializeField] private Camera _PlayerCamera;

    // Максимальное расстояние попадания.
    [SerializeField] private float _HitDistance = 100f;

    // Слои объектов, в которые может попасть пуля.
    // По умолчанию включены все слои.
    [SerializeField] private LayerMask _HitLayers = ~0;


    [Header("Weapon Switching")]

    // Насколько далеко опускаем оружие вниз.
    // Значение задаётся в единицах UI.
    [SerializeField] private float _LowerDistance = 1200f;

    // Сколько секунд занимает опускание.
    [SerializeField] private float _LowerDuration = 0.25f;

    // Сколько секунд занимает поднятие.
    [SerializeField] private float _RaiseDuration = 0.3f;


    // Начальная позиция контейнера смены оружия.
    private Vector2 _SwapStartPosition;


    // Источник звука.
    private AudioSource _AudioSource;

    // Сейчас проигрывается выстрел?
    private bool _IsFiring;

    // Сейчас происходит смена оружия?
    private bool _IsSwitching;


    private void Awake()
    {
        _AudioSource = GetComponent<AudioSource>();

        // Пытаемся найти камеру среди родителей.
        if (_PlayerCamera == null)
        {
            _PlayerCamera = GetComponentInParent<Camera>();
        }

        // Если не нашли — ищем главную камеру сцены.
        if (_PlayerCamera == null)
        {
            _PlayerCamera = Camera.main;
        }
    }


    private void Start()
    {
        // Проверяем, что основные ссылки назначены.
        if (_WeaponImage == null ||
            _WeaponSwapRect == null ||
            _Pistol == null)
        {
            Debug.LogError(
                "WeaponShooter: не назначены Weapon Image, " +
                "Weapon Swap Rect или Pistol!"
            );

            enabled = false;
            return;
        }

        // Запоминаем обычное положение
        // контейнера с оружием.
        _SwapStartPosition =
            _WeaponSwapRect.anchoredPosition;

        // По умолчанию выбираем пистолет.
        // На старте анимация переключения не нужна.
        _SelectedWeapon = _Pistol;

        // Показываем обычный спрайт пистолета.
        _WeaponImage.sprite = _SelectedWeapon.IdleSprite;

        // Обновляем интерфейс патронов.
        UpdateAmmoUI();
    }


    private void Update()
    {
        // ЛКМ — стрельба.
        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }

        // Клавиша 1 — пистолет.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectWeapon(_Pistol);
        }

        // Клавиша 2 — дробовик.
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectWeapon(_Shotgun);
        }
    }


    // ---------------------------
    // ВЫБОР ОРУЖИЯ
    // ---------------------------

    public void SelectWeapon(Weapon newWeapon)
    {
        // Если оружия нет, ничего не делаем.
        if (newWeapon == null)
        {
            return;
        }

        // Если уже держим это оружие,
        // нет смысла снова его доставать.
        if (_SelectedWeapon == newWeapon)
        {
            return;
        }

        // Нельзя менять оружие посреди выстрела
        // или другой смены оружия.
        if (_IsFiring || _IsSwitching)
        {
            return;
        }

        // Помечаем начало смены оружия.
        _IsSwitching = true;

        // Запускаем анимацию переключения.
        StartCoroutine(
            SwitchWeaponAnimation(newWeapon)
        );
    }


    // ---------------------------
    // АНИМАЦИЯ СМЕНЫ ОРУЖИЯ
    // ---------------------------

    private IEnumerator SwitchWeaponAnimation(Weapon newWeapon)
    {
        // Запоминаем старое оружие.
        Weapon oldWeapon = _SelectedWeapon;


        // ЭТАП 1.
        // Меняем обычный спрайт старого оружия
        // на промежуточный.
        //
        // Например, героиня отпускает пистолет.
        _WeaponImage.sprite =
            oldWeapon.SwitchSprite != null
                ? oldWeapon.SwitchSprite
                : oldWeapon.IdleSprite;


        // ЭТАП 2.
        // Вычисляем позицию за нижней границей экрана.
        Vector2 loweredPosition =
            _SwapStartPosition
            + Vector2.down * _LowerDistance;

        // Плавно опускаем оружие.
        yield return MoveWeapon(
            loweredPosition,
            _LowerDuration
        );


        // ЭТАП 3.
        // Оружие находится внизу.
        //
        // Теперь действительно меняем
        // выбранное оружие.
        _SelectedWeapon = newWeapon;

        // Запрашиваем промежуточный спрайт
        // уже у НОВОГО оружия.
        //
        // Например, руки берут дробовик.
        _WeaponImage.sprite =
            newWeapon.SwitchSprite != null
                ? newWeapon.SwitchSprite
                : newWeapon.IdleSprite;

        // Обновляем иконку патронов и их количество.
        UpdateAmmoUI();


        // ЭТАП 4.
        // Плавно поднимаем новое оружие обратно.
        yield return MoveWeapon(
            _SwapStartPosition,
            _RaiseDuration
        );


        // ЭТАП 5.
        // Когда руки полностью поднялись,
        // устанавливаем обычный спрайт нового оружия.
        _WeaponImage.sprite = newWeapon.IdleSprite;


        // Смена закончена.
        // Теперь снова можно стрелять и переключаться.
        _IsSwitching = false;
    }


    // ---------------------------
    // ПЛАВНОЕ ПЕРЕМЕЩЕНИЕ ОРУЖИЯ
    // ---------------------------

    private IEnumerator MoveWeapon(
        Vector2 targetPosition,
        float duration
    )
    {
        // Запоминаем позицию,
        // из которой начинается движение.
        Vector2 startPosition =
            _WeaponSwapRect.anchoredPosition;

        // Если длительность нулевая,
        // просто ставим объект в нужную точку.
        if (duration <= 0f)
        {
            _WeaponSwapRect.anchoredPosition = targetPosition;
            yield break;
        }


        // Сколько времени прошло с начала движения.
        float elapsed = 0f;


        // Пока не прошло всё заданное время.
        while (elapsed < duration)
        {
            // Прибавляем время текущего кадра.
            elapsed += Time.deltaTime;

            // Превращаем прошедшее время
            // в число от 0 до 1.
            //
            // 0 = начало движения
            // 1 = конец движения
            float t = Mathf.Clamp01(
                elapsed / duration
            );


            // SmoothStep делает движение мягче:
            // сначала разгон, затем замедление.
            float smoothT = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            // Плавно перемещаем UI-контейнер
            // между начальной и конечной точкой.
            _WeaponSwapRect.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );


            // Ждём следующего кадра.
            yield return null;
        }


        // Гарантированно выставляем
        // точную конечную позицию.
        _WeaponSwapRect.anchoredPosition =
            targetPosition;
    }


    // ---------------------------
    // СТРЕЛЬБА
    // ---------------------------

    private void Fire()
    {
        // Не стреляем во время анимации выстрела
        // или смены оружия.
        if (_IsFiring || _IsSwitching)
        {
            return;
        }

        if (_SelectedWeapon == null)
        {
            return;
        }

        // Нет патронов — нет выстрела.
        if (!_SelectedWeapon.TrySpendAmmo())
        {
            return;
        }

        // Обновляем HUD.
        UpdateAmmoUI();

        // НОВОЕ!
        // Сразу при выстреле проверяем попадание
        // и наносим урон выбранным оружием.
        DealDamage(_SelectedWeapon);

        // Запускаем обычную анимацию стрельбы.
        _IsFiring = true;

        StartCoroutine(
            FireAnimation(_SelectedWeapon)
        );
    }



    private void DealDamage(Weapon weapon)
    {
        if (_PlayerCamera == null || weapon == null)
        {
            Debug.LogWarning("Нет камеры или оружия!");
            return;
        }

        // Создаём луч из центра камеры.
        Ray ray = new Ray(
            _PlayerCamera.transform.position,
            _PlayerCamera.transform.forward
        );

        // Проверяем, столкнулся ли луч с Collider.
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            _HitDistance,
            _HitLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            // Показываем луч до точки попадания.
            Debug.DrawLine(
                ray.origin,
                hit.point,
                Color.red,
                2f
            );

            // ВАЖНО!
            // Показываем, во что именно попала пуля.
            Debug.Log(
                "Пуля попала в: " + hit.collider.name +
                " | Корневой объект: " + hit.collider.transform.root.name
            );

            // Ищем IDamageable на объекте попадания
            // или у его родителей.
            IDamageable damageable =
                hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(weapon.Damage);

                Debug.Log(
                    "УРОН НАНЕСЁН: " + weapon.Damage
                );
            }
            else
            {
                Debug.LogWarning(
                    "ПОПАДАНИЕ БЕЗ УРОНА! Объект: "
                    + hit.collider.name
                    + " — у него не найден IDamageable."
                );
            }
        }
        else
        {
            // Показываем луч, который никуда не попал.
            Debug.DrawRay(
                ray.origin,
                ray.direction * _HitDistance,
                Color.yellow,
                2f
            );

            Debug.LogWarning(
                "ПРОМАХ! Луч не встретил ни одного Collider."
            );
        }
    }




    private IEnumerator FireAnimation(Weapon weapon)
    {
        // Получаем звук выбранного оружия.
        if (weapon.FireSound != null)
        {
            _AudioSource.PlayOneShot(weapon.FireSound);
        }

        // Получаем спрайты стрельбы.
        Sprite[] fireSprites = weapon.FireSprites;

        // Если кадры анимации существуют,
        // проигрываем их по очереди.
        if (fireSprites != null)
        {
            foreach (Sprite sprite in fireSprites)
            {
                if (sprite != null)
                {
                    _WeaponImage.sprite = sprite;
                }

                // Ждём время одного кадра.
                yield return new WaitForSeconds(
                    Mathf.Max(0.01f, weapon.FrameTime)
                );
            }
        }

        // Возвращаем обычный спрайт.
        _WeaponImage.sprite = weapon.IdleSprite;

        // Разрешаем новые выстрелы.
        _IsFiring = false;
    }


    // ---------------------------
    // ИНТЕРФЕЙС ПАТРОНОВ
    // ---------------------------

    private void UpdateAmmoUI()
    {
        if (_SelectedWeapon == null)
        {
            return;
        }

        // Обновляем изображение типа боеприпаса.
        if (_AmmoImage != null)
        {
            _AmmoImage.sprite = _SelectedWeapon.AmmoSprite;
            _AmmoImage.enabled =
                _SelectedWeapon.AmmoSprite != null;
        }

        // Обновляем числовой счётчик.
        if (_AmmoText != null)
        {
            _AmmoText.text = _SelectedWeapon.AmmoCount.ToString();
        }
    }


    // ---------------------------
    // ПОДБОР ПАТРОНОВ
    // ---------------------------

    // Оставляем совместимость с нашим AmmoPickup.
    public bool TryPickUpAmmo(int amount)
    {
        if (_SelectedWeapon == null || amount <= 0)
        {
            return false;
        }

        // Если патронов уже максимум,
        // подобрать их нельзя.
        if (_SelectedWeapon.AmmoCount >=
            _SelectedWeapon.MaxAmmo)
        {
            return false;
        }

        // Добавляем патроны выбранному оружию.
        _SelectedWeapon.AddAmmo(amount);

        // Обновляем интерфейс.
        UpdateAmmoUI();

        return true;
    }
}
