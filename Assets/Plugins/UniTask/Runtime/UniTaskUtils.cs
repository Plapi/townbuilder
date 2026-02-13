using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks
{
    public static class UniTaskUtils
    {
        public static List<(UniTask task, Func<CancellationToken, UniTask> handler)> CreateRaceTasks(
            Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>> raceTasks, 
            CancellationToken cancellationToken)
        {
            var tasks = new List<(UniTask task, Func<CancellationToken, UniTask> handler)>();
            foreach (var task in raceTasks)
                tasks.Add(new ValueTuple<UniTask, Func<CancellationToken, UniTask>>(task.Key.Invoke(cancellationToken), task.Value));
            return tasks;
        }
        
        public static List<(UniTask task, Func<CancellationToken, UniTask<T>> handler)> CreateRaceTasks<T>(
            Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask<T>>> raceTasks, 
            CancellationToken cancellationToken)
        {
            var tasks = new List<(UniTask task, Func<CancellationToken, UniTask<T>> handler)>();
            foreach (var task in raceTasks)
                tasks.Add(new ValueTuple<UniTask, Func<CancellationToken, UniTask<T>>>(task.Key.Invoke(cancellationToken), task.Value));
            return tasks;
        }
    }
}