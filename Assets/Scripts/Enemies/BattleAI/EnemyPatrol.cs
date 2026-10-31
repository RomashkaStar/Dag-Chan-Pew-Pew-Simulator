
using UnityEngine;
using System.Collections.Generic;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Points")]

    // Точки, между которыми перемещается противник.
    // Для врагов из префабов список заполняет EnemySpawner.
    [SerializeField]
    private List<Transform> _PatrolPoints =
        new List<Transform>();


    [Header("Movement")]

    [SerializeField] private float _MoveSpeed = 2f;

    // Скорость поворота к точке в градусах/сек.
    [SerializeField] private float _TurnSpeed = 180f;

    // Расстояние, на котором точка считается достигнутой.
    [SerializeField] private float _ReachDistance = 0.15f;


    [Header("Random Pauses")]

    // Вероятность остановиться после достижения точки.
    // 0 = никогда, 1 = всегда, 0.65 = 65%.
    [SerializeField, Range(0f, 1f)]
    private float _PauseChance = 0.65f;

    [SerializeField] private float _MinPauseTime = 1f;
    [SerializeField] private float _MaxPauseTime = 3f;

    // Частота случайных остановок во время движения.
    // Пример: 0.08 означает примерно 0.08 события
    // в секунду, пока остановки разрешены.
    [SerializeField, Min(0f)]
    private float _RandomStopRate = 0.08f;

    // Минимальный промежуток между случайными
    // остановками, чтобы враг не тормозил постоянно.
    [SerializeField] private float _StopCooldown = 4f;


    [Header("Looking Around")]

    [SerializeField] private bool _LookAround = true;

    // Максимальный угол осматривания относительно
    // направления, в котором враг начал паузу.
    [SerializeField] private float _LookAngle = 80f;


    // Точка, к которой сейчас идёт противник.
    private Transform _CurrentTarget;

    // Состояние временной остановки.
    private bool _IsPaused;

    // Нужно ли после паузы выбрать новую точку?
    // true — достигли точки; false — остановились в пути.
    private bool _ChoosePointAfterPause;

    private float _PauseEndTime;
    private float _NextStopAllowedTime;

    // Исходный поворот перед остановкой
    // и поворот, к которому враг сейчас стремится.
    private Quaternion _PauseStartRotation;
    private Quaternion _TargetLookRotation;

    private float _NextLookTime;


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Start()
    {
        // Сразу выбираем первую случайную точку.
        // Если подходящих точек нет, отключаем патруль.
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
        // Во время паузы враг не перемещается,
        // а только ожидает и, возможно, осматривается.
        if (_IsPaused)
        {
            HandlePause();
            return;
        }

        Move();
    }


    // ------------------------------------
    // ПЕРЕМЕЩЕНИЕ
    // ------------------------------------

    private void Move()
    {
        // Если текущая точка исчезла или не назначена,
        // пытаемся выбрать другую.
        if (_CurrentTarget == null)
        {
            if (!ChooseNextPoint())
            {
                enabled = false;
            }

            return;
        }


        Vector3 targetPosition = _CurrentTarget.position;

        // Двигаемся только по горизонтальной
        // плоскости XZ, без изменения высоты Y.
        targetPosition.y = transform.position.y;

        Vector3 direction =
            targetPosition - transform.position;

        float distance = direction.magnitude;


        // ==================================
        // ДОСТИГЛИ ТОЧКИ
        // ==================================

        if (distance <= _ReachDistance)
        {
            // Random.value выдаёт число от 0 до 1.
            // Так реализуем вероятность остановки
            // после достижения точки.
            if (Random.value < _PauseChance)
            {
                // После этой паузы выберем новую точку.
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


        // ==================================
        // СЛУЧАЙНАЯ ОСТАНОВКА В ПУТИ
        // ==================================

        // Переводим частоту случайных остановок
        // в вероятность для конкретного кадра.
        //
        // Формула 1 - Exp(-rate * deltaTime)
        // нужна, чтобы вероятность не зависела от FPS.
        //
        // Иначе при 144 FPS враг останавливался бы
        // гораздо чаще, чем при 30 FPS.
        float stopChanceThisFrame =
            1f - Mathf.Exp(
                -_RandomStopRate * Time.deltaTime
            );


        // Проверяем две вещи:
        // 1. Закончился ли cooldown остановок.
        // 2. Выпала ли случайная остановка.
        if (Time.time >= _NextStopAllowedTime &&
            Random.value < stopChanceThisFrame)
        {
            // false — после паузы продолжим движение
            // к той же точке, а не выберем новую.
            StartPause(false);
            return;
        }


        // ==================================
        // ПОВОРОТ И ДВИЖЕНИЕ
        // ==================================

        direction.Normalize();

        // Получаем поворот, при котором
        // противник смотрит в сторону точки.
        Quaternion targetRotation =
            Quaternion.LookRotation(direction, Vector3.up);


        // Поворачиваем врага постепенно,
        // ограничивая скорость через TurnSpeed.
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _TurnSpeed * Time.deltaTime
        );


        // MoveTowards перемещает врага к цели,
        // не позволяя проскочить точку.
        //
        // В отличие от CharacterController.Move,
        // прямое изменение transform.position
        // само по себе не обрабатывает столкновения.
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

        // Запоминаем причину остановки:
        // достигли точки или просто решили постоять.
        _ChoosePointAfterPause = chooseNewPoint;


        // Определяем случайную продолжительность паузы.
        // Max гарантирует, что значения не станут
        // отрицательными или перепутанными местами.
        float minTime = Mathf.Max(0f, _MinPauseTime);
        float maxTime = Mathf.Max(minTime, _MaxPauseTime);

        float pauseDuration = Random.Range(
            minTime,
            maxTime
        );


        // Запоминаем момент завершения паузы.
        _PauseEndTime = Time.time + pauseDuration;


        // Сохраняем направление, в котором враг
        // смотрел до начала остановки.
        // От него будем рассчитывать углы осматривания.
        _PauseStartRotation = transform.rotation;

        _TargetLookRotation = transform.rotation;


        // Через небольшой случайный промежуток
        // враг выберет первое направление взгляда.
        _NextLookTime =
            Time.time + Random.Range(0.2f, 0.6f);


        // Новую случайную остановку разрешаем
        // только после окончания текущей паузы
        // и дополнительного StopCooldown.
        _NextStopAllowedTime =
            Time.time +
            pauseDuration +
            Mathf.Max(0f, _StopCooldown);
    }


    // ------------------------------------
    // ПОВЕДЕНИЕ ВО ВРЕМЯ ПАУЗЫ
    // ------------------------------------

    private void HandlePause()
    {
        // ==================================
        // ЗАВЕРШЕНИЕ ПАУЗЫ
        // ==================================

        if (Time.time >= _PauseEndTime)
        {
            _IsPaused = false;


            // Если остановились после достижения
            // точки, выбираем следующую.
            if (_ChoosePointAfterPause)
            {
                if (!ChooseNextPoint())
                {
                    enabled = false;
                }
            }

            // Если пауза произошла посреди пути,
            // _CurrentTarget остаётся прежним.
            // На следующем кадре продолжим движение.
            return;
        }


        // ==================================
        // ОСМАТРИВАНИЕ
        // ==================================

        if (!_LookAround)
        {
            return;
        }


        // Периодически выбираем новое направление,
        // в которое противник будет поворачивать голову
        // (в нашем случае — весь объект врага).
        if (Time.time >= _NextLookTime)
        {
            float randomAngle = Random.Range(
                -Mathf.Abs(_LookAngle),
                Mathf.Abs(_LookAngle)
            );


            // AngleAxis создаёт поворот на случайный
            // угол вокруг мировой вертикальной оси Y.
            //
            // Умножая его на _PauseStartRotation,
            // получаем направление относительно
            // первоначального поворота врага.
            //
            // Таким образом каждый новый взгляд
            // не накапливает предыдущие повороты.
            _TargetLookRotation =
                Quaternion.AngleAxis(
                    randomAngle,
                    Vector3.up
                ) * _PauseStartRotation;


            // Следующий взгляд выберем
            // через случайный промежуток.
            _NextLookTime =
                Time.time + Random.Range(0.5f, 1.2f);
        }


        // Плавно доворачиваемся к выбранному
        // направлению с обычной скоростью TurnSpeed.
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            _TargetLookRotation,
            _TurnSpeed * Time.deltaTime
        );
    }


    // ------------------------------------
    // ВЫБОР СЛЕДУЮЩЕЙ ТОЧКИ
    // ------------------------------------

    private bool ChooseNextPoint()
    {
        // Формируем временный список точек,
        // из которых можно выбрать следующую.
        List<Transform> availablePoints =
            new List<Transform>();

        foreach (Transform point in _PatrolPoints)
        {
            // Пропускаем отсутствующие точки.
            if (point == null)
            {
                continue;
            }

            // Не выбираем ту же самую точку,
            // к которой враг направлялся до этого.
            if (point == _CurrentTarget)
            {
                continue;
            }

            // Защита от повторяющихся ссылок
            // на одну точку в общем списке.
            if (!availablePoints.Contains(point))
            {
                availablePoints.Add(point);
            }
        }


        // Если выбирать не из чего,
        // сообщаем об этом вызывающему методу.
        if (availablePoints.Count == 0)
        {
            return false;
        }


        // Выбираем случайную точку
        // из списка доступных.
        int randomIndex = Random.Range(
            0,
            availablePoints.Count
        );

        _CurrentTarget = availablePoints[randomIndex];

        return true;
    }


    // ------------------------------------
    // НАЗНАЧЕНИЕ ТОЧЕК ПАТРУЛИРОВАНИЯ
    // ------------------------------------

    // EnemySpawner вызывает этот метод после
    // создания врага, передавая точки из сцены.
    public void SetPatrolPoints(List<Transform> patrolPoints)
    {
        // Создаём новый List, чтобы у каждого врага
        // был собственный список точек.
        //
        // При этом Transform внутри списка остаются
        // ссылками на те же объекты сцены.
        //
        // То есть копируем сам список,
        // но НЕ создаём новые GameObject с точками.
        _PatrolPoints = patrolPoints != null
            ? new List<Transform>(patrolPoints)
            : new List<Transform>();
    }
}
