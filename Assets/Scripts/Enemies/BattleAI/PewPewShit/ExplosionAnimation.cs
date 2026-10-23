
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class RocketExplosionAnimation : MonoBehaviour
{
    [Header("Explosion Animation")]

    // Массив кадров взрыва.
    // Количество кадров не ограничено.
    [SerializeField]
    private Sprite[] _ExplosionSprites;

    // Время отображения одного кадра.
    [SerializeField, Min(0.01f)]
    private float _FrameTime = 0.08f;


    [Header("Billboard")]

    // Нужно ли постоянно поворачивать
    // изображение взрыва к игроку?
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
        // Автоматически ищем камеру.
        if (Camera.main != null)
        {
            _PlayerCamera = Camera.main.transform;
        }

        // Начинаем анимацию сразу после появления.
        StartCoroutine(PlayExplosion());
    }


    private void LateUpdate()
    {
        if (!_FaceCamera)
        {
            return;
        }

        // Повторно ищем камеру, если она
        // ещё не была найдена.
        if (_PlayerCamera == null)
        {
            if (Camera.main == null)
            {
                return;
            }

            _PlayerCamera = Camera.main.transform;
        }

        // Поворачиваем плоскость взрыва к камере.
        transform.rotation = _PlayerCamera.rotation;
    }


    // ------------------------------------
    // АНИМАЦИЯ ВЗРЫВА
    // ------------------------------------

    private IEnumerator PlayExplosion()
    {
        // Если спрайтов нет, удаляем пустой эффект.
        if (_ExplosionSprites == null ||
            _ExplosionSprites.Length == 0)
        {
            Destroy(gameObject);
            yield break;
        }

        // Задержка между кадрами.
        WaitForSeconds frameDelay =
            new WaitForSeconds(
                Mathf.Max(0.01f, _FrameTime)
            );

        // Последовательно перебираем все кадры.
        for (int i = 0; i < _ExplosionSprites.Length; i++)
        {
            // Устанавливаем очередной спрайт.
            _SpriteRenderer.sprite = _ExplosionSprites[i];

            // Ждём перед следующим кадром.
            yield return frameDelay;
        }

        // Все кадры показаны!
        // Уничтожаем объект взрыва.
        Destroy(gameObject);
    }
}
