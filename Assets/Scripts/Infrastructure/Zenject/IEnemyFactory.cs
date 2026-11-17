using UnityEngine;

public interface IEnemyFactory
{
    AiAgent Create(AiAgent prefab, Vector3 position, Quaternion rotation);
}
