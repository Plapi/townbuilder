using System.Collections.Generic;
using com.Plapamaru.TownCrafter.Factory;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class FactoryEntityMoveSnapper
{
    private const float SNAP_EPSILON = 0.0001f;

    private static readonly HashSet<Entity> PendingEntities = new HashSet<Entity>();
    private static bool _isSnapping;

    static FactoryEntityMoveSnapper()
    {
        Undo.postprocessModifications += OnPostprocessModifications;
    }

    private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
    {
        if (_isSnapping || EditorApplication.isPlayingOrWillChangePlaymode)
            return modifications;

        foreach (var modification in modifications)
        {
            var propertyModification = modification.currentValue;
            if (propertyModification == null || propertyModification.target is not Transform transform)
                continue;

            if (!IsPositionModification(propertyModification.propertyPath))
                continue;

            if (transform.TryGetComponent<Entity>(out var entity))
                PendingEntities.Add(entity);
        }

        if (PendingEntities.Count > 0)
        {
            EditorApplication.delayCall -= SnapPendingEntities;
            EditorApplication.delayCall += SnapPendingEntities;
        }

        return modifications;
    }

    private static bool IsPositionModification(string propertyPath)
    {
        return propertyPath != null && propertyPath.StartsWith("m_LocalPosition");
    }

    private static void SnapPendingEntities()
    {
        if (PendingEntities.Count == 0)
            return;

        _isSnapping = true;

        try
        {
            foreach (var entity in PendingEntities)
                SnapEntity(entity);
        }
        finally
        {
            PendingEntities.Clear();
            _isSnapping = false;
        }
    }

    private static void SnapEntity(Entity entity)
    {
        if (entity == null)
            return;

        var transform = entity.transform;
        var snappedGridPos = FactoryUtils.GetGridPos(transform);
        var originalPosition = transform.position;

        entity.SnapToGrid(snappedGridPos);

        if ((transform.position - originalPosition).sqrMagnitude <= SNAP_EPSILON)
            return;

        EditorUtility.SetDirty(entity);
        EditorUtility.SetDirty(transform);
    }
}
