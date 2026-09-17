using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Sprites.Overlay
{
     [Overlay(typeof(ISpriteEditor), k_OverlayId, k_DisplayName, false, defaultLayout = Layout.VerticalToolbar, maxHeight = 1000, maxWidth = 1000, defaultDockPosition = DockPosition.Top, defaultDockZone = DockZone.LeftColumn)]
    internal class SpriteFrameModeOverlay : Overlays.Overlay, ICreateVerticalToolbar, ICreateHorizontalToolbar, ITransientOverlay
    {
        public const string k_OverlayId = "com.unity.2d.sprite/SpriteFrameModeOverlay";
        const string k_DisplayName = "Mode";
        OverlayToolbar m_Toolbar;
        SpriteFrameModule m_SpriteFrameModule;
        List<SpriteFrameModeToolStripBase> m_AllOverlayToggle =  new List<SpriteFrameModeToolStripBase>();
        List<SpriteFrameModeToolStripBase> m_ActivatedOverlayToggle =  new List<SpriteFrameModeToolStripBase>();
        Layout m_RequestedLayout = Layout.VerticalToolbar;

        public override void OnCreated()
        {
            displayed = false;
            m_Toolbar = new OverlayToolbar();
            CollectInterfaces();
            CreateToolbar(this.layout);
        }

        public override void OnWillBeDestroyed()
        {
            displayed = false;
        }

        void CollectInterfaces()
        {
            if (m_AllOverlayToggle.Count != 0)
                return;
            foreach (var moduleClassType in TypeCache.GetTypesDerivedFrom<SpriteFrameModeToolStripBase>())
            {
                if (moduleClassType.IsAbstract)
                    continue;
                var constructorType = new Type[0];
                // Get the public instance constructor that takes ISpriteEditorModule parameter.
                var constructorInfoObj = moduleClassType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public, null,
                    CallingConventions.HasThis, constructorType, null);
                if (constructorInfoObj != null)
                {
                    try
                    {
                        var newInstance = constructorInfoObj.Invoke(new object[0]) as SpriteFrameModeToolStripBase;
                        if (newInstance != null)
                        {
                            m_AllOverlayToggle.Add(newInstance);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Unable to instantiate Sprite Frame Custom Overlay Provider " + moduleClassType.FullName + ". Exception:" + ex);
                    }
                }
                else
                    Debug.LogWarning(moduleClassType.FullName + " does not have a parameterless constructor");
            }
            m_AllOverlayToggle.Sort((x, y) => x.order.CompareTo(y.order));
        }

        void CreateToolbar(Layout overlayLayout)
        {
            CollectInterfaces();
            m_RequestedLayout = overlayLayout;
            m_Toolbar.Clear();
            foreach (var toggles in m_ActivatedOverlayToggle)
            {
                foreach(var v in toggles.GetUIContent(overlayLayout))
                    m_Toolbar.Add(v);
            }

            displayed = visible;
        }

        public override VisualElement CreatePanelContent()
        {
            CreateToolbar(Layout.Panel);
            return m_Toolbar;
        }

        public bool visible
        {
            get => m_SpriteFrameModule != null;
        }

        public OverlayToolbar CreateVerticalToolbarContent()
        {
            CreateToolbar(Layout.VerticalToolbar);
            return m_Toolbar;
        }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            CreateToolbar(Layout.HorizontalToolbar);
            return m_Toolbar;
        }

        public void Activate(SpriteFrameModule spriteFrameModule)
        {
            m_SpriteFrameModule = spriteFrameModule;
            m_ActivatedOverlayToggle.Clear();
            foreach (var overlayToggle in m_AllOverlayToggle)
            {
                try
                {
                    var mode = overlayToggle.GetSpriteFrameModeType();
                    for (int i = 0; i < m_SpriteFrameModule.modes.Count; ++i)
                    {
                        if (m_SpriteFrameModule.modes[i].GetType() == mode)
                        {
                            if (!overlayToggle.OverlayActivated(m_SpriteFrameModule.modes[i]))
                                continue;
                            overlayToggle.onToolStripToggled += OnOverlayToggleCallback;
                            m_ActivatedOverlayToggle.Add(overlayToggle);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Unable to add {overlayToggle?.GetType()} to the toolbar. Exception: {e}");
                }
            }
            CreateToolbar(m_RequestedLayout);
            displayed = m_ActivatedOverlayToggle.Count > 1;
        }


        public void OnOverlayToggleCallback(SpriteFrameModeToolStripBase arg2)
        {
            m_SpriteFrameModule.OnModeActivate(arg2);
            foreach (var overlayToggle in m_ActivatedOverlayToggle)
            {
                try
                {
                    overlayToggle.NotifyModeToolStripToggled(arg2);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"{overlayToggle?.GetType()}.SpriteFrameModeToggled exception:{e}");
                }
            }
        }

        public SpriteFrameModeToolStripBase GetSpriteFrameModeToolStrip(Type type)
        {
            foreach (var overlayToggle in m_AllOverlayToggle)
            {
                if (overlayToggle.GetType() == type)
                    return overlayToggle;
            }

            return null;
        }

        public void Deactivate()
        {
            m_SpriteFrameModule = null;
            foreach (var overlayToggle in m_AllOverlayToggle)
            {
                overlayToggle.OverlayDeactivated();
                overlayToggle.onToolStripToggled -= OnOverlayToggleCallback;
            }
            displayed = false;
        }
    }
}
