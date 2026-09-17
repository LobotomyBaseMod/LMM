using System;
using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    [Serializable]
    struct EditCapabilityUndoData
    {
        public EditCapability data;
        public EditCapability originalData;
    }

    [Serializable]
    class EditCapabilityUndoObject : UndoObject<EditCapability>
    {
        [SerializeField]
        EditCapability m_OriginalData;

        public EditCapability originalData
        {
            get => m_OriginalData;
            set => m_OriginalData = value;
        }
    }
}
