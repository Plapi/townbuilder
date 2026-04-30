using UnityEngine;

internal class SimulationDeltaTime : Singleton<SimulationDeltaTime>
{
    public bool IsPaused { get; private set; }
    public float DeltaTime => IsPaused ? 0f : Time.deltaTime;

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
    }
}