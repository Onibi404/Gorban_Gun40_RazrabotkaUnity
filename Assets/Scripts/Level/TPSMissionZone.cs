using UnityEngine;

/// <summary>Trigger zone that reports entry to the mission state machine.</summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class TPSMissionZone : MonoBehaviour
{
    public enum ZoneId { First, Second, Exit }

    [SerializeField] private TPSMissionDirector _mission;
    [SerializeField] private ZoneId _zone;

    public void Configure(TPSMissionDirector mission, ZoneId zone)
    {
        _mission = mission;
        _zone = zone;
    }

    private void OnTriggerEnter(Collider other)
    {
        Notify(other);
    }

    // A player can be inside a zone when the previous objective completes.
    private void OnTriggerStay(Collider other)
    {
        Notify(other);
    }

    private void Notify(Collider other)
    {
        if (_mission != null && _mission.IsPlayer(other))
            _mission.EnterZone(_zone);
    }
}
