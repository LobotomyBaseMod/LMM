using System;
using System.Collections.Generic;

using UnityEngine;

using Codice.Client.Common;

namespace Unity.PlasticSCM.Editor.UI.Tree
{
    internal class EmptyStatePanel : CenteredContentPanel
    {
        internal string Text { get { return mText; } }

        internal EmptyStatePanel(Action repaintAction)
            : base(repaintAction)
        {
        }

        internal bool IsEmpty()
        {
            return string.IsNullOrEmpty(mText);
        }

        internal void UpdateContent(
            string contentText,
            bool bDrawOkIcon = false,
            MultiLinkLabelData multiLinkLabelData = null)
        {
            mText = contentText;
            mbDrawOkIcon = bDrawOkIcon;
            mMultiLinkLabelData = multiLinkLabelData;
        }

        protected override void DrawGUI()
        {
            CenterContent(BuildDrawActions(
                mbDrawOkIcon, mText, mMultiLinkLabelData).ToArray());
        }

        static List<Action> BuildDrawActions(
            bool hasOkIcon,
            string text,
            MultiLinkLabelData multiLinkLabelData)
        {
            List<Action> result = new List<Action>()
            {
                () =>
                {
                    if (hasOkIcon)
                        GUILayout.Label(Images.GetStepOkIcon(), UnityStyles.EmptyState.Icon);

                    GUILayout.Label(text, UnityStyles.EmptyState.Label);
                }
            };

            if (multiLinkLabelData == null)
                return result;

            result.Add(
                () =>
                {
                    DrawTextBlockWithLink.ForMultiLinkLabel(
                        multiLinkLabelData,
                        UnityStyles.EmptyState.LabelForMultiLinkLabel,
                        UnityStyles.EmptyState.LinkForMultiLinkLabel);
                });

            return result;
        }

        bool mbDrawOkIcon;
        string mText;
        MultiLinkLabelData mMultiLinkLabelData;
    }
}
