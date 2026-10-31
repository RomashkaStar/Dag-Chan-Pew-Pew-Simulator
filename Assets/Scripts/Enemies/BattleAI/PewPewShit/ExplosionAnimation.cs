
using UnityEngine;
using System.Collections;

// На объекте обязательно должен быть SpriteRenderer,
// поскольку через него мы отображаем кадры взрыва.
[RequireComponent(typeof(SpriteRenderer))]
public class RocketExplosionAnimation : MonoBehaviour
{
    [Header("Explosion Animation")]

    // Массив кадров взрыва.
    // Порядок элементов определяет порядок анимации.
    [SerializeField]
    private Sprite[] _ExplosionSprites;

    // Время отображения каждого кадра.
    [SerializeField, Min(0.01f)]
    private float _FrameTime = 0.08f;


    [Header("Billboard")]

    // Нужно ли постоянно поворачивать
    // изображение взрыва лицом к камере?
    [SerializeField]
    private bool _FaceCamera = true;


    private SpriteRenderer _SpriteRenderer;
    private Transform _PlayerCamera;


    private void Awake()
    {
        _SpriteRenderer = GetComponent<SpriteRenderer>();
    }


    private void Start()
    {
        // Получаем главную камеру сцены.
        // Camera.main ищет камеру с тегом MainCamera.
        if (Camera.main != null)
        {
            _PlayerCamera = Camera.main.transform;
        }

        // Запускаем покадровую анимацию взрыва.
        StartCoroutine(PlayExplosion());
    }


    private void LateUpdate()
    {
        if (!_FaceCamera)
        {
            return;
        }

        // Если камера ещё не найдена или исчезла,
        // пытаемся получить её повторно.
        if (_PlayerCamera == null)
        {
            if (Camera.main == null)
            {
                return;
            }

            _PlayerCamera = Camera.main.transform;
        }

        // BILLBOARD:
        // Копируем поворот камеры на спрайт.
        //
        // Благодаря этому плоское 2D-изображение
        // постоянно ориентировано как камера,
        // создавая эффект объёмного взрыва в 3D-мире.
        //
        // Используем LateUpdate, чтобы поворот
        // происходил после обычного Update,
        // когда камера уже могла изменить положение.
        transform.rotation = _PlayerCamera.rotation;
    }


    // ------------------------------------
    // АНИМАЦИЯ ВЗРЫВА
    // ------------------------------------

    private IEnumerator PlayExplosion()
    {
        // Если кадры не назначены,
        // анимировать нечего — удаляем объект.
        if (_ExplosionSprites == null ||
            _ExplosionSprites.Length == 0)
        {
            Destroy(gameObject);
            yield break;
        }


        // Создаём один объект ожидания,
        // который будем использовать для всех кадров.
        //
        // Так не придётся создавать новый
        // WaitForSeconds на каждой итерации цикла.
        WaitForSeconds frameDelay =
            new WaitForSeconds(
                Mathf.Max(0.01f, _FrameTime)
            );


        // Последовательно перебираем массив спрайтов.
        for (int i = 0; i < _ExplosionSprites.Length; i++)
        {
            // Показываем очередной кадр взрыва.
            _SpriteRenderer.sprite = _ExplosionSprites[i];

            // Приостанавливаем корутину на FrameTime.
            // Остальная игра продолжает работать.
            yield return frameDelay;
        }


        // Когда все кадры закончились,
        // уничтожаем объект эффекта.
        //
        // Таким образом взрывы не накапливаются
        // в сцене после уничтожения ракет.
        Destroy(gameObject);
    }
}
