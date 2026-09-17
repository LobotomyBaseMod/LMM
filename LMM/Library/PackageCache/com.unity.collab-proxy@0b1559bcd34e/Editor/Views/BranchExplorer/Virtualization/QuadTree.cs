using System.Collections.Generic;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal class QuadTree
    {
        internal QuadTree(Rect overallBounds)
        {
            mOverallBounds = overallBounds;
            mQuadrant = new Quadrant(null, overallBounds);
            mIndexedQuadrants = new Dictionary<IVirtualChild, Quadrant>();
        }

        internal void InsertNode(IVirtualChild node)
        {
            mIndexedQuadrants[node] = mQuadrant.InsertNode(node);
        }

        internal IEnumerable<IVirtualChild> GetNodesInside(Rect bounds)
        {
            List<QuadNode> result = new List<QuadNode>();
            mQuadrant.GetIntersectingNodes(result, bounds);

            foreach (QuadNode n in result)
            {
                yield return n.Node;
            }
        }

        /// The canvas is split up into four Quadrants and objects are stored in the quadrant that contains them
        /// and each quadrant is split up into four child Quadrants recurrsively. Objects that overlap more than
        /// one quadrant are stored in the mNodes list for this Quadrant.
        class Quadrant
        {
            internal Quadrant(Quadrant parent, Rect bounds)
            {
                mParent = parent;
                mBounds = bounds;
            }

            internal Quadrant InsertNode(IVirtualChild node)
            {
                Quadrant child = GetQuadrant(node.Bounds);

                if (child != null)
                {
                    return child.InsertNode(node);
                }

                QuadNode n = new QuadNode(node);

                if (mNodes == null)
                {
                    n.Next = n;
                }
                else
                {
                    // link up in circular link list.
                    QuadNode x = mNodes;
                    n.Next = x.Next;
                    x.Next = n;
                }
                mNodes = n;
                return this;
            }

            internal void GetIntersectingNodes(List<QuadNode> nodes, Rect bounds)
            {
                if (bounds == default)
                    return;

                float w = mBounds.width / 2;
                float h = mBounds.height / 2;

                // assumption that the Rect struct is almost as fast as doing the operations
                // manually since Rect is a value type.
                Rect topLeft = new Rect(mBounds.xMin, mBounds.yMin, w, h);
                Rect topRight = new Rect(mBounds.xMin + w, mBounds.yMin, w, h);
                Rect bottomLeft = new Rect(mBounds.xMin, mBounds.yMin + h, w, h);
                Rect bottomRight = new Rect(mBounds.xMin + w, mBounds.yMin + h, w, h);

                // See if any child quadrants completely contain this node.
                if (topLeft.Intersects(bounds) && mTopLeft != null)
                {
                    mTopLeft.GetIntersectingNodes(nodes, bounds);
                }

                if (topRight.Intersects(bounds) && mTopRight != null)
                {
                    mTopRight.GetIntersectingNodes(nodes, bounds);
                }

                if (bottomLeft.Intersects(bounds) && mBottomLeft != null)
                {
                    mBottomLeft.GetIntersectingNodes(nodes, bounds);
                }

                if (bottomRight.Intersects(bounds) && mBottomRight != null)
                {
                    mBottomRight.GetIntersectingNodes(nodes, bounds);
                }

                GetIntersectingNodes(mNodes, nodes, bounds);
            }

            Quadrant GetQuadrant(Rect bounds)
            {
                float w = mBounds.width / 2;
                if (w == 0)
                {
                    w = 1;
                }

                float h = mBounds.height / 2;
                if (h == 0)
                {
                    h = 1;
                }

                // assumption that the Rect struct is almost as fast as doing the operations
                // manually since Rect is a value type.
                Rect topLeft = new Rect(mBounds.xMin, mBounds.yMin, w, h);
                Rect topRight = new Rect(mBounds.xMin + w, mBounds.yMin, w, h);
                Rect bottomLeft = new Rect(mBounds.xMin, mBounds.yMin + h, w, h);
                Rect bottomRight = new Rect(mBounds.xMin + w, mBounds.yMin + h, w, h);

                // See if any child quadrants completely contain this node.
                if (topLeft.Contains(bounds))
                {
                    if (mTopLeft == null)
                    {
                        mTopLeft = new Quadrant(this, topLeft);
                    }
                    return mTopLeft;
                }

                if (topRight.Contains(bounds))
                {
                    if (mTopRight == null)
                    {
                        mTopRight = new Quadrant(this, topRight);
                    }
                    return mTopRight;
                }

                if (bottomLeft.Contains(bounds))
                {
                    if (mBottomLeft == null)
                    {
                        mBottomLeft = new Quadrant(this, bottomLeft);
                    }
                    return mBottomLeft;
                }

                if (bottomRight.Contains(bounds))
                {
                    if (mBottomRight == null)
                    {
                        mBottomRight = new Quadrant(this, bottomRight);
                    }
                    return mBottomRight;
                }

                return null;
            }

            static void GetIntersectingNodes(QuadNode last, List<QuadNode> nodes, Rect bounds)
            {
                if (last == null)
                    return;

                QuadNode n = last;
                do
                {
                    n = n.Next; // first node.
                    if (n.Node.Bounds.Intersects(bounds))
                    {
                        nodes.Add(n);
                    }
                } while (n != last);
            }

            Quadrant mParent;
            Rect mBounds;
            QuadNode mNodes; // nodes that overlap the sub quadrant boundaries.

            // The quadrant is subdivided when nodes are inserted that are
            // completely contained within those subdivisions.
            Quadrant mTopLeft;
            Quadrant mTopRight;
            Quadrant mBottomLeft;
            Quadrant mBottomRight;
        }

        class QuadNode
        {
            internal IVirtualChild Node { get { return mNode; } }

            /// QuadNodes form a linked (circular) list in the Quadrant.
            internal QuadNode Next
            {
                get { return mNext; }
                set { mNext = value; }
            }

            internal QuadNode(IVirtualChild node)
            {
                mNode = node;
            }

            IVirtualChild mNode;
            QuadNode mNext;
        }

        Rect mOverallBounds;
        Quadrant mQuadrant;
        IDictionary<IVirtualChild, Quadrant> mIndexedQuadrants;
    }
}
