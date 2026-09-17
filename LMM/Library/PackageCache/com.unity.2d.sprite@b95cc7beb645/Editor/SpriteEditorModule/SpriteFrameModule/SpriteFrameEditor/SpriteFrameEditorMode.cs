using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.U2D.Sprites.SpriteFrameEditor
{
    [SpriteEditorModuleMode(types: new[] { typeof(SpriteFrameModule) })]
    class SpriteFrameEditorMode  : ISpriteEditorModuleMode
    {
        SpriteFrameModule m_Module;
        Action<ISpriteEditorModuleMode> m_OnModeRequestActivateCallback = _ => { };
        SpriteFrameScenePreview m_SpriteFrameScenePreview;
        ISpriteEditor m_SpriteEditor;

        public SpriteFrameEditorMode()
        { }

        public bool ActivateMode()
        {
            if(m_SpriteFrameScenePreview == null)
                m_SpriteFrameScenePreview = new SpriteFrameScenePreview(spriteEditor);
            spriteEditor.spriteRects = m_Module.GetSpriteRectWorkingData();
            m_Module.EnableSpriteFrameInspector(true);
            m_SpriteFrameScenePreview.ActivateScenePreview();
            return true;
        }

        public void DeactivateMode()
        {
            m_Module?.EnableSpriteFrameInspector(false);
            m_SpriteFrameScenePreview?.DeactivateScenePreview();
        }

        public void OnAddToModule(SpriteEditorModuleModeSupportBase module)
        {
            m_Module = module as SpriteFrameModule;
        }

        public void OnRemoveFromModule(SpriteEditorModuleModeSupportBase module)
        {
            m_Module.EnableSpriteFrameInspector(false);
            m_SpriteFrameScenePreview.DeactivateScenePreview();
            m_SpriteFrameScenePreview = null;
            m_Module = null;
        }

        public event Action<ISpriteEditorModuleMode> onModeRequestActivate
        {
            add => m_OnModeRequestActivateCallback += value;
            remove => m_OnModeRequestActivateCallback -= value;
        }

        public void RequestModeToActivate()
        {
            m_OnModeRequestActivateCallback?.Invoke(this);
        }

        public bool ApplyModeData(bool apply, HashSet<Type> dataProviderTypes)
        {
            return apply;
        }


        public bool CanBeActivated()
        {
            return true;
        }

        public void DoMainGUI()
        {
            m_Module.DoSpriteFrameMainGUI();
        }

        public void DoToolbarGUI(Rect drawArea)
        {
        }

        public void DoPostGUI()
        {
            m_Module.DoSpriteFramePostGUI();
        }

        public ISpriteEditor spriteEditor { get; set; }

        public Type[] GetSupportedModuleTypes()
        {
            return new [] { typeof(SpriteFrameModule) };
        }
    }
}
