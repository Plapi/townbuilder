using System;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public static class SimulationClock
    {
        public static float TimeScale = 1f;

        public static bool IsPaused { get; private set; }
        public static float DeltaTime => IsPaused ? 0f : Time.deltaTime * TimeScale;

        public static Action<bool> OnPaused;

        public static void SetPaused(bool paused)
        {
            if (IsPaused != paused)
            {
                IsPaused = paused;
                OnPaused?.Invoke(paused);
            }
        }
    }
}