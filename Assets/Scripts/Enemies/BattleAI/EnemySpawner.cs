
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]

    // Префаб врага, который будем создавать.
    [SerializeField] private GameObject _EnemyPrefab;


    [Header("Patrol Points")]

    // Родительский объект точек патрулирования.
    // Если не назначен, ищем дочерний PatrolPoints.
    [SerializeField] private Transform _PatrolPointsRoot;


    [Header("Spawn Settings")]

    // Высота появления относительно позиции спавнера.
    [SerializeField] private float _SpawnHeight = 3f;

    // Задержка перед созданием следующего врага
    // после смерти предыдущего.
    [SerializeField, Min(0f)]
    private float _RespawnDelay = 10f;


    [Header("Player")]

    [SerializeField] private Transform _Player;


    // Список точек патрулирования, который
    // спавнер будет передавать созданным врагам.
    private List<Transform> _PatrolPoints =
        new List<Transform>();


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Start()
    {
        if (_EnemyPrefab == null)
        {
            Debug.LogError(
                "EnemySpawner: не назначен Enemy Prefab!",
                this
            );

            return;
        }


        // Если ссылка не назначена вручную,
        // ищем дочерний объект с именем PatrolPoints.
        //
        // Предполагается такая структура:
        //
        // EnemySpawner
        // └── PatrolPoints
        //     ├── Point_01
        //     └── Point_02
        if (_PatrolPointsRoot == null)
        {
            _PatrolPointsRoot =
                transform.Find("PatrolPoints");
        }


        if (_PatrolPointsRoot == null)
        {
            Debug.LogError(
                "EnemySpawner: не найден объект PatrolPoints!",
                this
            );

            return;
        }


        CollectPatrolPoints();


        if (_PatrolPoints.Count == 0)
        {
            Debug.LogError(
                "EnemySpawner: нет ни одной точки патрулирования!",
                this
            );

            return;
        }


        // Если игрок не назначен в Inspector,
        // ищем его по компоненту GO_DagChan.
        //
        // Эту ссылку затем передадим EnemyCombatAI,
        // чтобы враг понимал, кого преследовать.
        if (_Player == null)
        {
            GO_DagChan player =
                FindFirstObjectByType<GO_DagChan>();

            if (player != null)
            {
                _Player = player.transform;
            }
            else
            {
                Debug.LogError(
                    "EnemySpawner: игрок Dag-Chan не найден!",
                    this
                );
            }
        }


        // Запускаем корутину, которая будет
        // постоянно создавать врагов после их смерти.
        StartCoroutine(SpawnRoutine());
    }


    // ------------------------------------
    // СБОР ТОЧЕК ПАТРУЛИРОВАНИЯ
    // ------------------------------------

    private void CollectPatrolPoints()
    {
        _PatrolPoints.Clear();


        // Перебираем непосредственных потомков
        // объекта PatrolPoints.
        //
        // Сохраняем ссылки на их Transform.
        // Это реальные объекты сцены, а не копии.
        foreach (Transform point in _PatrolPointsRoot)
        {
            _PatrolPoints.Add(point);
        }


        Debug.Log(
            "EnemySpawner: найдено точек: " +
            _PatrolPoints.Count,
            this
        );
    }


    // ------------------------------------
    // ЦИКЛ СПАВНА
    // ------------------------------------

    private IEnumerator SpawnRoutine()
    {
        // Бесконечная корутина:
        // создать врага -> дождаться смерти ->
        // подождать RespawnDelay -> создать нового.
        while (true)
        {
            Vector3 spawnPosition =
                transform.position +
                Vector3.up * _SpawnHeight;


            // Instantiate создаёт отдельный экземпляр
            // префаба со всеми его компонентами.
            GameObject spawnedEnemy = Instantiate(
                _EnemyPrefab,
                spawnPosition,
                transform.rotation
            );


            // ==================================
            // ПЕРЕДАЁМ ВРАГУ ИГРОКА
            // ==================================

            // Ищем EnemyCombatAI даже среди
            // неактивных дочерних объектов (true).
            EnemyCombatAI combatAI =
                spawnedEnemy.GetComponentInChildren<EnemyCombatAI>(true);

            if (combatAI != null && _Player != null)
            {
                // Префаб не хранит обычную ссылку
                // на конкретного игрока сцены.
                //
                // Поэтому после Instantiate
                // передаём её через публичный метод.
                combatAI.SetPlayer(_Player);
            }
            else
            {
                Debug.LogWarning(
                    "EnemySpawner: не удалось назначить Player!",
                    spawnedEnemy
                );
            }


            // ==================================
            // ПОЛУЧАЕМ КОМПОНЕНТЫ ВРАГА
            // ==================================

            EnemyPatrol patrol =
                spawnedEnemy.GetComponent<EnemyPatrol>();

            // Enemy нужен для отслеживания смерти.
            // Сам спавнер не управляет HP противника.
            Enemy enemy =
                spawnedEnemy.GetComponent<Enemy>();


            if (patrol == null || enemy == null)
            {
                Debug.LogError(
                    "EnemySpawner: у префаба нет " +
                    "EnemyPatrol или Enemy!",
                    spawnedEnemy
                );

                Destroy(spawnedEnemy);

                // yield break завершает ВСЮ корутину.
                // При ошибочном префабе респавн остановится.
                yield break;
            }


            // ==================================
            // ПЕРЕДАЁМ ТОЧКИ ПАТРУЛИРОВАНИЯ
            // ==================================

            // Важен жизненный цикл Unity:
            //
            // Instantiate активного префаба
            // вызывает Awake и OnEnable,
            // но Start будет вызван позднее.
            //
            // Поэтому успеваем назначить точки
            // до Start() компонента EnemyPatrol,
            // где выбирается первая цель движения.
            patrol.SetPatrolPoints(_PatrolPoints);


            // ==================================
            // ОЖИДАЕМ СМЕРТЬ ВРАГА
            // ==================================

            // Корутина остаётся здесь, пока
            // созданный враг жив.
            //
            // yield return null — продолжить
            // проверку на следующем кадре.
            //
            // Проверяем IsDead, а не только null,
            // потому что Enemy может оставить
            // труп в сцене после смерти.
            while (enemy != null && !enemy.IsDead)
            {
                yield return null;
            }


            // ==================================
            // ЗАДЕРЖКА РЕСПАВНА
            // ==================================

            // После смерти ждём заданное время.
            // WaitForSeconds учитывает Time.timeScale,
            // поэтому при остановке игры
            // ожидание также приостанавливается.
            yield return new WaitForSeconds(
                _RespawnDelay
            );


            // Возвращаемся к началу while(true)
            // и создаём следующего противника.
        }
    }


    // ------------------------------------
    // ОТОБРАЖЕНИЕ В РЕДАКТОРЕ
    // ------------------------------------

    // Этот метод рисует вспомогательные Gizmos
    // в окне Scene, когда спавнер выделен.
    //
    // Они нужны только для удобства разработки,
    // на сам спавн и игровой мир не влияют.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // Красная сфера показывает точку появления.
        Vector3 spawnPosition =
            transform.position +
            Vector3.up * _SpawnHeight;

        Gizmos.DrawWireSphere(
            spawnPosition,
            0.4f
        );


        Transform pointsRoot = _PatrolPointsRoot;

        if (pointsRoot == null)
        {
            pointsRoot = transform.Find("PatrolPoints");
        }

        if (pointsRoot == null)
        {
            return;
        }


        Gizmos.color = Color.yellow;

        // Показываем точки патрулирования
        // и линии от спавнера до каждой точки.
        foreach (Transform point in pointsRoot)
        {
            Gizmos.DrawWireSphere(
                point.position,
                0.2f
            );

            Gizmos.DrawLine(
                spawnPosition,
                point.position
            );
        }
    }
}
