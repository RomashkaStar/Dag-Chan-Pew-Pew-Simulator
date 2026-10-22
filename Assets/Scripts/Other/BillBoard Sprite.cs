
using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    // Камера игрока, к которой поворачиваем спрайт.
    [SerializeField] private Transform _PlayerCamera;

    private void Start()
    {
        // Если камеру не назначили вручную,
        // ищем главную камеру сцены.
        if (_PlayerCamera == null && Camera.main != null)
        {
            _PlayerCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        // Если камеры нет, ничего не делаем.
        if (_PlayerCamera == null)
        {
            return;
        }

        // Получаем направление от коробки к игроку.
        Vector3 direction =
            _PlayerCamera.position - transform.position;

        // Убираем вертикальную составляющую.
        // Спрайт не будет наклоняться вверх-вниз.
        direction.y = 0f;

        // Защита от нулевого вектора.
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // Поворачиваем спрайт в направлении игрока.
        transform.rotation =
            Quaternion.LookRotation(direction, Vector3.up);
    }
}
