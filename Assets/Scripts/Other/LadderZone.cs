
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LadderZone : MonoBehaviour
{
    [Header("Лестница")]

    // Скорость автоматического подъёма.
    [SerializeField] private float _ClimbSpeed = 3f;

    public float ClimbSpeed => _ClimbSpeed;


    private void Reset()
    {
        // Лестница не должна быть твёрдой стеной.
        // Нам нужна зона, в которую можно зайти.
        GetComponent<BoxCollider>().isTrigger = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        // Ищем скрипт игрока.
        GO_DagChan player =
            other.GetComponentInParent<GO_DagChan>();

        if (player != null)
        {
            // Сообщаем игроку, что он на лестнице.
            player.EnterLadder(this);
        }
    }


    private void OnTriggerStay(Collider other)
    {
        // На случай, если игрок уже находится
        // внутри зоны при её активации.
        GO_DagChan player =
            other.GetComponentInParent<GO_DagChan>();

        if (player != null)
        {
            player.EnterLadder(this);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        GO_DagChan player =
            other.GetComponentInParent<GO_DagChan>();

        if (player != null)
        {
            // Игрок отошёл от лестницы.
            player.ExitLadder(this);
        }
    }
}
