using System;
using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    [Serializable]
    abstract class UndoObject : ScriptableObject
    {
        static bool s_Undoing = false;
        IUndoSystem m_UndoSystem;

        public static void BeginUndo()
        {
            s_Undoing = true;
        }

        public static void EndUndo()
        {
            s_Undoing = false;
        }

        public static bool undoing => s_Undoing;

        public static T Create<T, T1>(T1 data, IUndoSystem undoSystem) where T : UndoObject<T1> where T1:struct, IEquatable<T1>
        {
            var undoObject = CreateInstance<T>();
            undoObject.hideFlags = HideFlags.HideAndDontSave;
            undoObject.Init(data);
            undoObject.m_UndoSystem = undoSystem;
            return undoObject;
        }

        public static void Dispose(UndoObject undoObject)
        {
            if (undoObject != null)
            {
                undoObject?.Dispose();
                if(!EditorUtility.IsPersistent(undoObject))
                    DestroyImmediate(undoObject);
            }
        }

        protected IUndoSystem undoSystem => m_UndoSystem;
        public abstract void Dispose();
    }

    [Serializable]
    class UndoObject<T> : UndoObject, ISerializationCallbackReceiver where T: IEquatable<T>
    {

        [SerializeField]
        int m_Version = 0;
        int m_CurrentVersion = 0;

        [SerializeField]
        T m_Data;

        public void Init(T data)
        {
            m_Data = data;
        }

        public void SetData(T data, string actionName)
        {
            if (!data.Equals(m_Data))
            {
                if (!undoing)
                {
                    undoSystem.RegisterCompleteObjectUndo(this, actionName);
                    m_Data = data;
                    m_CurrentVersion++;
                    m_Version = m_CurrentVersion;
                }
                else
                {
                    SetData(data);
                }
            }
        }

        public T data => m_Data;

        public void SetData(T data)
        {
            m_Data = data;
        }

        public bool VersionChanged(bool resetVersion)
        {
            bool returnValue = m_CurrentVersion != m_Version;
            if (resetVersion)
                m_CurrentVersion = m_Version;
            return returnValue;
        }

        public override void Dispose()
        {
            undoSystem.ClearUndo(this);
        }

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {

        }
    }
}
