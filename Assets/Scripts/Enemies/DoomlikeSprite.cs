
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DoomEnemySprite : MonoBehaviour
{
    [Header("References")]

    // Камера игрока, к которой должен
    // постоянно поворачиваться спрайт.
    [SerializeField] private Transform _PlayerCamera;

    // Корневой объект противника.
    // Именно он показывает, куда смотрит враг.
    [SerializeField] private Transform _EnemyRoot;


    [Header("Directional Sprites")]

    // Враг смотрит лицом на игрока.
    [SerializeField] private Sprite _FrontSprite;

    // Игрок смотрит врагу в спину.
    [SerializeField] private Sprite _BackSprite;

    // Игрок видит левую сторону врага.
    [SerializeField] private Sprite _LeftSprite;

    // Игрок видит правую сторону врага.
    [SerializeField] private Sprite _RightSprite;


    // Компонент, отображающий спрайт.
    private SpriteRenderer _SpriteRenderer;


    private void Awake()
    {
        _SpriteRenderer = GetComponent<SpriteRenderer>();

        // Если корневой объект не назначили,
        // используем родителя спрайта.
        if (_EnemyRoot == null)
        {
            _EnemyRoot = transform.parent;
        }

        // Если камеру не назначили,
        // пробуем найти Main Camera.
        if (_PlayerCamera == null && Camera.main != null)
        {
            _PlayerCamera = Camera.main.transform;
        }
    }


    private void LateUpdate()
    {
        if (_PlayerCamera == null || _EnemyRoot == null)
        {
            return;
        }

        // Сначала поворачиваем плоскость спрайта
        // к камере игрока.
        FaceCamera();

        // Затем выбираем нужную картинку.
        UpdateDirectionalSprite();
    }


    private void FaceCamera()
    {
        // Получаем направление от спрайта
        // к камере игрока.
        Vector3 direction =
            _PlayerCamera.position - transform.position;

        // Запрещаем наклоняться вверх-вниз.
        // Спрайт должен стоять вертикально.
        direction.y = 0f;

        // Если камера находится практически
        // в той же точке, не вращаем спрайт.
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // Поворачиваем плоскость спрайта к игроку.
        transform.rotation =
            Quaternion.LookRotation(direction, Vector3.up);
    }


    private void UpdateDirectionalSprite()
    {
        // Определяем направление от самого врага
        // к игроку.
        Vector3 toPlayer =
            _PlayerCamera.position - _EnemyRoot.position;

        // Игнорируем разницу по высоте.
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return;
        }

        toPlayer.Normalize();


        // Узнаём, насколько игрок находится
        // перед врагом или позади него.
        //
        // +1 — игрок точно перед врагом.
        //  0 — игрок сбоку.
        // -1 — игрок точно за спиной врага.
        float forwardDot =
            Vector3.Dot(_EnemyRoot.forward, toPlayer);


        // Узнаём, находится ли игрок
        // слева или справа от врага.
        //
        // +1 — справа.
        // -1 — слева.
        float rightDot =
            Vector3.Dot(_EnemyRoot.right, toPlayer);


        Sprite targetSprite;


        // Проверяем, с какой стороны игрок
        // видит врага сильнее: спереди/сзади
        // или слева/справа.
        if (Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot))
        {
            // Если игрок находится перед врагом,
            // показываем лицо.
            if (forwardDot >= 0f)
            {
                targetSprite = _FrontSprite;
            }
            else
            {
                // Иначе показываем спину.
                targetSprite = _BackSprite;
            }
        }
        else
        {
            // Если игрок находится справа от врага.
            if (rightDot >= 0f)
            {
                targetSprite = _RightSprite;
            }
            else
            {
                // Если игрок находится слева.
                targetSprite = _LeftSprite;
            }
        }


        // Меняем спрайт, только если
        // нужная картинка существует
        // и отличается от текущей.
        if (targetSprite != null &&
            _SpriteRenderer.sprite != targetSprite)
        {
            _SpriteRenderer.sprite = targetSprite;
        }
    }
}
