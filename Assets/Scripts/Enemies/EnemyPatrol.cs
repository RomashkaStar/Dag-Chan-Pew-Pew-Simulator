
using UnityEngine;
using System.Collections.Generic;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Points")]

    // Список всех доступных точек патрулирования.
    [SerializeField]
    private List<Transform> _PatrolPoints =
        new List<Transform>();


    [Header("Movement")]

    // Скорость передвижения врага.
    [SerializeField] private float _MoveSpeed = 2f;

    // Скорость поворота врага в градусах за секунду.
    [SerializeField] private float _TurnSpeed = 180f;

    // Расстояние, на котором точка считается достигнутой.
    [SerializeField] private float _ReachDistance = 0.15f;


    [Header("Random Pauses")]

    // Вероятность остановиться после достижения точки.
    //
    // 0 = никогда.
    // 1 = всегда.
    // 0.65 = вероятность 65%.
    [SerializeField, Range(0f, 1f)]
    private float _PauseChance = 0.65f;

    // Минимальная и максимальная продолжительность паузы.
    [SerializeField] private float _MinPauseTime = 1f;
    [SerializeField] private float _MaxPauseTime = 3f;

    // Частота случайных остановок прямо во время пути.
    // 0 — отключить.
    // 0.08 — примерно 0.08 остановки в секунду,
    // пока враг движется и не находится на cooldown.
    [SerializeField, Min(0f)]
    private float _RandomStopRate = 0.08f;

    // Защита от слишком частых случайных остановок.
    [SerializeField] private float _StopCooldown = 4f;


    [Header("Looking Around")]

    // Будет ли враг оглядываться, пока стоит.
    [SerializeField] private bool _LookAround = true;

    // Максимальное отклонение от первоначального
    // направления взгляда в градусах.
    [SerializeField] private float _LookAngle = 80f;


    // Текущая точка назначения.
    private Transform _CurrentTarget;

    // Находится ли противник сейчас на паузе.
    private bool _IsPaused;

    // Нужно ли после паузы выбрать новую точку.
    private bool _ChoosePointAfterPause;

    // Время окончания текущей паузы.
    private float _PauseEndTime;

    // Когда разрешена следующая случайная остановка.
    private float _NextStopAllowedTime;

    // Поворот врага на момент начала паузы.
    private Quaternion _PauseStartRotation;

    // Поворот, к которому враг стремится во время паузы.
    private Quaternion _TargetLookRotation;

    // Время следующего изменения направления взгляда.
    private float _NextLookTime;


    private void Start()
    {
        if (!ChooseNextPoint())
        {
            Debug.LogWarning(
                "EnemyPatrol: нет доступных точек патрулирования!",
                this
            );

            enabled = false;
        }
    }


    private void Update()
    {
        // Если враг находится на паузе,
        // обрабатываем только его поведение на месте.
        if (_IsPaused)
        {
            HandlePause();
            return;
        }

        // Иначе продолжаем обычное движение.
        Move();
    }


    private void Move()
    {
        if (_CurrentTarget == null)
        {
            if (!ChooseNextPoint())
            {
                enabled = false;
            }

            return;
        }

        // Позиция точки назначения.
        Vector3 targetPosition = _CurrentTarget.position;

        // Сохраняем высоту врага,
        // поскольку пока движемся только по X и Z.
        targetPosition.y = transform.position.y;

        // Направление к точке.
        Vector3 direction =
            targetPosition - transform.position;

        float distance = direction.magnitude;


        // Если дошли до точки.
        if (distance <= _ReachDistance)
        {
            // Случайно решаем, остановиться или нет.
            if (Random.value < _PauseChance)
            {
                // После этой паузы нужно выбрать
                // следующую точку.
                StartPause(true);
            }
            else
            {
                if (!ChooseNextPoint())
                {
                    enabled = false;
                }
            }

            return;
        }


        // Случайная остановка ПОСРЕДИ пути.
        //
        // Используем Exp, чтобы вероятность
        // корректно учитывала длительность кадра.
        float stopChanceThisFrame =
            1f - Mathf.Exp(
                -_RandomStopRate * Time.deltaTime
            );

        if (Time.time >= _NextStopAllowedTime &&
            Random.value < stopChanceThisFrame)
        {
            // После такой паузы продолжаем
            // идти к прежней точке.
            StartPause(false);
            return;
        }


        direction.Normalize();

        // Плавно поворачиваем врага в сторону движения.
        Quaternion targetRotation =
            Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _TurnSpeed * Time.deltaTime
        );


        // Двигаемся к цели.
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            _MoveSpeed * Time.deltaTime
        );
    }


    // ------------------------------------
    // НАЧАЛО ПАУЗЫ
    // ------------------------------------

    private void StartPause(bool chooseNewPoint)
    {
        _IsPaused = true;

        // Запоминаем, зачем мы остановились.
        _ChoosePointAfterPause = chooseNewPoint;

        // Выбираем случайную продолжительность.
        float minTime = Mathf.Max(0f, _MinPauseTime);
        float maxTime = Mathf.Max(minTime, _MaxPauseTime);

        float pauseDuration = Random.Range(
            minTime,
            maxTime
        );

        _PauseEndTime = Time.time + pauseDuration;

        // Запоминаем направление, в котором враг
        // смотрел до остановки.
        _PauseStartRotation = transform.rotation;

        _TargetLookRotation = transform.rotation;

        // Через небольшое время враг начнёт оглядываться.
        _NextLookTime = Time.time + Random.Range(0.2f, 0.6f);

        // Не позволяем слишком быстро снова
        // остановиться посреди маршрута.
        _NextStopAllowedTime =
            Time.time + pauseDuration + Mathf.Max(0f, _StopCooldown);
    }


    // ------------------------------------
    // ПОВЕДЕНИЕ ВО ВРЕМЯ ПАУЗЫ
    // ------------------------------------

    private void HandlePause()
    {
        // Проверяем, закончилась ли пауза.
        if (Time.time >= _PauseEndTime)
        {
            _IsPaused = false;

            // Если остановились на достигнутой точке,
            // выбираем новую.
            if (_ChoosePointAfterPause)
            {
                if (!ChooseNextPoint())
                {
                    enabled = false;
                }
            }

            // Если остановились посреди пути,
            // _CurrentTarget не меняется.
            // В следующем кадре продолжим движение.
            return;
        }


        // Если оглядывание отключено,
        // враг просто стоит на месте.
        if (!_LookAround)
        {
            return;
        }


        // Пора выбрать новое направление взгляда.
        if (Time.time >= _NextLookTime)
        {
            // Случайный угол от -LookAngle до +LookAngle.
            float randomAngle = Random.Range(
                -Mathf.Abs(_LookAngle),
                Mathf.Abs(_LookAngle)
            );

            // Поворачиваемся относительно направления,
            // которое было в момент начала паузы.
            _TargetLookRotation =
                Quaternion.AngleAxis(
                    randomAngle,
                    Vector3.up
                ) * _PauseStartRotation;

            // Следующее оглядывание через
            // случайный промежуток времени.
            _NextLookTime =
                Time.time + Random.Range(0.5f, 1.2f);
        }


        // Плавно поворачиваем врага к выбранному углу.
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            _TargetLookRotation,
            _TurnSpeed * Time.deltaTime
        );
    }


    // ------------------------------------
    // ВЫБОР СЛУЧАЙНОЙ ТОЧКИ
    // ------------------------------------

    private bool ChooseNextPoint()
    {
        List<Transform> availablePoints =
            new List<Transform>();

        foreach (Transform point in _PatrolPoints)
        {
            // Пропускаем пустые ссылки.
            if (point == null)
            {
                continue;
            }

            // Не выбираем ту же самую точку.
            if (point == _CurrentTarget)
            {
                continue;
            }

            // Не добавляем дубликаты.
            if (!availablePoints.Contains(point))
            {
                availablePoints.Add(point);
            }
        }

        // Других доступных точек нет.
        if (availablePoints.Count == 0)
        {
            return false;
        }

        // Случайно выбираем следующую точку.
        int randomIndex = Random.Range(
            0,
            availablePoints.Count
        );

        _CurrentTarget = availablePoints[randomIndex];

        return true;
    }
}
