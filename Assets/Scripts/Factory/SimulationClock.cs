using System;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    internal class SimulationClock : Singleton<SimulationClock>
    {
        public bool IsPaused { get; private set; }
        public float DeltaTime => IsPaused ? 0f : Time.deltaTime;

        public Action<bool> OnPaused;

        public void SetPaused(bool paused)
        {
            if (IsPaused != paused)
            {
                IsPaused = paused;
                OnPaused?.Invoke(paused);
            }
        }
    }
}