using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine.SceneManagement;
#if UNITY_6000_3_OR_NEWER
using EntityId = UnityEngine.EntityId;
#else
using EntityId = System.Int32;
#endif
#if UNITY_6000_3_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<UnityEngine.EntityId>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<UnityEngine.EntityId>;
#elif UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

public static class EditorCollapseAll
{
    private const BindingFlags INSTANCE_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags STATIC_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    [MenuItem("Assets/Collapse All", priority = 1000)]
    public static void CollApseAll()
    {
        EditorWindow projectWindow = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser").GetField("s_LastInteractedProjectBrowser", STATIC_FLAGS).GetValue(null) as EditorWindow;
        if (projectWindow)
        {
            object assetTree = projectWindow.GetType().GetField("m_AssetTree", INSTANCE_FLAGS).GetValue(projectWindow);
            if (assetTree != null)
                CollapseTreeViewController(projectWindow, assetTree, (TreeViewState)projectWindow.GetType().GetField("m_AssetTreeState", INSTANCE_FLAGS).GetValue(projectWindow));

            object folderTree = projectWindow.GetType().GetField("m_FolderTree", INSTANCE_FLAGS).GetValue(projectWindow);
            if (folderTree != null)
            {
                object treeViewDataSource = folderTree.GetType().GetProperty("data", INSTANCE_FLAGS).GetValue(folderTree, null);
#if UNITY_6000_5_OR_NEWER
                object singletonFavoritesEntityIds = typeof(EditorWindow).Assembly.GetType("UnityEditor.FavoritesEntityIds").BaseType.GetProperty("instance", STATIC_FLAGS).GetValue(null, null);
                MethodInfo filterIdToEntityIdRemapper = singletonFavoritesEntityIds.GetType().GetMethod("GetOrAllocateEntityIdFor", INSTANCE_FLAGS);
                int searchFiltersRootFilterID = (int)typeof(EditorWindow).Assembly.GetType("UnityEditor.SavedSearchFilters").GetMethod("GetRootFilterId", STATIC_FLAGS).Invoke(null, null);
                EntityId searchFiltersRootEntityID = (EntityId)filterIdToEntityIdRemapper.Invoke(singletonFavoritesEntityIds, new object[] { searchFiltersRootFilterID });
#else
                int searchFiltersRootInstanceID = (int)typeof(EditorWindow).Assembly.GetType("UnityEditor.SavedSearchFilters").GetMethod("GetRootInstanceID", STATIC_FLAGS).Invoke(null, null);
#pragma warning disable CS0618 // Type or member is obsolete
                EntityId searchFiltersRootEntityID = searchFiltersRootInstanceID;
#pragma warning restore CS0618 // Type or member is obsolete
#endif
                bool isSearchFilterRootExpanded = (bool)treeViewDataSource.GetType().GetMethod("IsExpanded", INSTANCE_FLAGS, null, new System.Type[] { typeof(EntityId) }, null).Invoke(treeViewDataSource, new object[] { searchFiltersRootEntityID });

                CollapseTreeViewController(projectWindow, folderTree, (TreeViewState)projectWindow.GetType().GetField("m_FolderTreeState", INSTANCE_FLAGS).GetValue(projectWindow), isSearchFilterRootExpanded ? new EntityId[1] { searchFiltersRootEntityID } : null);

                // Preserve Assets and Packages folders' expanded states because they aren't automatically preserved inside ProjectBrowserColumnOneTreeViewDataSource.SetExpandedIDs
                // https://github.com/Unity-Technologies/UnityCsReference/blob/e740821767d2290238ea7954457333f06e952bad/Editor/Mono/ProjectBrowserColumnOne.cs#L408-L420
#if UNITY_6000_3_OR_NEWER
                InternalEditorUtility.expandedProjectWindowItemIds = (EntityId[])treeViewDataSource.GetType().GetMethod("GetExpandedIDs", INSTANCE_FLAGS).Invoke(treeViewDataSource, null);
#else
                InternalEditorUtility.expandedProjectWindowItems = (int[])treeViewDataSource.GetType().GetMethod("GetExpandedIDs", INSTANCE_FLAGS).Invoke(treeViewDataSource, null);
#endif

                TreeViewItem rootItem = (TreeViewItem)treeViewDataSource.GetType().GetField("m_RootItem", INSTANCE_FLAGS).GetValue(treeViewDataSource);
                if (rootItem.hasChildren)
                {
                    foreach (TreeViewItem item in rootItem.children)
                        EditorPrefs.SetBool("ProjectBrowser" + item.displayName, (bool)treeViewDataSource.GetType().GetMethod("IsExpanded", INSTANCE_FLAGS, null, new System.Type[] { typeof(EntityId) }, null).Invoke(treeViewDataSource, new object[] { item.id }));
                }
            }
        }
    }

    [MenuItem("GameObject/Collapse All", priority = 40)]
    private static void CollapseGameObjects(MenuCommand command)
    {
        // This happens when this button is clicked while multiple Objects were selected. In this case,
        // this function will be called once for each selected Object. We don't want that, we want
        // the function to be called only once
        if (command.context)
        {
            EditorApplication.update -= CallCollapseGameObjectsOnce;
            EditorApplication.update += CallCollapseGameObjectsOnce;

            return;
        }

        EditorWindow hierarchyWindow = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow").GetField("s_LastInteractedHierarchy", STATIC_FLAGS).GetValue(null) as EditorWindow;
        if (hierarchyWindow)
        {
            object hierarchyTreeOwner = hierarchyWindow.GetType().GetField("m_SceneHierarchy", INSTANCE_FLAGS).GetValue(hierarchyWindow);
            object hierarchyTree = hierarchyTreeOwner.GetType().GetField("m_TreeView", INSTANCE_FLAGS).GetValue(hierarchyTreeOwner);
            if (hierarchyTree != null)
            {
                List<EntityId> expandedSceneIDs = new(4);
                foreach (string expandedSceneName in (IEnumerable<string>)hierarchyTreeOwner.GetType().GetMethod("GetExpandedSceneNames", INSTANCE_FLAGS).Invoke(hierarchyTreeOwner, null))
                {
                    Scene scene = SceneManager.GetSceneByName(expandedSceneName);
                    if (scene.IsValid())
#if UNITY_6000_3_OR_NEWER
                        expandedSceneIDs.Add((EntityId)typeof(SceneHandle).GetMethod("ToEntityId", INSTANCE_FLAGS).Invoke(scene.handle, null)); // SceneHandle's EntityId is used by SceneHierarchyWindow
#else
                        expandedSceneIDs.Add(scene.GetHashCode()); // GetHashCode returns m_Handle which in turn is used as the Scene's instanceID by SceneHierarchyWindow
#endif
                }

                CollapseTreeViewController(hierarchyWindow, hierarchyTree, (TreeViewState)hierarchyTreeOwner.GetType().GetField("m_TreeViewState", INSTANCE_FLAGS).GetValue(hierarchyTreeOwner), expandedSceneIDs);
            }
        }
    }

    private static void CallCollapseGameObjectsOnce()
    {
        EditorApplication.update -= CallCollapseGameObjectsOnce;
        CollapseGameObjects(new MenuCommand(null));
    }

    private static void CollapseTreeViewController(EditorWindow editorWindow, object treeViewController, TreeViewState treeViewState, IList<EntityId> additionalInstanceIDsToExpand = null)
    {
        object treeViewDataSource = treeViewController.GetType().GetProperty("data", INSTANCE_FLAGS).GetValue(treeViewController, null);
        List<EntityId> treeViewSelectedIDs = new(treeViewState.selectedIDs);
        EntityId[] additionalInstanceIDsToExpandArray;
        if (additionalInstanceIDsToExpand != null && additionalInstanceIDsToExpand.Count > 0)
        {
            treeViewSelectedIDs.AddRange(additionalInstanceIDsToExpand);

            additionalInstanceIDsToExpandArray = new EntityId[additionalInstanceIDsToExpand.Count];
            additionalInstanceIDsToExpand.CopyTo(additionalInstanceIDsToExpandArray, 0);
        }
        else
            additionalInstanceIDsToExpandArray = new EntityId[0];

        treeViewDataSource.GetType().GetMethod("SetExpandedIDs", INSTANCE_FLAGS).Invoke(treeViewDataSource, new object[] { additionalInstanceIDsToExpandArray });
        treeViewDataSource.GetType().GetMethod("RevealItems", INSTANCE_FLAGS).Invoke(treeViewDataSource, new object[] { treeViewSelectedIDs.ToArray() });

        editorWindow.Repaint();
    }

    [MenuItem("CONTEXT/Component/Collapse All", priority = 1400)]
    private static void CollapseComponents(MenuCommand command)
    {
        // Credit: https://forum.unity.com/threads/is-it-possible-to-fold-a-component-from-script-inspector-view.296333/#post-2353538
        ActiveEditorTracker tracker = ActiveEditorTracker.sharedTracker;
        for (int i = 0, length = tracker.activeEditors.Length; i < length; i++)
            tracker.SetVisible(i, 0);

        EditorWindow.focusedWindow.Repaint();
    }
}