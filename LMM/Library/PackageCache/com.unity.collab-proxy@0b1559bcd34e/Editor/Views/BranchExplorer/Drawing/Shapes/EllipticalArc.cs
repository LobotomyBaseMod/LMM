// Copyright © 2003-2004, Luc Maisonobe
// 2015 - Alexey Rozanov <thehdotx@gmail.com> - Adaptations for oval center computations
// 2022 - Alexey Rozanov <thehdotx@gmail.com> - Fix for arcs sometimes drawn in inverted order.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with
// or without modification, are permitted provided that
// the following conditions are met:
//
//    Redistributions of source code must retain the
//    above copyright notice, this list of conditions and
//    the following disclaimer.
//    Redistributions in binary form must reproduce the
//    above copyright notice, this list of conditions and
//    the following disclaimer in the documentation
//    and/or other materials provided with the
//    distribution.
//    Neither the names of spaceroots.org, spaceroots.com
//    nor the names of their contributors may be used to
//    endorse or promote products derived from this
//    software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
// CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
// THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
// USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
// USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

// UIElements adaptation by Alexey Rozanov <thehdotx@gmail.com>, 2015.
// I do not mind if anyone would find this adaptation useful, but
// please retain the above disclaimer made by the original class
// author Luc Maisonobe. He worked really hard on this subject, so
// please respect him by at least keeping the above disclaimer intact
// if you use his code.
//
// Commented out some unused values calculations.
// These are not supposed to be removed from the source code,
// as these may be helpful for debugging.
//
// Adapted from http://www.spaceroots.org/documents/ellipse/EllipticalArc.java
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class EllipticalArc
    {
        const float TwoPi = 2 * Mathf.PI;

        /// <summary>
        /// Coefficients for error estimation while using cubic Bezier curves for approximation,
        /// 0 &lt;= b/a &lt;= 0.25
        /// </summary>
        static readonly float[][][] Coeffs3Low = {
            new[]
            {
                new[] {3.85268f, -21.229f, -0.330434f, 0.0127842f},
                new[] {-1.61486f, 0.706564f, 0.225945f, 0.263682f},
                new[] {-0.910164f, 0.388383f, 0.00551445f, 0.00671814f},
                new[] {-0.630184f, 0.192402f, 0.0098871f, 0.0102527f}
            },
            new[]
            {
                new[] {-0.162211f, 9.94329f, 0.13723f, 0.0124084f},
                new[] {-0.253135f, 0.00187735f, 0.0230286f, 0.01264f},
                new[] {-0.0695069f, -0.0437594f, 0.0120636f, 0.0163087f},
                new[] {-0.0328856f, -0.00926032f, -0.00173573f, 0.00527385f}
            }
        };

        /// <summary>
        /// Coefficients for error estimation while using cubic Bezier curves for approximation,
        /// 0.25 &lt;= b/a &lt;= 1
        /// </summary>
        static readonly float[][][] Coeffs3High = {
            new[]
            {
                new[] {0.0899116f, -19.2349f, -4.11711f, 0.183362f},
                new[] {0.138148f, -1.45804f, 1.32044f, 1.38474f},
                new[] {0.230903f, -0.450262f, 0.219963f, 0.414038f},
                new[] {0.0590565f, -0.101062f, 0.0430592f, 0.0204699f}
            },
            new[]
            {
                new[] {0.0164649f, 9.89394f, 0.0919496f, 0.00760802f},
                new[] {0.0191603f, -0.0322058f, 0.0134667f, -0.0825018f},
                new[] {0.0156192f, -0.017535f, 0.00326508f, -0.228157f},
                new[] {-0.0236752f, 0.0405821f, -0.0173086f, 0.176187f}
            }
        };

        /// <summary>
        /// Safety factor to convert the "best" error approximation into a "max bound" error
        /// </summary>
        static readonly float[] Safety3 = { 0.0010f, 4.98f, 0.207f, 0.0067f };

        float mCx;
        float mCy;
        float mA;
        float mB;
        float mCosTheta;
        float mSinTheta;
        float mEta1;
        float mEta2;
        float mX1;
        float mY1;
        float mX2;
        float mY2;
        bool mDrawInOppositeDirection;

        const int MaxDegree = 3;
        const double DefaultFlatness = 0.5;

        internal EllipticalArc(
            float cx, float cy,
            float a, float b,
            float theta,
            float lambda1, float lambda2)
        {
            mCx = cx;
            mCy = cy;
            mA = a;
            mB = b;
            mCosTheta = Mathf.Cos(theta);
            mSinTheta = Mathf.Sin(theta);

            mEta1 = Mathf.Atan2(Mathf.Sin(lambda1) / b, Mathf.Cos(lambda1) / a);
            mEta2 = Mathf.Atan2(Mathf.Sin(lambda2) / b, Mathf.Cos(lambda2) / a);

            // Make sure we have eta1 <= eta2 <= eta1 + 2*PI
            mEta2 -= TwoPi * Mathf.Floor((mEta2 - mEta1) / TwoPi);

            // The preceding correction fails if we have exactly eta2-eta1 == 2*PI
            // it reduces the interval to zero length
            if (lambda2 - lambda1 > Mathf.PI && mEta2 - mEta1 < Mathf.PI)
            {
                mEta2 += TwoPi;
            }

            ComputeEndPoints();
        }

        void ComputeEndPoints()
        {
            float aCosEta1 = mA * Mathf.Cos(mEta1);
            float bSinEta1 = mB * Mathf.Sin(mEta1);
            mX1 = mCx + aCosEta1 * mCosTheta - bSinEta1 * mSinTheta;
            mY1 = mCy + aCosEta1 * mSinTheta + bSinEta1 * mCosTheta;

            float aCosEta2 = mA * Mathf.Cos(mEta2);
            float bSinEta2 = mB * Mathf.Sin(mEta2);
            mX2 = mCx + aCosEta2 * mCosTheta - bSinEta2 * mSinTheta;
            mY2 = mCy + aCosEta2 * mSinTheta + bSinEta2 * mCosTheta;
        }

        // internal for testing
        internal static float RationalFunction(float x, float[] c)
        {
            return (x * (x * c[0] + c[1]) + c[2]) / (x + c[3]);
        }

        float EstimateError(float etaA, float etaB)
        {
            float x = mB / mA;
            float dEta = etaB - etaA;
            float eta = 0.5f * (etaA + etaB);
            float cos2 = Mathf.Cos(2 * eta);
            float cos4 = Mathf.Cos(4 * eta);
            float cos6 = Mathf.Cos(6 * eta);

            float[][][] coeffs = x < 0.25 ? Coeffs3Low : Coeffs3High;

            float c0 = RationalFunction(x, coeffs[0][0]) +
                        cos2 * RationalFunction(x, coeffs[0][1]) +
                        cos4 * RationalFunction(x, coeffs[0][2]) +
                        cos6 * RationalFunction(x, coeffs[0][3]);

            float c1 = RationalFunction(x, coeffs[1][0]) +
                        cos2 * RationalFunction(x, coeffs[1][1]) +
                        cos4 * RationalFunction(x, coeffs[1][2]) +
                        cos6 * RationalFunction(x, coeffs[1][3]);

            return RationalFunction(x, Safety3) * mA * Mathf.Exp(c0 + c1 * dEta);
        }

        void BuildArc(Painter2D painter)
        {
            // Find the number of Bezier curves needed
            bool found = false;
            int n = 1;
            float dEta;
            float etaB;

            while (!found && n < 1024)
            {
                dEta = (mEta2 - mEta1) / n;
                if (dEta <= 0.5 * Mathf.PI)
                {
                    etaB = mEta1;
                    found = true;
                    for (int i = 0; found && i < n; ++i)
                    {
                        float etaA = etaB;
                        etaB += dEta;
                        found = EstimateError(etaA, etaB) <= DefaultFlatness;
                    }
                }
                n = n << 1;
            }

            if (!mDrawInOppositeDirection)
            {
                dEta = (mEta2 - mEta1) / n;
                etaB = mEta1;
            }
            else
            {
                dEta = (mEta1 - mEta2) / n;
                etaB = mEta2;
            }

            float cosEtaB = Mathf.Cos(etaB);
            float sinEtaB = Mathf.Sin(etaB);
            float aCosEtaB = mA * cosEtaB;
            float bSinEtaB = mB * sinEtaB;
            float aSinEtaB = mA * sinEtaB;
            float bCosEtaB = mB * cosEtaB;
            float xB = mCx + aCosEtaB * mCosTheta - bSinEtaB * mSinTheta;
            float yB = mCy + aCosEtaB * mSinTheta + bSinEtaB * mCosTheta;
            float xBDot = -aSinEtaB * mCosTheta - bCosEtaB * mSinTheta;
            float yBDot = -aSinEtaB * mSinTheta + bCosEtaB * mCosTheta;

            float t = Mathf.Tan(0.5f * dEta);
            float alpha = Mathf.Sin(dEta) * (Mathf.Sqrt(4 + 3 * t * t) - 1) / 3;

            for (int i = 0; i < n; ++i)
            {
                float xA = xB;
                float yA = yB;
                float xADot = xBDot;
                float yADot = yBDot;

                etaB += dEta;
                cosEtaB = Mathf.Cos(etaB);
                sinEtaB = Mathf.Sin(etaB);
                aCosEtaB = mA * cosEtaB;
                bSinEtaB = mB * sinEtaB;
                aSinEtaB = mA * sinEtaB;
                bCosEtaB = mB * cosEtaB;
                xB = mCx + aCosEtaB * mCosTheta - bSinEtaB * mSinTheta;
                yB = mCy + aCosEtaB * mSinTheta + bSinEtaB * mCosTheta;
                xBDot = -aSinEtaB * mCosTheta - bCosEtaB * mSinTheta;
                yBDot = -aSinEtaB * mSinTheta + bCosEtaB * mCosTheta;

                // Use cubic Bezier curves
                painter.BezierCurveTo(
                    new Vector2((float)(xA + alpha * xADot), (float)(yA + alpha * yADot)),
                    new Vector2((float)(xB - alpha * xBDot), (float)(yB - alpha * yBDot)),
                    new Vector2((float)xB, (float)yB));
            }
        }

        // internal for testing
        internal static float GetAngle(Vector2 v1, Vector2 v2)
        {
            return Mathf.Atan2(v1.x * v2.y - v2.x * v1.y, v1.x * v2.x + v1.y * v2.y);
        }

        // internal for testing
        internal readonly struct SimpleMatrix
        {
            readonly float mA, mB, mC, mD;

            internal SimpleMatrix(float a, float b, float c, float d)
            {
                mA = a;
                mB = b;
                mC = c;
                mD = d;
            }

            public static Vector2 operator *(SimpleMatrix m, Vector2 p)
            {
                return new Vector2(
                    (m.mA * p.x + m.mB * p.y),
                    (m.mC * p.x + m.mD * p.y));
            }
        }

        /// <summary>
        /// ArcTo Helper - builds an elliptical arc using the endpoint parameterization
        /// </summary>
        internal static void BuildArc(
            Painter2D painter,
            Vector2 p1,
            Vector2 p2,
            Vector2 size,
            float theta,
            bool isLargeArc,
            bool clockwise)
        {
            var orth = new SimpleMatrix(
                Mathf.Cos(theta), Mathf.Sin(theta),
                -Mathf.Sin(theta), Mathf.Cos(theta));

            var rest = new SimpleMatrix(
                Mathf.Cos(theta), -Mathf.Sin(theta),
                Mathf.Sin(theta), Mathf.Cos(theta));

            Vector2 p1S = orth * new Vector2((p1.x - p2.x) / 2, (p1.y - p2.y) / 2);

            float rx = size.x;
            float ry = size.y;
            float rx2 = rx * rx;
            float ry2 = ry * ry;
            float y1S2 = p1S.y * p1S.y;
            float x1S2 = p1S.x * p1S.x;

            float numerator = rx2 * ry2 - rx2 * y1S2 - ry2 * x1S2;
            float denominator = rx2 * y1S2 + ry2 * x1S2;

            if (Mathf.Abs(denominator) < 1e-8)
            {
                painter.LineTo(p2);
                return;
            }

            if ((numerator / denominator) < 0)
            {
                float lambda = x1S2 / rx2 + y1S2 / ry2;
                float lambdaSqrt = Mathf.Sqrt(lambda);
                if (lambda > 1)
                {
                    rx *= lambdaSqrt;
                    ry *= lambdaSqrt;
                    rx2 = rx * rx;
                    ry2 = ry * ry;
                    numerator = rx2 * ry2 - rx2 * y1S2 - ry2 * x1S2;
                    if (numerator < 0)
                        numerator = 0;

                    denominator = rx2 * y1S2 + ry2 * x1S2;
                }
            }

            float multiplier = Mathf.Sqrt(Mathf.Abs(numerator / denominator));
            Vector2 mulVec = new Vector2((float)(rx * p1S.y / ry), (float)(-ry * p1S.x / rx));

            int sign = (clockwise != isLargeArc) ? 1 : -1;

            Vector2 cs = new Vector2(mulVec.x * multiplier * sign, mulVec.y * multiplier * sign);

            Vector2 translation = new Vector2((p1.x + p2.x) / 2, (p1.y + p2.y) / 2);

            Vector2 c = rest * cs + translation;

            var p1NoOffset = orth * (p1 - c);
            var p2NoOffset = orth * (p2 - c);

            var revisedP1 = clockwise ? p1NoOffset : p2NoOffset;
            var revisedP2 = clockwise ? p2NoOffset : p1NoOffset;

            var thetaStart = GetAngle(new Vector2(1, 0), revisedP1);
            var thetaEnd = GetAngle(new Vector2(1, 0), revisedP2);

            var arc = new EllipticalArc(c.x, c.y, rx, ry, theta, thetaStart, thetaEnd);

            float ManhattanDistance(Vector2 a, Vector2 b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

            if (ManhattanDistance(p2, new Vector2((float)arc.mX2, (float)arc.mY2)) >
                ManhattanDistance(p2, new Vector2((float)arc.mX1, (float)arc.mY1)))
            {
                arc.mDrawInOppositeDirection = true;
            }

            arc.BuildArc(painter);
        }
    }
}
