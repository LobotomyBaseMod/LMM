using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>Abstract class that is used by systems to encapsulate Sprite data representation. Currently this is used by Sprite Editor Window.</summary>
    [Serializable]
    public class SpriteRect
    {
        [SerializeField]
        string m_Name;

        [SerializeField]
        string m_OriginalName;

        [SerializeField]
        Vector2 m_Pivot;

        [SerializeField]
        SpriteAlignment m_Alignment;

        [SerializeField]
        Vector4 m_Border;

        [SerializeField]
        string m_CustomData;

        [SerializeField]
        Rect m_Rect;

        [SerializeField]
        string m_SpriteID;

        GUID m_GUID;

        /// <summary>The name of the Sprite data.</summary>
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>Vector2value representing the pivot for the Sprite data.</summary>
        public Vector2 pivot
        {
            get { return m_Pivot; }
            set { m_Pivot = value; }
        }

        /// <summary>SpriteAlignment that represents the pivot value for the Sprite data.</summary>
        public SpriteAlignment alignment
        {
            get { return m_Alignment; }
            set { m_Alignment = value; }
        }

        /// <summary>Returns a Vector4 that represents the border of the Sprite data.</summary>
        public Vector4 border
        {
            get { return m_Border; }
            set { m_Border = value; }
        }

        /// <summary>Gets and sets the custom sprite data.</summary>
        public string customData
        {
            get { return m_CustomData; }
            set { m_CustomData = value; }
        }

        /// <summary>Rect value that represents the position and size of the Sprite data.</summary>
        public Rect rect
        {
            get { return m_Rect; }
            set { m_Rect = value; }
        }

        internal string originalName
        {
            get
            {
                if (m_OriginalName == null)
                {
                    m_OriginalName = name;
                }
                return m_OriginalName;
            }

            set { m_OriginalName = value; }
        }

        /// <summary>GUID to uniquely identify the SpriteRect data. This will be populated to Sprite.spriteID to identify the SpriteRect used to generate the Sprite.</summary>
        public GUID spriteID
        {
            get
            {
                ValidateGUID();
                return m_GUID;
            }
            set
            {
                m_GUID = value;
                m_SpriteID = m_GUID.ToString();
                ValidateGUID();
            }
        }

        private void ValidateGUID()
        {
            if (m_GUID.Empty())
            {
                // We can't use ISerializationCallbackReceiver because we will hit into Script serialization errors
                m_GUID = new GUID(m_SpriteID);
                if (m_GUID.Empty())
                {
                    m_GUID = GUID.Generate();
                    m_SpriteID = m_GUID.ToString();
                }
            }
        }

        /// <summary>Helper method to get SpriteRect.spriteID from a SerializedProperty.</summary>
        /// <param name="sp">The SerializedProperty to acquire from.</param>
        /// <returns>GUID for the SpriteRect.</returns>
        public static GUID GetSpriteIDFromSerializedProperty(SerializedProperty sp)
        {
            return new GUID(sp.FindPropertyRelative("m_SpriteID").stringValue);
        }
    }
}
