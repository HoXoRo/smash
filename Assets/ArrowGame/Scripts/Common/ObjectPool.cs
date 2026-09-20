/****************************************************************************
* ScriptType: 主框架
* 请勿修改!!!
****************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;


public class ObjectPool<T> where T : new()
{
    private Stack<T> m_Stack = new Stack<T>();

    private Action<T> m_ActionOnNew;
    private Action<T> m_ActionOnGet;
    private Action<T> m_ActionOnRelease;

    public int CountAll { get; private set; }
    public int CountInactive { get { return m_Stack.Count; } }
    public int CountActive { get { return CountAll - CountInactive; } }

    public ObjectPool()
    {
    }

    public ObjectPool(Action<T> actionOnGet, Action<T> actionOnRelease)
    {
        m_ActionOnGet = actionOnGet;
        m_ActionOnRelease = actionOnRelease;
    }

    public ObjectPool(Action<T> actionOnNew, Action<T> actionOnGet, Action<T> actionOnRelease)
    {
        m_ActionOnNew = actionOnNew;
        m_ActionOnGet = actionOnGet;
        m_ActionOnRelease = actionOnRelease;
    }

    public T Get()
    {
        T element;
        if (m_Stack.Count == 0)
        {
            element = new T();
            CountAll++;

            if (m_ActionOnNew != null)
            {
                m_ActionOnNew(element);
            }
        }
        else
        {
            element = m_Stack.Pop();
        }

        if (m_ActionOnGet != null)
            m_ActionOnGet(element);

        return element;
    }

    public void Release(T element)
    {
        if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element))
            Debug.LogError("[ObjectPool]Error: Trying to destroy object that is already released to pool!");

        if (m_ActionOnRelease != null)
            m_ActionOnRelease(element);

        m_Stack.Push(element);
    }

    public void Clear()
    {
        m_Stack.Clear();
    }

    public void Dispose()
    {
        m_Stack.Clear();
        m_Stack = null;

        m_ActionOnNew = null;
        m_ActionOnGet = null;
        m_ActionOnRelease = null;
    }
}
