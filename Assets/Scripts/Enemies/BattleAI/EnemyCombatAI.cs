
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    private enum AIState
    {
        Patrol,
        Alert,
        Attack,
        Search
    }

    [Header("References")]

    [SerializeField] private Enemy _Enemy;
    [SerializeField] private EnemyPatrol _Patrol;
    [SerializeField] private EnemyRocketLauncher _RocketLauncher;

    // Корневой объект игрока Dag-Chan.
    [SerializeField] private Transform _Player;

    // Точка, откуда враг смотрит на игрока.
    [SerializeField] private Transform _EyePoint;


    [Header("Detection")]

    [SerializeField] private float _DetectionDistance = 15f;

    [SerializeField, Range(1f, 360f)]
    private float _ViewAngle = 120f;

    [SerializeField] private LayerMask _SightLayers = ~0;

    // Целимся примерно в центр тела игрока.
    [SerializeField] private float _PlayerAimHeight = 1f;


    [Header("Combat")]

    [SerializeField] private float _AttackDistance = 12f;

    // Задержка между обнаружением и атакой.
    [SerializeField] private float _ReactionTime = 0.6f;

    // Интервал между ракетами.
    [SerializeField] private float _FireCooldown = 2f;

    // Скорость разворота в бою.
    [SerializeField] private float _TurnSpeed = 240f;

    // Максимальная ошибка направления
    // при запуске ракеты.
    [SerializeField] private float _FireAngle = 12f;

    // Через сколько секунд без видимости
    // противник забывает игрока.
    [SerializeField] private float _ForgetTime = 3f;


    private AIState _State = AIState.Patrol;

    private float _ReactionEndTime;
    private float _NextFireTime;
    private float _LastSeenTime;

    private Vector3 _LastSeenPosition;

    // Чтобы отреагировать на первое получение урона
    // только один раз.
    private bool _NoticedDamage;

    private CharacterController _PlayerController;


    private void Start()
    {
        if (_Enemy == null)
            _Enemy = GetComponent<Enemy>();

        if (_Patrol == null)
            _Patrol = GetComponent<EnemyPatrol>();

        if (_RocketLauncher == null)
            _RocketLauncher = GetComponent<EnemyRocketLauncher>();

        if (_Player != null)
        {
            _PlayerController =
                _Player.GetComponentInChildren<CharacterController>();
        }
    }


    private void Update()
    {
        if (_Enemy == null || _Enemy.IsDead || _Player == null)
        {
            return;
        }

        // Реагируем на первое попадание,
        // даже если игрок находится за спиной.
        bool justHit =
            _Enemy.HasBeenHit && !_NoticedDamage;

        if (_Enemy.HasBeenHit)
        {
            _NoticedDamage = true;
        }


        switch (_State)
        {
            case AIState.Patrol:

                if (CanSeePlayer(true) || justHit)
                {
                    EnterAlert();
                }

                break;


            case AIState.Alert:

                if (CanSeePlayer(false))
                {
                    RememberPlayerPosition();
                }

                TurnTowards(_LastSeenPosition);

                if (Time.time >= _ReactionEndTime)
                {
                    _State = CanSeePlayer(false)
                        ? AIState.Attack
                        : AIState.Search;
                }

                break;


            case AIState.Attack:

                if (!CanSeePlayer(false))
                {
                    _State = AIState.Search;
                    break;
                }

                RememberPlayerPosition();
                TurnTowards(_LastSeenPosition);

                TryShoot();

                break;


            case AIState.Search:

                if (CanSeePlayer(false))
                {
                    RememberPlayerPosition();
                    _State = AIState.Attack;
                    break;
                }

                // Смотрим туда, где в последний раз
                // находился игрок.
                TurnTowards(_LastSeenPosition);

                if (Time.time - _LastSeenTime >= _ForgetTime)
                {
                    ReturnToPatrol();
                }

                break;
        }
    }


    private void EnterAlert()
    {
        Debug.Log(
            $"{name} ЗАМЕТИЛ ИГРОКА!",
            this
        );

        _State = AIState.Alert;

        _ReactionEndTime =
            Time.time + Mathf.Max(0f, _ReactionTime);

        RememberPlayerPosition();

        // Останавливаем обычный патруль.
        if (_Patrol != null)
        {
            _Patrol.enabled = false;
        }

        // Отключаем спокойное гавканье.
        _Enemy.SetAlerted();
    }


    private void RememberPlayerPosition()
    {
        _LastSeenTime = Time.time;

        _LastSeenPosition =
            _Player.position + Vector3.up * _PlayerAimHeight;
    }



    private bool CanSeePlayer(bool checkViewAngle)
    {
        if (_Player == null)
        {
            return false;
        }

        // Точка, из которой враг смотрит.
        Vector3 eyePosition = _EyePoint != null
            ? _EyePoint.position
            : transform.position + Vector3.up * 1.2f;

        // Точка, куда смотрит враг.
        Vector3 targetPosition;

        if (_PlayerController != null)
        {
            // Центр настоящего коллайдера игрока.
            targetPosition = _PlayerController.bounds.center;
        }
        else
        {
            // Запасной вариант.
            targetPosition =
                _Player.position + Vector3.up * _PlayerAimHeight;
        }

        // Направление от врага к игроку.
        Vector3 direction = targetPosition - eyePosition;

        float distance = direction.magnitude;

        // Проверяем расстояние.
        if (distance > _DetectionDistance ||
            distance < 0.001f)
        {
            return false;
        }

        // Проверяем угол обзора.
        if (checkViewAngle)
        {
            Vector3 horizontalDirection =
                _Player.position - transform.position;

            horizontalDirection.y = 0f;

            if (horizontalDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Vector3.Angle(
                    transform.forward,
                    horizontalDirection
                );

                if (angle > _ViewAngle / 2f)
                {
                    return false;
                }
            }
        }

        // НОВОЕ!
        // Находим ВСЕ коллайдеры на пути луча.
        RaycastHit[] hits = Physics.RaycastAll(
            eyePosition,
            direction.normalized,
            distance + 0.1f,
            _SightLayers,
            QueryTriggerInteraction.Ignore
        );

        // RaycastAll не гарантирует порядок попаданий.
        // Поэтому сортируем их по расстоянию.
        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            // НОВОЕ!
            // Игнорируем собственные коллайдеры врага.
            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                continue;
            }

            // Если первый посторонний объект — игрок,
            // значит он действительно виден.
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

            // Иначе что-то перекрыло видимость.
            Debug.DrawLine(
                eyePosition,
                hit.point,
                Color.red
            );

            return false;
        }

        // Луч не встретил игрока.
        Debug.DrawLine(
            eyePosition,
            targetPosition,
            Color.yellow
        );

        return false;
    }



    private void TurnTowards(Vector3 position)
    {
        Vector3 direction = position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _TurnSpeed * Time.deltaTime
        );
    }


    private void TryShoot()
    {
        if (_RocketLauncher == null)
        {
            return;
        }

        // Проверяем расстояние до игрока.
        Vector3 direction =
            _Player.position - transform.position;

        direction.y = 0f;

        if (direction.magnitude > _AttackDistance)
        {
            return;
        }

        // Противник должен хотя бы примерно
        // повернуться лицом к игроку.
        float angle = Vector3.Angle(
            transform.forward,
            direction
        );

        if (angle > _FireAngle)
        {
            return;
        }


        // Проверяем время перезарядки.
        if (Time.time < _NextFireTime)
        {
            return;
        }


        // Запускаем ракету.
        if (_RocketLauncher.FireAt(_Player))
        {
            // Задержка подготовки + перезарядка.
            //
            // Например:
            // 1.2 секунды гавканья
            // +
            // 2 секунды перезарядки
            //
            // Следующий выстрел начнёт готовиться
            // через 3.2 секунды.
            _NextFireTime =
                Time.time +
                _RocketLauncher.LaunchDelay +
                Mathf.Max(0.1f, _FireCooldown);
        }
    }


    private void ReturnToPatrol()
    {
        _State = AIState.Patrol;

        // Возобновляем случайное патрулирование.
        if (_Patrol != null)
        {
            _Patrol.enabled = true;
        }
    }
}
