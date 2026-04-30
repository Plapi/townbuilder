using UnityEngine;

internal class SimulationDeltaTime : Singleton<SimulationDeltaTime>
{
    public bool IsPaused { get; private set; }
    public float DeltaTime { get; private set; }

    protected override void Initialize()
    {
        DeltaTime = Time.deltaTime;
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        DeltaTime = paused ? 0f : Time.deltaTime;
    }
}