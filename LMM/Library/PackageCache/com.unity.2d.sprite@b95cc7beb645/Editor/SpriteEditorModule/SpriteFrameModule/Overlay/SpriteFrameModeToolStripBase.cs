using System;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Sprites.Overlay
{
    internal abstract class SpriteFrameModeToolStripBase
    {
        Action<SpriteFrameModeToolStripBase> m_ModeToolStripToggled;

        public event Action<SpriteFrameModeToolStripBase> onToolStripToggled
        {
            add => m_ModeToolStripToggled += value;
            remove => m_ModeToolStripToggled -= value;
        }

        /// <summary>
        /// Call to inform the rest of the tool strips that this tool strip has been toggled.
        /// </summary>
        protected void OnToolStripToggled()
        {
            m_ModeToolStripToggled?.Invoke(this);
        }

        /// <summary>
        /// Notify this toolstrip when a toolstrip SpriteFrameModeToolStripBase has been toggled.
        /// </summary>
        /// <param name="value">The tool strip that is toggled.</param>
        public abstract void NotifyModeToolStripToggled(SpriteFrameModeToolStripBase value);

        /// <summary>
        /// Get the UI content for the tool strip.
        /// </summary>
        /// <param name="overlayLayout">Desired overlay layout.</param>
        /// <returns>VisualElements to be added to the tool strip.</returns>
        public abstract VisualElement[] GetUIContent(Layout overlayLayout);

        /// <summary>
        /// Order of the visual element should be placed.
        /// </summary>
        public abstract int order { get; }

        /// <summary>
        /// Called when the SpriteFrame overlay tool strip is ready.
        /// </summary>
        /// <param name="spriteEditorModule">The mode that is associated to this toolstrip as indicated in GetSpriteFrameModeType.</param>
        /// <returns>Returns true if the overlay can be activated. False otherwise.</returns>
        public abstract bool OverlayActivated(ISpriteEditorModuleMode spriteEditorModule);

        /// <summary>
        /// Called when the SpriteFrame overlay toolstrip is deactivated.
        /// </summary>
        public abstract void OverlayDeactivated();

        /// <summary>
        /// The type of the SpriteFrame mode that is associated with this toolstrip.
        /// </summary>
        /// <returns>Type of the mode.</returns>
        public abstract Type GetSpriteFrameModeType();
    }
}
