using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Sprites
{
    class SpriterEditorHintPopup : VisualElement
    {
        const string k_UXMLPath = "Packages/com.unity.2d.sprite/Editor/SpriteEditor/SpriteEditorHintPopup/SpriteEditorHintPopup.uxml";
        Texture2D m_IconTexture;
        string m_HintText = "";
        Image m_IconElement;
        Label m_HintTextElement;

        public  SpriterEditorHintPopup()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UXMLPath).CloneTree(this);
            m_IconElement = this.Q<Image>("icon");
            icon = icon;
            m_HintTextElement = this.Q<Label>("hint");
            hintText = hintText;
        }

        public string hintText
        {
            get => m_HintText;
            set
            {
                m_HintText = value;
                if (m_HintTextElement != null)
                    m_HintTextElement.text = m_HintText;
            }
        }

        public Texture2D icon
        {
            get => m_IconTexture;
            set
            {
                m_IconTexture = value;
                if (m_IconElement != null)
                    m_IconElement.image = m_IconTexture;
            }
        }
    }
}
