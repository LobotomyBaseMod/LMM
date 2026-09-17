using System.IO;

using UnityEditor;
using UnityEngine;

using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal static class DrawItemNameBar
    {
        internal static void Draw(Rect rect, ExpandedTreeNode node)
        {
            EditorGUI.DrawRect(
                rect,
                UnityStyles.Colors.FooterBarBackground);

            if (node == null)
                return;

            GUI.Label(
                rect,
                new GUIContent(
                    Path.Combine(
                        Path.GetFileName(node.WkInfo.ClientPath),
                        node.RelativePath).Replace("\\", "/"),
                    DrawExpandedTreeNode.GetIcon(node)),
                UnityStyles.CloudDrive.ItemNameBarLabel);
        }
    }
}
