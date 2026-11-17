using UnityEngine;

public interface IPickupFactory
{
    GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation);
}
