
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    // Машина состояний (FSM).
    // В каждый момент времени враг находится
    // только в одном из этих состояний.
    private enum AIState
    {
        Patrol,     // Патрулирует территорию.
        Alert,      // Заметил игрока, готовится атаковать.
        Attack,     // Атакует игрока.
        Search      // Потерял игрока, пытается обнаружить снова.
    }


    [Header("References")]

    [SerializeField] private Enemy _Enemy;
    [SerializeField] private EnemyPatrol _Patrol;
    [SerializeField] private EnemyRocketLauncher _RocketLauncher;

    // Цель нашего противника — Dag-Chan.
    [SerializeField] private Transform _Player;

    // Точка, из которой противник проверяет видимость.
    [SerializeField] private Transform _EyePoint;


    [Header("Detection")]

    [SerializeField] private float _DetectionDistance = 15f;

    // Полный угол обзора врага.
    // Например, 120° означает по 60° в каждую сторону.
    [SerializeField, Range(1f, 360f)]
    private float _ViewAngle = 120f;

    // Слои, которые учитываются при проверке видимости.
    // Важно включить Player И стены.
    [SerializeField] private LayerMask _SightLayers = ~0;

    // Запасная высота прицеливания,
    // если CharacterController игрока не найден.
    [SerializeField] private float _PlayerAimHeight = 1f;


    [Header("Combat")]

    [SerializeField] private float _AttackDistance = 12f;

    // Сколько враг соображает после обнаружения игрока.
    [SerializeField] private float _ReactionTime = 0.6f;

    // Пауза между атаками.
    [SerializeField] private float _FireCooldown = 2f;

    // Скорость поворота противника в градусах/сек.
    [SerializeField] private float _TurnSpeed = 240f;

    // Допустимая погрешность прицеливания.
    // Враг не стреляет, пока не повернётся
    // достаточно точно в сторону игрока.
    [SerializeField] private float _FireAngle = 12f;

    // Через сколько секунд после потери игрока
    // враг забывает о нём и возвращается к патрулю.
    [SerializeField] private float _ForgetTime = 3f;


    // Начальное состояние — патрулирование.
    private AIState _State = AIState.Patrol;

    // Вместо таймеров, уменьшаемых каждый кадр,
    // храним конкретное время нужного события.
    private float _ReactionEndTime;
    private float _NextFireTime;
    private float _LastSeenTime;

    // Последняя известная позиция игрока.
    private Vector3 _LastSeenPosition;

    // Позволяет врагу один раз отреагировать
    // на первое получение урона.
    private bool _NoticedDamage;

    private CharacterController _PlayerController;


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Start()
    {
        // Если компоненты не назначены в Inspector,
        // пробуем найти их на этом же объекте.
        if (_Enemy == null)
            _Enemy = GetComponent<Enemy>();

        if (_Patrol == null)
            _Patrol = GetComponent<EnemyPatrol>();

        if (_RocketLauncher == null)
            _RocketLauncher = GetComponent<EnemyRocketLauncher>();

        // CharacterController используем для
        // определения центра тела игрока при
        // проверке прямой видимости.
        if (_Player != null)
        {
            _PlayerController =
                _Player.GetComponentInChildren<CharacterController>();
        }
    }


    // ------------------------------------
    // МАШИНА СОСТОЯНИЙ
    // ------------------------------------

    private void Update()
    {
        // Мёртвый враг не должен принимать решения.
        // Без ссылки на игрока AI тоже не работает.
        if (_Enemy == null || _Enemy.IsDead || _Player == null)
        {
            return;
        }


        // Проверяем, получил ли враг урон впервые.
        //
        // HasBeenHit хранится в Enemy, а NoticedDamage
        // позволяет не реагировать на один и тот же
        // факт получения урона бесконечно.
        bool justHit =
            _Enemy.HasBeenHit && !_NoticedDamage;

        if (_Enemy.HasBeenHit)
        {
            _NoticedDamage = true;
        }


        // switch — основа нашей машины состояний.
        //
        // Вместо одновременного выполнения
        // патрулирования, поиска и атаки
        // выполняется логика только одного состояния.
        switch (_State)
        {
            // ==================================
            // PATROL — ПАТРУЛИРОВАНИЕ
            // ==================================

            case AIState.Patrol:

                // Враг переходит в тревогу, если:
                // 1. Увидел игрока в секторе обзора.
                // 2. Получил первый урон.
                //
                // Непосредственное перемещение между
                // точками выполняет EnemyPatrol.
                if (CanSeePlayer(true) || justHit)
                {
                    EnterAlert();
                }

                break;


            // ==================================
            // ALERT — ТРЕВОГА
            // ==================================

            case AIState.Alert:

                // Если игрок виден, обновляем
                // последнюю известную позицию.
                //
                // Здесь угол обзора уже не проверяем:
                // после обнаружения враг может
                // продолжать следить за игроком.
                if (CanSeePlayer(false))
                {
                    RememberPlayerPosition();
                }

                TurnTowards(_LastSeenPosition);


                // Выдерживаем задержку реакции.
                // После неё либо атакуем видимого игрока,
                // либо начинаем поиск потерянного.
                if (Time.time >= _ReactionEndTime)
                {
                    _State = CanSeePlayer(false)
                        ? AIState.Attack
                        : AIState.Search;
                }

                break;


            // ==================================
            // ATTACK — АТАКА
            // ==================================

            case AIState.Attack:

                // Если потеряли прямую видимость,
                // прекращаем атаку и переходим к поиску.
                if (!CanSeePlayer(false))
                {
                    _State = AIState.Search;
                    break;
                }

                // Пока игрок виден, запоминаем
                // его позицию, поворачиваемся
                // и пытаемся выпустить ракету.
                RememberPlayerPosition();
                TurnTowards(_LastSeenPosition);

                TryShoot();

                break;


            // ==================================
            // SEARCH — ПОИСК
            // ==================================

            case AIState.Search:

                // Если игрок снова появился,
                // сразу возвращаемся к атаке.
                if (CanSeePlayer(false))
                {
                    RememberPlayerPosition();
                    _State = AIState.Attack;
                    break;
                }

                // Продолжаем смотреть туда,
                // где последний раз видели игрока.
                TurnTowards(_LastSeenPosition);


                // Если слишком долго не видели игрока,
                // забываем о нём и возобновляем патруль.
                if (Time.time - _LastSeenTime >= _ForgetTime)
                {
                    ReturnToPatrol();
                }

                break;
        }
    }


    // ------------------------------------
    // НАЗНАЧЕНИЕ ЦЕЛИ
    // ------------------------------------

    // Этот метод вызывает EnemySpawner после
    // создания противника через Instantiate.
    //
    // Префаб не может заранее хранить обычную
    // ссылку на конкретного игрока в сцене,
    // поэтому передаём её после создания врага.
    public void SetPlayer(Transform player)
    {
        _Player = player;
    }


    // ------------------------------------
    // ПЕРЕХОД В СОСТОЯНИЕ ТРЕВОГИ
    // ------------------------------------

    private void EnterAlert()
    {
        Debug.Log(
            $"{name} заметил игрока!",
            this
        );

        _State = AIState.Alert;


        // Запоминаем, КОГДА должна закончиться
        // задержка реакции.
        //
        // Например, если Time.time = 10,
        // а ReactionTime = 0.6,
        // реагировать закончим на 10.6 секунде.
        _ReactionEndTime =
            Time.time + Mathf.Max(0f, _ReactionTime);

        RememberPlayerPosition();


        // Отключаем патрулирование, чтобы
        // EnemyPatrol не пытался двигать врага,
        // пока EnemyCombatAI управляет его поворотом.
        if (_Patrol != null)
        {
            _Patrol.enabled = false;
        }

        // Сообщаем основному скрипту Enemy,
        // что противник перешёл в тревогу.
        _Enemy.SetAlerted();
    }


    // ------------------------------------
    // ЗАПОМИНАНИЕ ИГРОКА
    // ------------------------------------

    private void RememberPlayerPosition()
    {
        // Запоминаем время и позицию игрока.
        //
        // Даже если потом игрок скроется за стеной,
        // AI будет знать, куда смотреть.
        _LastSeenTime = Time.time;

        _LastSeenPosition =
            _Player.position + Vector3.up * _PlayerAimHeight;
    }


    // ------------------------------------
    // ПРОВЕРКА ВИДИМОСТИ
    // ------------------------------------

    // checkViewAngle определяет, учитывать ли
    // сектор обзора противника.
    //
    // При патрулировании — true.
    // После обнаружения игрока — false.
    //
    // При этом расстояние и препятствия
    // проверяются в обоих случаях.
    private bool CanSeePlayer(bool checkViewAngle)
    {
        if (_Player == null)
        {
            return false;
        }


        // Точка, из которой враг смотрит на игрока.
        // Если EyePoint не задан, используем
        // положение немного выше основания врага.
        Vector3 eyePosition = _EyePoint != null
            ? _EyePoint.position
            : transform.position + Vector3.up * 1.2f;


        Vector3 targetPosition;

        if (_PlayerController != null)
        {
            // Целимся в центр CharacterController,
            // а не в Transform у ног игрока.
            targetPosition = _PlayerController.bounds.center;
        }
        else
        {
            targetPosition =
                _Player.position + Vector3.up * _PlayerAimHeight;
        }


        // Вычисляем направление и расстояние
        // от глаз врага до игрока.
        Vector3 direction = targetPosition - eyePosition;

        float distance = direction.magnitude;


        // Слишком далёкого игрока враг не видит.
        if (distance > _DetectionDistance ||
            distance < 0.001f)
        {
            return false;
        }


        // ==================================
        // ПРОВЕРКА УГЛА ОБЗОРА
        // ==================================

        if (checkViewAngle)
        {
            Vector3 horizontalDirection =
                _Player.position - transform.position;

            // Убираем вертикальную составляющую:
            // сектор обзора проверяется только
            // по горизонтальной плоскости.
            horizontalDirection.y = 0f;

            if (horizontalDirection.sqrMagnitude > 0.0001f)
            {
                // Vector3.Angle возвращает угол между
                // направлением взгляда врага и игроком.
                float angle = Vector3.Angle(
                    transform.forward,
                    horizontalDirection
                );

                // ViewAngle — ПОЛНЫЙ угол обзора.
                // Поэтому сравниваем с его половиной.
                if (angle > _ViewAngle / 2f)
                {
                    return false;
                }
            }
        }


        // ==================================
        // ПРОВЕРКА ПРЕПЯТСТВИЙ
        // ==================================

        // RaycastAll выпускает луч к игроку
        // и возвращает все пересечённые Collider.
        //
        // Нам нужно выяснить, что луч встретил
        // раньше: стену или самого игрока.
        RaycastHit[] hits = Physics.RaycastAll(
            eyePosition,
            direction.normalized,
            distance + 0.1f,
            _SightLayers,
            QueryTriggerInteraction.Ignore
        );


        // RaycastAll НЕ гарантирует порядок попаданий.
        //
        // Сортируем результаты по расстоянию:
        // ближайший объект будет первым.
        //
        // (a, b) => ... — лямбда-функция,
        // сравнивающая два попадания.
        // Sort использует её для определения порядка.
        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );


        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;


            // Игнорируем собственные Collider врага.
            // Иначе луч обнаруживал бы его тело
            // и считал, что обзор заблокирован.
            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                continue;
            }


            // Если первый значимый Collider
            // принадлежит игроку — видимость есть.
            if (hitTransform == _Player ||
                hitTransform.IsChildOf(_Player))
            {
                Debug.DrawLine(
                    eyePosition,
                    hit.point,
                    Color.green
                );

                return true;
            }


            // Если первым встретили стену
            // или другой объект, игрок закрыт.
            Debug.DrawLine(
                eyePosition,
                hit.point,
                Color.red
            );

            return false;
        }


        // Луч никого не обнаружил.
        // Например, Player не включён в SightLayers.
        Debug.DrawLine(
            eyePosition,
            targetPosition,
            Color.yellow
        );

        return false;
    }


    // ------------------------------------
    // ПОВОРОТ К ИГРОКУ
    // ------------------------------------

    private void TurnTowards(Vector3 position)
    {
        Vector3 direction = position - transform.position;

        // Поворачиваем врага только вокруг оси Y.
        // Он не должен наклоняться вверх-вниз.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }


        Quaternion targetRotation =
            Quaternion.LookRotation(direction);


        // RotateTowards постепенно поворачивает
        // противника к нужному направлению.
        //
        // TurnSpeed ограничивает скорость поворота,
        // а deltaTime делает её независимой от FPS.
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _TurnSpeed * Time.deltaTime
        );
    }


    // ------------------------------------
    // ПОПЫТКА ВЫСТРЕЛА
    // ------------------------------------

    private void TryShoot()
    {
        if (_RocketLauncher == null)
        {
            return;
        }


        Vector3 direction =
            _Player.position - transform.position;

        direction.y = 0f;


        // Недостаточно просто видеть игрока:
        // для стрельбы он должен находиться
        // внутри AttackDistance.
        if (direction.magnitude > _AttackDistance)
        {
            return;
        }


        // Проверяем, насколько точно враг
        // повернулся в сторону игрока.
        //
        // Если угол больше FireAngle,
        // противник сначала должен довернуться.
        float angle = Vector3.Angle(
            transform.forward,
            direction
        );

        if (angle > _FireAngle)
        {
            return;
        }


        // Не разрешаем новый выстрел,
        // пока не наступило время следующей атаки.
        if (Time.time < _NextFireTime)
        {
            return;
        }


        // FireAt запускает подготовку выстрела.
        // true означает, что Launcher принял запрос,
        // но ракета ещё не обязательно выпущена.
        if (_RocketLauncher.FireAt(_Player))
        {
            // Следующий запрос разрешаем только
            // после задержки запуска и перезарядки.
            //
            // Например:
            // LaunchDelay = 0.13
            // FireCooldown = 2
            //
            // Следующая попытка через 2.13 секунды.
            _NextFireTime =
                Time.time +
                _RocketLauncher.LaunchDelay +
                Mathf.Max(0.1f, _FireCooldown);
        }
    }


    // ------------------------------------
    // ВОЗВРАЩЕНИЕ К ПАТРУЛЮ
    // ------------------------------------

    private void ReturnToPatrol()
    {
        _State = AIState.Patrol;

        // Снова включаем EnemyPatrol.
        // Теперь именно он отвечает за
        // перемещение противника между точками.
        if (_Patrol != null)
        {
            _Patrol.enabled = true;
        }
    }
}
