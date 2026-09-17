using System;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections
{
    internal static class Degrees
    {
        internal static float ToRadians(float angle)
        {
            return (Mathf.PI / 180) * angle;
        }

        internal static float FromRadians(float radians)
        {
            return radians * (180 / Mathf.PI);
        }
    }
}
