using System.Threading;
using BitBenderGames;
using Cysharp.Threading.Tasks;
using com.Plapamaru.Utils;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Game
{
    public static class GameStateUtils
    {
        private const float TAP_MAX_TIME = 0.2f;

        public static async UniTask WaitingForTap(MobileTouchCamera mobileTouchCamera, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0) && Utils.Utils.MouseIsOverUI() == false, cancellationToken: cancellationToken);
                float time = Time.time;
                await UniTask.WaitUntil(() => Time.time > time + TAP_MAX_TIME || Input.GetMouseButtonUp(0) || mobileTouchCamera.HasInteraction,
                    cancellationToken: cancellationToken);
                if (mobileTouchCamera.HasInteraction == false && Time.time <= time + TAP_MAX_TIME)
                    return;
            }
        }
    }
}
