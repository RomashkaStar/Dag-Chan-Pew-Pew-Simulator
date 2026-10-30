
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeathScreenController : MonoBehaviour
{
    private InputAction _RestartAction;

    [Header("UI References")]
    [SerializeField] private GameObject _RootObject;
    [SerializeField] private Image _BackImage;
    [SerializeField] private Image _FrontImage;

    [Header("Death Animation")]
    [SerializeField] private Sprite[] _DeathFrames;
    [SerializeField, Min(0.01f)]
    private float _FrameTime = 0.08f;

    [Header("Final Foreground")]
    [SerializeField] private Sprite _FinalFrontSprite;

    [Header("Input")]
    [SerializeField] private PlayerInput _PlayerInput;

    [Header("Parallax")]
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


    private RectTransform _BackRect;
    private RectTransform _FrontRect;

    private Vector2 _BackStartPosition;
    private Vector2 _FrontStartPosition;

    private Vector2 _CurrentOffset;
    private Vector2 _TargetOffset;

    private InputAction _LookAction;

    private bool _IsDeathStarted;
    private bool _IsDeathFinished;


    private void Awake()
    {
        // Проверяем, правильно ли расположили скрипт.
        //
        // DeathScreenController нельзя размещать
        // на объекте, который он сам отключает.
        if (_RootObject != null &&
            transform.IsChildOf(_RootObject.transform))
        {
            Debug.LogError(
                "DeathScreenController находится внутри DeathScreenRoot! " +
                "Перенеси компонент на активный Canvas.",
                this
            );

            return;
        }

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

        // В начале игры экран смерти скрыт.
        if (_RootObject != null)
        {
            _RootObject.SetActive(false);
        }
    }


    private void Start()
    {
        _RestartAction = _PlayerInput.actions.FindAction("Player/Restart");

        if (_PlayerInput == null)
        {
            _PlayerInput = GetComponentInParent<PlayerInput>();
        }

        if (_PlayerInput != null &&
            _PlayerInput.actions != null)
        {
            _LookAction =
                _PlayerInput.actions.FindAction("Player/Look");
        }

        if (_LookAction == null)
        {
            Debug.LogWarning(
                "DeathScreenController: действие Player/Look не найдено!",
                this
            );
        }
    }


    // ------------------------------------
    // ЗАПУСК СМЕРТИ
    // ------------------------------------


    public void PlayDeathSequence()
    {
        // Не позволяем запускать смерть несколько раз.
        if (_IsDeathStarted)
        {
            return;
        }

        // Проверяем, активен ли сам контроллер.
        if (!gameObject.activeInHierarchy || !enabled)
        {
            Debug.LogError(
                "DeathScreenController неактивен!",
                this
            );

            return;
        }

        // Проверяем ссылки на анимацию.
        if (_RootObject == null ||
            _BackImage == null ||
            _DeathFrames == null ||
            _DeathFrames.Length == 0)
        {
            Debug.LogError(
                "Не настроены изображения или кадры смерти!",
                this
            );

            return;
        }

        _IsDeathStarted = true;
        _IsDeathFinished = false;


        // ====================================
        // ПОДГОТАВЛИВАЕМ ЗВУК СМЕРТИ
        // ====================================

        if (_DeathAudioSource != null)
        {
            // ЭТО САМОЕ ВАЖНОЕ!
            //
            // Разрешаем конкретному AudioSource
            // играть даже тогда, когда
            // AudioListener.pause == true.
            //
            // Остальные AudioSource останутся
            // под действием общей паузы.
            _DeathAudioSource.ignoreListenerPause = true;
        }


        // ====================================
        // ЗАМОРАЖИВАЕМ ИГРОВОЙ МИР
        // ====================================

        // Останавливаем игровое время.
        //
        // Физика, движение ракет и обычные
        // игровые таймеры перестанут продвигаться.
        Time.timeScale = 0f;


        // Ставим все обычные звуки на паузу.
        //
        // Музыка, лай врагов, шаги, стрельба —
        // всё замолчит.
        AudioListener.pause = true;


        // ====================================
        // ПРОИГРЫВАЕМ ЗВУК СМЕРТИ
        // ====================================

        if (_DeathAudioSource != null &&
            _DeathSound != null)
        {
            // Этот источник игнорирует общую паузу,
            // поэтому звук смерти будет слышен.
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


        // ====================================
        // ЗАПУСКАЕМ АНИМАЦИЮ СМЕРТИ
        // ====================================

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
        // Используем Realtime, чтобы анимация
        // работала даже при Time.timeScale = 0.
        WaitForSecondsRealtime delay =
            new WaitForSecondsRealtime(
                Mathf.Max(0.01f, _FrameTime)
            );

        for (int i = 0; i < _DeathFrames.Length; i++)
        {
            _BackImage.sprite = _DeathFrames[i];

            // Показываем переднюю картинку.
            if (_FrontImage != null && _FinalFrontSprite != null && i == _DeathFrames.Length - 2)
            {
                _FrontImage.sprite = _FinalFrontSprite;
                _FrontImage.gameObject.SetActive(true);
            }

            yield return delay;
        }

        // Оставляем последний кадр навсегда.
        _BackImage.sprite =
            _DeathFrames[_DeathFrames.Length - 1];

        // Теперь включается параллакс.
        _IsDeathFinished = true;

        Debug.Log("АНИМАЦИЯ СМЕРТИ ЗАВЕРШЕНА!", this);
    }


    // ------------------------------------
    // ПАРАЛЛАКС
    // ------------------------------------

    private void Update()
    {
        if (_IsDeathStarted && _RestartAction != null && _RestartAction.WasPressedThisFrame())
        {
            RestartLevel();
            return;
        }

        // Пока анимация не закончилась,
        // картинки не двигаем.
        if (!_IsDeathFinished ||
            _LookAction == null)
        {
            return;
        }

        Vector2 lookInput =
            _LookAction.ReadValue<Vector2>();

        bool isGamepad =
            _LookAction.activeControl?.device is Gamepad;

        if (isGamepad)
        {
            // Стик задаёт скорость движения,
            // поэтому используем deltaTime.
            _TargetOffset +=
                lookInput *
                _GamepadParallaxSensitivity *
                Time.unscaledDeltaTime;
        }
        else
        {
            // Мышь передаёт уже готовое смещение.
            // Накапливаем его, не умножая на deltaTime.
            _TargetOffset +=
                lookInput * _MouseParallaxSensitivity;
        }

        // Ограничиваем максимальное смещение.
        _TargetOffset.x =
            Mathf.Clamp(_TargetOffset.x, -1f, 1f);

        _TargetOffset.y =
            Mathf.Clamp(_TargetOffset.y, -1f, 1f);


        // Плавно приближаемся к нужной позиции.
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


    private void ApplyParallax()
    {
        // Задний слой смещается слабо.
        if (_BackRect != null)
        {
            _BackRect.anchoredPosition =
                _BackStartPosition +
                new Vector2(
                    _CurrentOffset.x * _BackMoveAmountX,
                    _CurrentOffset.y * _BackMoveAmountY
                );
        }

        // Передний слой смещается сильнее.
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



    public void RestartLevel()
    {
        // ====================================
        // ВОЗВРАЩАЕМ ИГРОВОЕ ВРЕМЯ
        // ====================================

        // Размораживаем игру.
        Time.timeScale = 1f;


        // ====================================
        // ВОЗВРАЩАЕМ ВСЕ ЗВУКИ
        // ====================================

        // Снимаем общую паузу.
        //
        // Все обычные AudioSource снова
        // смогут воспроизводить звуки.
        AudioListener.pause = false;


        // ====================================
        // ПЕРЕЗАПУСКАЕМ УРОВЕНЬ
        // ====================================

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }


}
