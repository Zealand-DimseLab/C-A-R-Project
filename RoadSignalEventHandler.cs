using UnityEngine.Events;

public class RoadSignalEventHandler : UnityEvent<RoadSignals>
{
}
public enum RoadSignals
{
    Red,
    Yellow,
    Green
}