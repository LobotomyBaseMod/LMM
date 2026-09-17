using System;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI.Tree
{
    internal abstract class CenteredContentPanel
    {
        internal CenteredContentPanel(Action repaintAction)
        {
            mRepaintAction = repaintAction;
        }

        internal void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.Repaint && mLastValidRect != rect)
            {
                mLastValidRect = rect;
                mRepaintAction();
            }

            GUILayout.BeginArea(mLastValidRect);

            DrawGUI();

            GUILayout.EndArea();
        }

        protected abstract void DrawGUI();

        protected static void CenterContent(params Action[] contents)
        {
            CenterVertical(() =>
            {
                foreach (Action content in contents)
                {
                    CenterHorizontal(() =>
                    {
                        content();
                    });
                }
            });
        }

        static void CenterVertical(Action content)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            content();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        static void CenterHorizontal(Action content)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            content();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        protected readonly Action mRepaintAction;
        Rect mLastValidRect;
    }
}
