
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]

    // Префаб врага, которого будем создавать.
    [SerializeField] private GameObject _EnemyPrefab;


    [Header("Patrol Points")]

    // Родительский объект со всеми точками.
    //
    // Если не назначен, автоматически
    // ищем дочерний объект "PatrolPoints".
    [SerializeField] private Transform _PatrolPointsRoot;


    [Header("Spawn Settings")]

    // Высота появления врага относительно спавнера.
    [SerializeField] private float _SpawnHeight = 3f;

    // Через сколько секунд после смерти врага
    // появится следующий.
    [SerializeField, Min(0f)]
    private float _RespawnDelay = 10f;


    [Header("Player")]

    [SerializeField] private Transform _Player;


    // Собранный список точек патрулирования.
    private List<Transform> _PatrolPoints =
        new List<Transform>();


    private void Start()
    {
        // Проверяем префаб.
        if (_EnemyPrefab == null)
        {
            Debug.LogError(
                "EnemySpawner: не назначен Enemy Prefab!",
                this
            );

            return;
        }


        // Если вручную не назначили объект
        // с точками, пробуем найти его сами.
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


        // Собираем все точки патрулирования.
        CollectPatrolPoints();


        if (_PatrolPoints.Count == 0)
        {
            Debug.LogError(
                "EnemySpawner: нет ни одной точки патрулирования!",
                this
            );

            return;
        }


        // Если игрок не назначен вручную,
        // автоматически ищем его в сцене.
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



        // Запускаем цикл появления врагов.
        StartCoroutine(SpawnRoutine());
    }


    // ------------------------------------
    // СОБИРАЕМ ТОЧКИ ПАТРУЛИРОВАНИЯ
    // ------------------------------------

    private void CollectPatrolPoints()
    {
        _PatrolPoints.Clear();


        // Перебираем непосредственных детей
        // объекта PatrolPoints.
        //
        // Каждый ребёнок считается
        // отдельной точкой патрулирования.
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
        while (true)
        {
            // Создаём врага в позиции спавнера
            // с учётом дополнительной высоты.
            Vector3 spawnPosition =
                transform.position +
                Vector3.up * _SpawnHeight;


            GameObject spawnedEnemy = Instantiate(
                _EnemyPrefab,
                spawnPosition,
                transform.rotation
            );


            // Получаем EnemyCombatAI у созданного врага.
            EnemyCombatAI combatAI =
                spawnedEnemy.GetComponentInChildren<EnemyCombatAI>(true);

            // Передаём ему игрока.
            if (combatAI != null && _Player != null)
            {
                combatAI.SetPlayer(_Player);
            }
            else
            {
                Debug.LogWarning(
                    "EnemySpawner: не удалось назначить Player!",
                    spawnedEnemy
                );
            }



            // Находим EnemyPatrol на созданном враге.
            EnemyPatrol patrol =
                spawnedEnemy.GetComponent<EnemyPatrol>();


            // Находим базовый Enemy.
            //
            // Он нужен, чтобы узнать,
            // жив ли созданный противник.
            Enemy enemy =
                spawnedEnemy.GetComponent<Enemy>();


            // Проверяем необходимые компоненты.
            if (patrol == null || enemy == null)
            {
                Debug.LogError(
                    "EnemySpawner: у префаба нет " +
                    "EnemyPatrol или Enemy!",
                    spawnedEnemy
                );

                Destroy(spawnedEnemy);

                yield break;
            }


            // ==================================
            // НАЗНАЧАЕМ ТОЧКИ ПАТРУЛИРОВАНИЯ
            // ==================================

            // Передаём созданному врагу
            // точки ИМЕННО ЭТОГО спавнера.
            //
            // Instantiate уже вызвал Awake
            // и OnEnable, но Start нового врага
            // ещё не выполнялся.
            patrol.SetPatrolPoints(_PatrolPoints);


            // ==================================
            // ОЖИДАЕМ СМЕРТЬ ВРАГА
            // ==================================

            // Ждём, пока враг жив.
            //
            // Проверяем именно IsDead,
            // потому что после смерти у нас
            // может оставаться труп на карте.
            while (enemy != null && !enemy.IsDead)
            {
                yield return null;
            }


            // ==================================
            // ОЖИДАЕМ РЕСПАВН
            // ==================================

            yield return new WaitForSeconds(
                _RespawnDelay
            );


            // После задержки цикл повторится
            // и создаст нового противника.
        }
    }


    // ------------------------------------
    // ОТОБРАЖЕНИЕ В РЕДАКТОРЕ
    // ------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // Показываем место появления врага.
        Vector3 spawnPosition =
            transform.position +
            Vector3.up * _SpawnHeight;

        Gizmos.DrawWireSphere(
            spawnPosition,
            0.4f
        );


        // Показываем линии от спавнера к точкам.
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
