#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CodeEditor.Rendering;
using UnityEngine;

namespace Unity.CodeEditor.Platform
{
    internal sealed class PathFigures : List<PathFigure>
    {
        /// <summary>
        /// Parses the specified path data to a <see cref="PathFigures"/>.
        /// </summary>
        /// <param name="pathData">The s.</param>
        /// <returns></returns>
        internal static PathFigures Parse(string pathData)
        {
            /*
            var pathGeometry = new PathGeometry();
            
            using (var context = new PathGeometryContext(pathGeometry))
            using (var parser = new PathMarkupParser(context))
            {
                parser.Parse(pathData);
            }

            return pathGeometry.Figures!;
            */
            return new PathFigures();
        }
    }

    internal sealed class PathFigure
    {
        internal event EventHandler? SegmentsInvalidated;

        private PathSegments? _segments;

        private IDisposable? _segmentsDisposable;

        private IDisposable? _segmentsPropertiesDisposable;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFigure"/> class.
        /// </summary>
        internal PathFigure()
        {
            Segments = new PathSegments();
        }

        static PathFigure()
        {
        }

        private void OnSegmentsChanged()
        {
            _segmentsDisposable?.Dispose();
            _segmentsPropertiesDisposable?.Dispose();

            _segments?.ForEach(_ => InvalidateSegments());

            //_segmentsPropertiesDisposable = _segments?.TrackItemPropertyChanged(_ => InvalidateSegments());
        }

        private void InvalidateSegments()
        {
            SegmentsInvalidated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is closed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is closed; otherwise, <c>false</c>.
        /// </value>
        internal bool IsClosed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is filled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is filled; otherwise, <c>false</c>.
        /// </value>
        internal bool IsFilled { get; set; }

        /// <summary>
        /// Gets or sets the segments.
        /// </summary>
        /// <value>
        /// The segments.
        /// </value>
        internal PathSegments? Segments
        {
            get { return _segments; }
            set
            {
                _segments = value;
                OnSegmentsChanged();
            }
        }

        /// <summary>
        /// Gets or sets the start point.
        /// </summary>
        /// <value>
        /// The start point.
        /// </value>
        internal Vector2 StartPoint { get; set; }
        
        public override string ToString()
            => FormattableString.Invariant($"M {StartPoint} {string.Join(" ", _segments ?? Enumerable.Empty<PathSegment>())}{(IsClosed ? "Z" : "")}");

        internal void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.BeginFigure(StartPoint, IsFilled);

            if (Segments != null)
            {
                foreach (var segment in Segments)
                {
                    segment.ApplyTo(ctx);
                }
            }

            ctx.EndFigure(IsClosed);
        }
    }

    internal sealed class PathSegments : List<PathSegment>
    {
    }

    internal abstract class PathSegment
    {
        internal abstract void ApplyTo(StreamGeometryContext ctx);
    }

    internal sealed class LineSegment : PathSegment
    {
        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>
        /// The point.
        /// </value>
        internal Vector2 Point { get; set; }

        internal override void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.LineTo(Point);
        }

        public override string ToString()
            => FormattableString.Invariant($"L {Point}");
    }

    internal sealed class ArcSegment : PathSegment
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is large arc.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is large arc; otherwise, <c>false</c>.
        /// </value>
        internal bool IsLargeArc { get; set; }

        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>
        /// The point.
        /// </value>
        internal Vector2 Point { get; set; }

        /// <summary>
        /// Gets or sets the rotation angle.
        /// </summary>
        /// <value>
        /// The rotation angle.
        /// </value>
        internal double RotationAngle { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        internal Vector2 Size { get; set; }

        /// <summary>
        /// Gets or sets the sweep direction.
        /// </summary>
        /// <value>
        /// The sweep direction.
        /// </value>
        internal BackgroundGeometryBuilder.SweepDirection SweepDirection { get; set; }

        internal override void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.ArcTo(Point, Size, RotationAngle, IsLargeArc, SweepDirection);
        }

        public override string ToString()
            => FormattableString.Invariant($"A {Size} {RotationAngle} {(IsLargeArc ? 1 : 0)} {(int)SweepDirection} {Point}");
    }
    /// <summary>
    /// Describes a geometry using drawing commands.
    /// </summary>
    /// <remarks>
    /// This class is used to define the geometry of a <see cref="StreamGeometry"/>. An instance
    /// of <see cref="StreamGeometryContext"/> is obtained by calling
    /// <see cref="StreamGeometry.Open"/>.
    /// </remarks>
    internal class StreamGeometryContext //: IGeometryContext
    {
        private readonly IStreamGeometryContextImpl _impl;

        private Vector2 _currentPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryContext"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        internal StreamGeometryContext(IStreamGeometryContextImpl impl)
        {
            _impl = impl;
        }

        /// <summary>
        /// Sets path's winding rule (default is EvenOdd). You should call this method before any calls to BeginFigure. If you wonder why, ask Direct2D guys about their design decisions.
        /// </summary>
        /// <param name="fillRule"></param>

        internal void SetFillRule(FillRule fillRule)
        {
            _impl.SetFillRule(fillRule);
        }

        /// <summary>
        /// Draws an arc to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        /// <param name="size">The radii of an oval whose perimeter is used to draw the angle.</param>
        /// <param name="rotationAngle">The rotation angle (in radians) of the oval that specifies the curve.</param>
        /// <param name="isLargeArc">true to draw the arc greater than 180 degrees; otherwise, false.</param>
        /// <param name="sweepDirection">
        /// A value that indicates whether the arc is drawn in the Clockwise or Counterclockwise direction.
        /// </param>
        internal void ArcTo(Vector2 point, Vector2 size, double rotationAngle, bool isLargeArc, BackgroundGeometryBuilder.SweepDirection sweepDirection)
        {
            _impl.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection);
            _currentPoint = point;
        }


        /// <summary>
        /// Draws an arc to the specified point using polylines, quadratic or cubic Bezier curves
        /// Significantly more precise when drawing elliptic arcs with extreme width:height ratios.        
        /// </summary>         
        /// <param name="point">The destination point.</param>
        /// <param name="size">The radii of an oval whose perimeter is used to draw the angle.</param>
        /// <param name="rotationAngle">The rotation angle (in radians) of the oval that specifies the curve.</param>
        /// <param name="isLargeArc">true to draw the arc greater than 180 degrees; otherwise, false.</param>
        /// <param name="sweepDirection">
        /// A value that indicates whether the arc is drawn in the Clockwise or Counterclockwise direction.
        /// </param>
        internal void PreciseArcTo(Vector2 point, Vector2 size, double rotationAngle, bool isLargeArc, BackgroundGeometryBuilder.SweepDirection sweepDirection)
        {
            //PreciseEllipticArcHelper.ArcTo(this, _currentPoint, point, size, rotationAngle, isLargeArc, sweepDirection);
        }

        /// <summary>
        /// Begins a new figure.
        /// </summary>
        /// <param name="startPoint">The starting point for the figure.</param>
        /// <param name="isFilled">Whether the figure is filled.</param>
        internal void BeginFigure(Vector2 startPoint, bool isFilled)
        {
            _impl.BeginFigure(startPoint, isFilled);
            _currentPoint = startPoint;
        }

        /// <summary>
        /// Draws a Bezier curve to the specified point.
        /// </summary>
        /// <param name="point1">The first control point used to specify the shape of the curve.</param>
        /// <param name="point2">The second control point used to specify the shape of the curve.</param>
        /// <param name="point3">The destination point for the end of the curve.</param>
        internal void CubicBezierTo(Vector2 point1, Vector2 point2, Vector2 point3)
        {
            _impl.CubicBezierTo(point1, point2, point3);
            _currentPoint = point3;
        }

        /// <summary>
        /// Draws a quadratic Bezier curve to the specified point
        /// </summary>
        /// <param name="control">The control point used to specify the shape of the curve.</param>
        /// <param name="endPoint">The destination point for the end of the curve.</param>
        internal void QuadraticBezierTo(Vector2 control, Vector2 endPoint)
        {
            _impl.QuadraticBezierTo(control, endPoint);
            _currentPoint = endPoint;
        }

        /// <summary>
        /// Draws a line to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        internal void LineTo(Vector2 point)
        {
            _impl.LineTo(point);
            _currentPoint = point;
        }

        /// <summary>
        /// Ends the figure started by <see cref="BeginFigure(Point, bool)"/>.
        /// </summary>
        /// <param name="isClosed">Whether the figure is closed.</param>
        internal void EndFigure(bool isClosed)
        {
            _impl.EndFigure(isClosed);
        }

        /// <summary>
        /// Finishes the drawing session.
        /// </summary>
        internal void Dispose()
        {
            _impl.Dispose();
        }
    }

    internal interface IStreamGeometryContextImpl : IGeometryContext
    {      
    }

    internal interface IGeometryContext : IDisposable
    {
        /// <summary>
        /// Draws an arc to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        /// <param name="size">The radii of an oval whose perimeter is used to draw the angle.</param>
        /// <param name="rotationAngle">The rotation angle (in radians) of the oval that specifies the curve.</param>
        /// <param name="isLargeArc">true to draw the arc greater than 180 degrees; otherwise, false.</param>
        /// <param name="sweepDirection">
        /// A value that indicates whether the arc is drawn in the Clockwise or Counterclockwise direction.
        /// </param>
        void ArcTo(Vector2 point, Vector2 size, double rotationAngle, bool isLargeArc, BackgroundGeometryBuilder.SweepDirection sweepDirection);

        /// <summary>
        /// Begins a new figure.
        /// </summary>
        /// <param name="startVector2">The starting point for the figure.</param>
        /// <param name="isFilled">Whether the figure is filled.</param>
        void BeginFigure(Vector2 startPoint, bool isFilled = true);

        /// <summary>
        /// Draws a Bezier curve to the specified point.
        /// </summary>
        /// <param name="point1">The first control point used to specify the shape of the curve.</param>
        /// <param name="point2">The second control point used to specify the shape of the curve.</param>
        /// <param name="point3">The destination point for the end of the curve.</param>
        void CubicBezierTo(Vector2 point1, Vector2 point2, Vector2 point3);

        /// <summary>
        /// Draws a quadratic Bezier curve to the specified point
        /// </summary>
        /// <param name="control">Control point</param>
        /// <param name="endPoint">DestinationPoint</param>
        void QuadraticBezierTo(Vector2 control, Vector2 endPoint);

        /// <summary>
        /// Draws a line to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        void LineTo(Vector2 point);

        /// <summary>
        /// Ends the figure started by <see cref="BeginFigure(Point, bool)"/>.
        /// </summary>
        /// <param name="isClosed">Whether the figure is closed.</param>
        void EndFigure(bool isClosed);

        /// <summary>
        /// Sets the fill rule.
        /// </summary>
        /// <param name="fillRule">The fill rule.</param>
        void SetFillRule(FillRule fillRule);
    }

    internal enum FillRule
    {
        EvenOdd,
        NonZero
    }
}