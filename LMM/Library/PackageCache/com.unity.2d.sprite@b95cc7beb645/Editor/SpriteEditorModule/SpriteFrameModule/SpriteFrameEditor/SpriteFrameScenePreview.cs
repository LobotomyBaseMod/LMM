using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    class SpriteFrameScenePreview
    {
        class SceneSprite
        {
            public readonly SpriteRenderer spriteRenderer;
            public readonly Sprite originalSprite;
            public readonly Sprite overrideSprite;

            public SceneSprite(SpriteRenderer spriteRenderer, Sprite originalSprite, Sprite overrideSprite)
            {
                this.spriteRenderer = spriteRenderer;
                this.originalSprite = originalSprite;
                this.overrideSprite = overrideSprite;
            }
        }

        SpriteRect m_SelectedSpriteRect;

        ISpriteEditor m_SpriteEditor;
        List<SceneSprite> m_SceneViewSpriteRenderers = new List<SceneSprite>();
        GameObject[] m_GameObjects;

        public SpriteFrameScenePreview(ISpriteEditor spriteEditor)
        {
            m_SpriteEditor = spriteEditor;
        }

        public void ActivateScenePreview()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            m_SpriteEditor.SetScenePreviewCallback(ScenePreviewCallback);
            m_SpriteEditor.GetMainVisualContainer().RegisterCallback<SpriteSelectionChangeEvent>(SelectionChange);
            m_SpriteEditor.GetDataProvider<ISpriteEditorDataProvider>().RegisterDataChangeCallback(OnSpriteRectDataChanged);
            m_SelectedSpriteRect = m_SpriteEditor.selectedSpriteRect;
        }

        void OnSpriteRectDataChanged(ISpriteEditorDataProvider obj)
        {
            PreviewSelected();
        }

        void SelectionChange(SpriteSelectionChangeEvent evt)
        {
            m_SelectedSpriteRect = m_SpriteEditor.selectedSpriteRect;
            PreviewSelected();
        }

        void ScenePreviewCallback(GameObject[] gameObjects)
        {
            m_GameObjects = gameObjects;
            PreviewSelected();
        }

        public void DeactivateScenePreview()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            RestoreSceneViewSpriteRendererSprite();
            m_SpriteEditor.SetScenePreviewCallback(null);
            m_SpriteEditor.GetMainVisualContainer().UnregisterCallback<SpriteSelectionChangeEvent>(SelectionChange);
        }

        void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if(obj == PlayModeStateChange.ExitingEditMode || obj == PlayModeStateChange.ExitingPlayMode)
                RestoreSceneViewSpriteRendererSprite();
        }

        void RestoreSceneViewSpriteRendererSprite()
        {
            if (m_SceneViewSpriteRenderers != null)
            {
                foreach (var sceneSprite in m_SceneViewSpriteRenderers)
                {
                    if (sceneSprite.spriteRenderer == null)
                        continue;

                    var currentSprite = sceneSprite.spriteRenderer.sprite;
                    if (currentSprite != sceneSprite.originalSprite && currentSprite == sceneSprite.overrideSprite)
                        sceneSprite.spriteRenderer.sprite = sceneSprite.originalSprite;
                    if(sceneSprite.overrideSprite != null)
                        UnityEngine.Object.DestroyImmediate(sceneSprite.overrideSprite);
                }

                m_SceneViewSpriteRenderers.Clear();
            }
        }

        void PreviewSelected()
        {
            RestoreSceneViewSpriteRendererSprite();
            var selectedSprite = m_SelectedSpriteRect;
            if (selectedSprite == null)
            {
                return;
            }

            if (m_GameObjects == null || m_GameObjects.Length == 0)
            {
                return;
            }

            var spriteRenderer = m_GameObjects[0].GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }

            if (spriteRenderer.sprite == null)
            {
                return;
            }


            var originalSprite = spriteRenderer.sprite;
            // TODO there is an optimzation here where we cna reuse the same Sprite by overriding the geometry only.
            var textureDataProvider = m_SpriteEditor.GetDataProvider<ITextureDataProvider>();
            var overrideTexture = textureDataProvider.texture;
            if (overrideTexture == null)
            {
                RestoreSceneViewSpriteRendererSprite();
                return;
            }

            textureDataProvider.GetTextureActualWidthAndHeight(out int width, out int height);
            // handle rect when selected rect is bigger than original texture
            var rect = selectedSprite.rect;
            var scale = new Vector2(overrideTexture.width/(float)width, overrideTexture.height/(float)height);//overrideTexture.width / (float)m_Controller.imageSize.x;
            rect = new Rect(rect.x * scale.x, rect.y * scale.y, rect.width * scale.x, rect.height * scale.y);
            var pivot = SpriteEditorUtility.GetPivotValue(selectedSprite.alignment, selectedSprite.pivot);
            var ppu = (selectedSprite.rect.width / originalSprite.rect.width) * originalSprite.pixelsPerUnit;
            var overrideSprite = Sprite.Create(overrideTexture, rect,
                pivot,
                ppu, 0,
                SpriteMeshType.FullRect);

            overrideSprite.name = $"2D-SpritePreview {GUID.Generate().ToString()}";
            m_SceneViewSpriteRenderers.Add(new SceneSprite(spriteRenderer, originalSprite, overrideSprite));
            spriteRenderer.sprite = overrideSprite;
        }
    }
}
