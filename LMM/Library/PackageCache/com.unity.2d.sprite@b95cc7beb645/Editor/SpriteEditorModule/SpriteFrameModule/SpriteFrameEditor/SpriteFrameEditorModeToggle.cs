using System;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.U2D.Sprites.Overlay;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Sprites.SpriteFrameEditor
{
    internal class SpriteFrameEditorModeToggle : SpriteFrameModeToolStripBase
    {
        EditorToolbarToggle m_Toggle;
        SpriteFrameEditorMode m_SpriteFrameEditorMode;
        public override void NotifyModeToolStripToggled(SpriteFrameModeToolStripBase value)
        {
            if (value != this)
            {
                m_Toggle.SetValueWithoutNotify(false);
            }
            else
            {
                m_Toggle.SetValueWithoutNotify(true);
            }
        }

        public override VisualElement[] GetUIContent(Layout overlayLayout)
        {
            var toolstrip = new VisualElement();
            if (m_Toggle == null)
            {
                m_Toggle = new EditorToolbarToggle()
                {
                    icon = Utilities.LoadIcon("Packages/com.unity.2d.sprite/Editor/Assets", "SpriteFrameToggle@32.png"),
                    tooltip = "Show Sprite Frame Editing Mode",
                };
                m_Toggle.RegisterValueChangedCallback(OnToggleChange);
            }
            toolstrip.Add(m_Toggle);
            if (overlayLayout == Layout.HorizontalToolbar)
                toolstrip.style.flexDirection = FlexDirection.Row;
            else
                toolstrip.style.flexDirection = FlexDirection.Column;
            EditorToolbarUtility.SetupChildrenAsButtonStrip(toolstrip);
            return new[] {toolstrip };
    }

        void OnToggleChange(ChangeEvent<bool> evt)
        {
            if (evt.newValue && m_SpriteFrameEditorMode != null)
            {
                m_SpriteFrameEditorMode.RequestModeToActivate();
                OnToolStripToggled();
            }
            else
            {
                // don't allow to turn off via UI
                m_Toggle.SetValueWithoutNotify(true);
            }
        }

        public override int order => Int32.MinValue;
        public override bool OverlayActivated(ISpriteEditorModuleMode spriteEditor)
        {
            if (spriteEditor is SpriteFrameEditorMode spriteFrameModule)
            {
                m_SpriteFrameEditorMode = spriteFrameModule;
                return true;
            }

            return false;
        }

        public override void OverlayDeactivated()
        {
            m_SpriteFrameEditorMode = null;
        }

        public override Type GetSpriteFrameModeType()
        {
            return typeof(SpriteFrameEditorMode);
        }
    }
}
