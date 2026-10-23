
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RocketDirectionalSprite : MonoBehaviour
{
    [Header("References")]

    // Корневой объект ракеты.
    // Именно он вращается в направлении полёта.
    [SerializeField] private Transform _RocketRoot;

    // Камера игрока.
    [SerializeField] private Transform _PlayerCamera;


    [Header("Directional Sprites")]

    // Вид ракеты спереди (со стороны носа).
    [SerializeField] private Sprite _FrontSprite;

    // Вид сзади (со стороны двигателя).
    [SerializeField] private Sprite _BackSprite;

    // Вид с левой стороны.
    [SerializeField] private Sprite _LeftSprite;

    // Вид с правой стороны.
    [SerializeField] private Sprite _RightSprite;


    // Компонент, который отображает изображение.
    private SpriteRenderer _SpriteRenderer;


    private void Awake()
    {
        _SpriteRenderer = GetComponent<SpriteRenderer>();

        // Автоматически ищем корень ракеты.
        if (_RocketRoot == null)
        {
            EnemyRocketProjectile rocket =
                GetComponentInParent<EnemyRocketProjectile>();

            if (rocket != null)
            {
                _RocketRoot = rocket.transform;
            }
        }
    }


    private void Start()
    {
        // Если камера не назначена,
        // автоматически находим главную камеру.
        if (_PlayerCamera == null && Camera.main != null)
        {
            _PlayerCamera = Camera.main.transform;
        }
    }


    private void LateUpdate()
    {
        // Проверяем наличие необходимых объектов.
        if (_RocketRoot == null)
        {
            return;
        }

        // Повторяем поиск камеры, если она
        // ещё не была найдена при запуске.
        if (_PlayerCamera == null)
        {
            if (Camera.main == null)
            {
                return;
            }

            _PlayerCamera = Camera.main.transform;
        }


        // 1. Определяем нужный ракурс ракеты.
        UpdateDirectionalSprite();

        // 2. Поворачиваем изображение к камере.
        FaceCamera();
    }


    // ----------------------------------------
    // ВЫБОР СПРАЙТА ПО НАПРАВЛЕНИЮ
    // ----------------------------------------

    private void UpdateDirectionalSprite()
    {
        // Направление от ракеты к камере игрока.
        Vector3 directionToCamera =
            _PlayerCamera.position - _RocketRoot.position;

        // Не нормализуем нулевой вектор.
        if (directionToCamera.sqrMagnitude < 0.0001f)
        {
            return;
        }

        directionToCamera.Normalize();


        // Определяем, насколько камера находится
        // спереди или сзади ракеты.
        float forwardDot = Vector3.Dot(
            _RocketRoot.forward,
            directionToCamera
        );


        // Определяем, насколько камера находится
        // справа или слева ракеты.
        float rightDot = Vector3.Dot(
            _RocketRoot.right,
            directionToCamera
        );


        Sprite targetSprite;


        // Сравниваем, с какой стороны
        // камера расположена сильнее.
        if (Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot))
        {
            // Камера преимущественно спереди или сзади.
            if (forwardDot >= 0f)
            {
                targetSprite = _FrontSprite;
            }
            else
            {
                targetSprite = _BackSprite;
            }
        }
        else
        {
            // Камера преимущественно сбоку.
            if (rightDot >= 0f)
            {
                targetSprite = _RightSprite;
            }
            else
            {
                targetSprite = _LeftSprite;
            }
        }


        // Изменяем спрайт только при необходимости.
        if (targetSprite != null &&
            _SpriteRenderer.sprite != targetSprite)
        {
            _SpriteRenderer.sprite = targetSprite;
        }
    }


    // ----------------------------------------
    // BILLBOARD — ПОВОРОТ СПРАЙТА К КАМЕРЕ
    // ----------------------------------------

    private void FaceCamera()
    {
        // Разворачиваем плоскость спрайта
        // параллельно плоскости камеры.
        //
        // ВАЖНО:
        // Вращаем только RocketVisual,
        // а не всю ракету.
        //
        // Иначе мы сломаем направление полёта.
        transform.rotation = _PlayerCamera.rotation;
    }
}
