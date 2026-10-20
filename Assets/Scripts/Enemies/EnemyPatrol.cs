
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Points")]

    // Первая точка патрулирования.
    [SerializeField] private Transform _PointA;

    // Вторая точка патрулирования.
    [SerializeField] private Transform _PointB;


    [Header("Movement")]

    // Скорость передвижения врага.
    [SerializeField] private float _MoveSpeed = 2f;

    // Насколько близко нужно подойти к точке,
    // чтобы считать её достигнутой.
    [SerializeField] private float _ReachDistance = 0.1f;


    // true  — сейчас идём к PointB.
    // false — сейчас идём к PointA.
    private bool _GoingToB = true;


    private void Update()
    {
        Move();
    }


    private void Move()
    {
        // Если точки не назначены — не двигаемся.
        if (_PointA == null || _PointB == null)
        {
            return;
        }

        // Выбираем точку, к которой сейчас идём.
        Vector3 targetPosition = _GoingToB
            ? _PointB.position
            : _PointA.position;


        // Вычисляем вектор от врага до точки.
        Vector3 direction =
            targetPosition - transform.position;

        // Двигаемся только по горизонтальной плоскости.
        // Вверх или вниз враг самостоятельно не летает.
        direction.y = 0f;


        // Узнаём расстояние до точки.
        float distance = direction.magnitude;


        // Если дошли до нужной точки...
        if (distance <= _ReachDistance)
        {
            // ...меняем направление патрулирования.
            _GoingToB = !_GoingToB;

            return;
        }


        // Нормализуем направление.
        // Получаем вектор длиной 1.
        direction.Normalize();


        // Двигаем врага в нужную сторону.
        transform.position +=
            direction * _MoveSpeed * Time.deltaTime;


        // Поворачиваем настоящего врага
        // в направлении его движения.
        //
        // Это НЕ поворот спрайта к игроку!
        // Здесь мы задаём, куда смотрит сам противник.
        transform.rotation =
            Quaternion.LookRotation(direction, Vector3.up);
    }
}
