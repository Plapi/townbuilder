using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BookmarkStorage : ScriptableSingleton<BookmarkStorage>
{
    [SerializeField]
    private List<Object> _objects = new List<Object>();

    public List<Object> Objects => _objects;

    public void Add(Object obj)
    {
        if (obj == null)
            return;

        if (!_objects.Contains(obj))
        {
            _objects.Add(obj);
            Save(true);
        }
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _objects.Count)
            return;

        _objects.RemoveAt(index);
        Save(true);
    }

    public void InsertAt(int index, Object obj)
    {
        if (obj == null)
            return;

        if (!_objects.Contains(obj))
        {
            _objects.Insert(index, obj);
            Save(true);
        }
    }
}