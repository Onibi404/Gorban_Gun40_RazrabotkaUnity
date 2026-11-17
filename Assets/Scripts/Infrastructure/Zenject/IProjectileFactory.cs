using UnityEngine;

public interface IProjectileFactory
{
    GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation);
}
