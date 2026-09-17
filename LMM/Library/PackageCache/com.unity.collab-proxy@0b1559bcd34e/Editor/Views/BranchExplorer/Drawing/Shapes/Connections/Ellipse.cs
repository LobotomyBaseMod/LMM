using System;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections
{
    internal class Ellipse
    {
        internal Ellipse(float x, float y, float width, float height)
        {
            mBounds = new Rect(x, y, width, height);

            a = Bounds.width / 2;
            b = Bounds.height / 2;

            x0 = Bounds.x + Bounds.width / 2;
            y0 = Bounds.y + Bounds.height / 2;

            ox = (Bounds.width / 2) * KAPPA;
            oy = (Bounds.height / 2) * KAPPA;

            xm = Bounds.x + Bounds.width / 2;
            ym = Bounds.y + Bounds.height / 2;
        }

        internal float X  { get { return Bounds.x; } }
        internal float Y  { get { return Bounds.y; } }
        internal float OX { get { return ox; } }
        internal float OY { get { return oy; } }
        internal float XW { get { return Bounds.xMax; } }
        internal float YH { get { return Bounds.yMax; } }
        internal float XM { get { return xm; } }
        internal float YM { get { return ym; } }

        internal float CalculateXPoint(float py)
        {
            float apow = Mathf.Pow(a, 2);
            float bpow = Mathf.Pow(b, 2);

            float yminusy0pow = Mathf.Pow(py - y0, 2);

            return Mathf.Sqrt((1 - (yminusy0pow / bpow)) * apow) + x0;
        }

        internal float GetSymmetricXPoint(float px)
        {
            return Bounds.x + (Bounds.x + Bounds.width - px);
        }

        internal float CalculateAngleAtPoint(float px, float py)
        {
            float numerator = a * (py - y0);
            float denominator = b * (px - x0);

            float result = Mathf.Atan(numerator / denominator);

            return Degrees.FromRadians(result);
        }

        internal Rect Bounds
        {
            get
            {
                return mBounds;
            }
        }

        const float KAPPA = 0.5522848f;

        Rect mBounds;
        float a;
        float b;
        float x0;
        float y0;

        float ox;
        float oy;
        float xm;
        float ym;
    }
}
