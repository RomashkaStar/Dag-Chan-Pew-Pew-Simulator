
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeathScreenController : MonoBehaviour
{
    private InputAction _RestartAction;
    private InputAction _LookAction;


    [Header("UI References")]

    // Корневой объект экрана смерти.
    // Сам контроллер должен находиться ВНЕ него,
    // иначе после отключения RootObject перестанет работать.
    [SerializeField] private GameObject _RootObject;

    [SerializeField] private Image _BackImage;
    [SerializeField] private Image _FrontImage;


    [Header("Death Animation")]

    // Кадры анимации смерти, воспроизводимые по порядку.
    [SerializeField] private Sprite[] _DeathFrames;

    [SerializeField, Min(0.01f)]
    private float _FrameTime = 0.08f;


    [Header("Final Foreground")]

    [SerializeField] private Sprite _FinalFrontSprite;


    [Header("Input")]

    [SerializeField] private PlayerInput _PlayerInput;


    [Header("Parallax")]

    // Насколько могут смещаться задний и передний
    // слои относительно своего исходного положения.
    // Передний слой двигается сильнее, создавая глубину.
    [SerializeField] private float _BackMoveAmountX = 15f;
    [SerializeField] private float _BackMoveAmountY = 10f;

    [SerializeField] private float _FrontMoveAmountX = 40f;
    [SerializeField] private float _FrontMoveAmountY = 25f;

    [SerializeField] private float _MoveSmoothness = 8f;


    [Header("Sensitivity")]

    [SerializeField] private float _MouseParallaxSensitivity = 0.003f;
    [SerializeField] private float _GamepadParallaxSensitivity = 1.2f;


    [Header("Death Audio")]

    [SerializeField] private AudioSource _DeathAudioSource;
    [SerializeField] private AudioClip _DeathSound;

    [SerializeField, Range(0f, 1f)]
    private float _DeathVolume = 1f;


    // RectTransform нужен для перемещения UI-элементов
    // через anchoredPosition.
    private RectTransform _BackRect;
    private RectTransform _FrontRect;

    // Запоминаем исходные координаты картинок,
    // чтобы параллакс не смещал их бесконечно.
    private Vector2 _BackStartPosition;
    private Vector2 _FrontStartPosition;


    // TargetOffset — куда картинка должна сместиться.
    // CurrentOffset — где она сейчас находится.
    // Разница между ними создаёт плавное движение.
    private Vector2 _CurrentOffset;
    private Vector2 _TargetOffset;


    // Первый флаг запрещает повторно запускать смерть.
    // Второй сообщает, что анимация завершена
    // и можно включать управление параллаксом.
    private bool _IsDeathStarted;
    private bool _IsDeathFinished;


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Awake()
    {
        // Контроллер нельзя размещать внутри
        // отключаемого экрана смерти.
        // Иначе вместе с RootObject отключится
        // и скрипт, управляющий всей анимацией.
        if (_RootObject != null &&
            transform.IsChildOf(_RootObject.transform))
        {
            Debug.LogError(
                "DeathScreenController находится внутри DeathScreenRoot! " +
                "Перемести контроллер на отдельный активный объект.",
                this
            );

            return;
        }

        // Запоминаем начальные положения слоёв.
        // Именно относительно них будет работать параллакс.
        if (_BackImage != null)
        {
            _BackRect = _BackImage.rectTransform;
            _BackStartPosition = _BackRect.anchoredPosition;
        }

        if (_FrontImage != null)
        {
            _FrontRect = _FrontImage.rectTransform;
            _FrontStartPosition = _FrontRect.anchoredPosition;

            _FrontImage.gameObject.SetActive(false);
        }

        // До смерти экран полностью скрыт.
        if (_RootObject != null)
        {
            _RootObject.SetActive(false);
        }
    }


    private void Start()
    {
        // Если PlayerInput не назначили,
        // пробуем найти его на родительском объекте,
        // а затем среди активных объектов сцены.
        if (_PlayerInput == null)
        {
            _PlayerInput = GetComponentInParent<PlayerInput>();
        }

        if (_PlayerInput == null)
        {
            _PlayerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (_PlayerInput != null &&
            _PlayerInput.actions != null)
        {
            // Restart отвечает за перезапуск уровня,
            // Look — за движение фона после смерти.
            _RestartAction =
                _PlayerInput.actions.FindAction("Player/Restart");

            _LookAction =
                _PlayerInput.actions.FindAction("Player/Look");
        }

        if (_RestartAction == null)
        {
            Debug.LogWarning(
                "DeathScreenController: не найден Player/Restart!",
                this
            );
        }

        if (_LookAction == null)
        {
            Debug.LogWarning(
                "DeathScreenController: не найден Player/Look!",
                this
            );
        }
    }


    // ------------------------------------
    // ЗАПУСК СМЕРТИ
    // ------------------------------------

    public void PlayDeathSequence()
    {
        // Не позволяем нескольким попаданиям
        // запустить одну и ту же смерть повторно.
        if (_IsDeathStarted)
        {
            return;
        }

        if (!gameObject.activeInHierarchy || !enabled)
        {
            Debug.LogError(
                "DeathScreenController отключён!",
                this
            );

            return;
        }

        // Без этих элементов проиграть анимацию нельзя.
        if (_RootObject == null ||
            _BackImage == null ||
            _DeathFrames == null ||
            _DeathFrames.Length == 0)
        {
            Debug.LogError(
                "Не назначены изображения или кадры смерти!",
                this
            );

            return;
        }

        _IsDeathStarted = true;
        _IsDeathFinished = false;


        // Звуку смерти разрешаем игнорировать
        // общую аудиопаузу, которую включим ниже.
        if (_DeathAudioSource != null)
        {
            _DeathAudioSource.ignoreListenerPause = true;
        }


        // Time.timeScale = 0 останавливает
        // игровое время: движение, физику и
        // всё, что зависит от deltaTime.
        //
        // Но Update продолжает вызываться,
        // поэтому параллакс и перезапуск работают.
        Time.timeScale = 0f;


        // ВАЖНО: timeScale сам по себе не выключает
        // все звуки, поэтому отдельно ставим
        // AudioListener на паузу.
        //
        // Исключение — AudioSource со свойством
        // ignoreListenerPause = true.
        AudioListener.pause = true;


        // Звук смерти играет поверх тишины,
        // несмотря на общую паузу AudioListener.
        if (_DeathAudioSource != null &&
            _DeathSound != null)
        {
            _DeathAudioSource.PlayOneShot(
                _DeathSound,
                _DeathVolume
            );
        }
        else
        {
            Debug.LogWarning(
                "Не назначен AudioSource или звук смерти!",
                this
            );
        }


        // Показываем экран смерти.
        // Передний слой появится позже,
        // на определённом кадре анимации.
        _RootObject.SetActive(true);

        _BackImage.gameObject.SetActive(true);
        _BackImage.enabled = true;

        if (_FrontImage != null)
        {
            _FrontImage.gameObject.SetActive(false);
        }

        _CurrentOffset = Vector2.zero;
        _TargetOffset = Vector2.zero;

        StartCoroutine(PlayAnimation());
    }


    // ------------------------------------
    // АНИМАЦИЯ СМЕРТИ
    // ------------------------------------

    private IEnumerator PlayAnimation()
    {
        // Используем Realtime, потому что
        // Time.timeScale уже равен нулю.
        //
        // Обычный WaitForSeconds здесь завис бы
        // в ожидании возобновления игрового времени.
        WaitForSecondsRealtime delay =
            new WaitForSecondsRealtime(
                Mathf.Max(0.01f, _FrameTime)
            );

        // Последовательно показываем кадры
        // анимации из массива _DeathFrames.
        for (int i = 0; i < _DeathFrames.Length; i++)
        {
            _BackImage.sprite = _DeathFrames[i];

            // На предпоследнем кадре добавляем
            // передний слой, который затем будет
            // двигаться независимо от заднего.
            if (_FrontImage != null &&
                _FinalFrontSprite != null &&
                i == _DeathFrames.Length - 2)
            {
                _FrontImage.sprite = _FinalFrontSprite;
                _FrontImage.gameObject.SetActive(true);
            }

            yield return delay;
        }

        // После завершения оставляем последний
        // кадр вместо скрытия изображения.
        _BackImage.sprite =
            _DeathFrames[_DeathFrames.Length - 1];

        // Только теперь разрешаем параллакс.
        _IsDeathFinished = true;

        Debug.Log("Анимация смерти завершена!", this);
    }


    // ------------------------------------
    // ОБРАБОТКА ВВОДА
    // ------------------------------------

    private void Update()
    {
        // Перезапуск проверяем отдельно и раньше
        // параллакса: он должен работать даже
        // во время самой анимации смерти.
        if (_IsDeathStarted &&
            _RestartAction != null &&
            _RestartAction.WasPressedThisFrame())
        {
            RestartLevel();
            return;
        }

        // Пока анимация не завершилась,
        // картинки не реагируют на управление.
        if (!_IsDeathFinished ||
            _LookAction == null)
        {
            return;
        }

        Vector2 lookInput =
            _LookAction.ReadValue<Vector2>();


        // Узнаём, с какого устройства пришёл ввод.
        // Мышь и геймпад обрабатываем по-разному,
        // потому что их значения имеют разный смысл.
        bool isGamepad =
            _LookAction.activeControl?.device is Gamepad;

        if (isGamepad)
        {
            // Стик передаёт направление отклонения.
            // Поэтому учитываем время кадра,
            // чтобы скорость не зависела от FPS.
            _TargetOffset +=
                lookInput *
                _GamepadParallaxSensitivity *
                Time.unscaledDeltaTime;
        }
        else
        {
            // Мышь передаёт смещение за кадр (delta).
            // Дополнительно умножать его на
            // deltaTime не требуется.
            _TargetOffset +=
                lookInput * _MouseParallaxSensitivity;
        }


        // Ограничиваем накопленное смещение.
        // Без Clamp картинки можно было бы
        // увести далеко за границы экрана.
        _TargetOffset.x =
            Mathf.Clamp(_TargetOffset.x, -1f, 1f);

        _TargetOffset.y =
            Mathf.Clamp(_TargetOffset.y, -1f, 1f);


        // Экспоненциальное сглаживание.
        //
        // Вместо мгновенного прыжка к TargetOffset
        // плавно приближаем CurrentOffset к цели.
        //
        // Используем unscaledDeltaTime, поскольку
        // игровое время заморожено.
        //
        // Формула через Exp делает сглаживание
        // более устойчивым к разному FPS.
        float smoothFactor =
            1f - Mathf.Exp(
                -_MoveSmoothness * Time.unscaledDeltaTime
            );

        _CurrentOffset = Vector2.Lerp(
            _CurrentOffset,
            _TargetOffset,
            smoothFactor
        );

        ApplyParallax();
    }


    // ------------------------------------
    // ПАРАЛЛАКС
    // ------------------------------------

    private void ApplyParallax()
    {
        // Оба слоя используют один CurrentOffset,
        // но разные множители движения.
        //
        // Задний смещается слабее, передний сильнее.
        // Так создаётся иллюзия глубины изображения.
        if (_BackRect != null)
        {
            _BackRect.anchoredPosition =
                _BackStartPosition +
                new Vector2(
                    _CurrentOffset.x * _BackMoveAmountX,
                    _CurrentOffset.y * _BackMoveAmountY
                );
        }

        if (_FrontRect != null)
        {
            _FrontRect.anchoredPosition =
                _FrontStartPosition +
                new Vector2(
                    _CurrentOffset.x * _FrontMoveAmountX,
                    _CurrentOffset.y * _FrontMoveAmountY
                );
        }
    }


    // ------------------------------------
    // ПЕРЕЗАПУСК УРОВНЯ
    // ------------------------------------

    public void RestartLevel()
    {
        // Восстанавливаем состояние игры ДО
        // загрузки сцены, потому что Time.timeScale
        // и AudioListener.pause — глобальные настройки.
        //
        // Они не сбрасываются автоматически
        // просто из-за перезагрузки уровня.
        Time.timeScale = 1f;
        AudioListener.pause = false;


        // Загружаем текущую сцену заново.
        // Все обычные объекты сцены создаются
        // с первоначальным состоянием.
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }
}
