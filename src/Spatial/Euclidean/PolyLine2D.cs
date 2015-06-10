﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MathNet.Spatial.Euclidean
{
    public class PolyLine2D : IEnumerable<Point2D>
    {
        private List<Point2D> _points;

        public int Count
        {
            get { return this._points.Count; }
        }

        public double Length
        {
            get { return this.GetPolyLineLength(); }
        }

        // Constructors
        public PolyLine2D() : this(Enumerable.Empty<Point2D>())
        {
            
        }

        public PolyLine2D(IEnumerable<Point2D> points)
        {
            this._points = new List<Point2D>(points);
        }

        // Methods
        public Point2D this[int key]
        {
            get { return this._points[key]; }
            set { this._points[key] = value; }
        }

        public Polygon2D ConvexHull()
        {
            throw new NotImplementedException();
        }

        private double GetPolyLineLength()
        {
            double length = 0;
            for (int i = 0; i < this._points.Count - 1; ++i)
                length += this[i].DistanceTo(this[i + 1]);
            return length;
        }

        public Point2D GetPointAtFraction(double fraction)
        {
            return this.GetPointAtLengthFromStart(fraction*this.Length);
        }

        public Point2D GetPointAtLengthFromStart(double lengthFromStart)
        {
            double length = this.Length;
            if (lengthFromStart >= length)
                return this.Last();
            if (lengthFromStart <= 0)
                return this.First();

            double cumulativeLength = 0;
            int i = 0;
            while (true)
            {
                double nextLength = cumulativeLength + this[i].DistanceTo(this[i + 1]);
                if (cumulativeLength <= lengthFromStart && nextLength > lengthFromStart)
                {
                    double leftover = lengthFromStart - cumulativeLength;
                    Vector2D direction = this[i].VectorTo(this[i + 1]).Normalize();
                    return this[i] + (direction * leftover);
                }
                else
                {
                    cumulativeLength = nextLength;
                    i++;
                }
            }
        }

        /// <summary>
        /// Reduce the complexity of a manifold of points represented as an IEnumerable of Point2D objects.
        /// This algorithm goes through each point in the manifold and computes the error that would be introduced
        /// from the original if that point were removed.  Then it removes nonadjacent points to produce a 
        /// reduced size manifold.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private static IEnumerable<Point2D> ReduceComplexitySingleStep(IEnumerable<Point2D> points, double tolerance)
        {
            var manifold = points.ToList();
            var errorByIndex = new double[manifold.Count];

            // At this point we will loop through the list of points (excluding the first and the last) and 
            // examine every adjacent triplet.  The middle point is tested against the segment created by
            // the two end points, and the error that would result in its deletion is computed as the length
            // of the point's projection onto the segment.
            for (int i = 1; i < manifold.Count - 1; i++)
            {
                // TODO: simplify this to remove all of the value copying
                var v0 = manifold[i - 1];
                var v1 = manifold[i];
                var v2 = manifold[i + 1];
                var projected = new Line2D(v0, v2).ClosestPointTo(v1, true);

                double error = v1.VectorTo(projected).Length;
                errorByIndex[i] = error;
            }

            // Now go through the list of errors and remove nonadjacent points with less than the error tolerance
            var thinnedPoints = new List<Point2D>();
            int preserveMe = 0;
            for (int i = 0; i < errorByIndex.Length - 1; i++)
            {
                if (i == preserveMe)
                {
                    thinnedPoints.Add(manifold[i]);
                }
                else
                {
                    if (errorByIndex[i] < tolerance)
                        preserveMe = i + 1;
                    else 
                        thinnedPoints.Add(manifold[i]);
                }
            }
            thinnedPoints.Add(manifold.Last());

            return thinnedPoints;
        }

        /// <summary>
        /// Reduce the complexity of a manifold of points represented as an IEnumerable of Point2D objects by
        /// iteratively removing all nonadjacent points which would each result in an error of less than the
        /// single step tolerance if removed.  Iterate until no further changes are made.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="singleStepTolerance"></param>
        /// <returns></returns>
        public static PolyLine2D ReduceComplexity(IEnumerable<Point2D> points, double singleStepTolerance)
        {
            var manifold = points.ToList();
            var n = manifold.Count;

            manifold = ReduceComplexitySingleStep(manifold, singleStepTolerance).ToList();
            var n1 = manifold.Count;

            while (n1 != n)
            {
                n = n1;
                manifold = ReduceComplexitySingleStep(manifold, singleStepTolerance).ToList();
                n1 = manifold.Count;
            }

            return new PolyLine2D(manifold);
        }

        /// <summary>
        /// Returns the closest point on the polyline to the given point.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Point2D ClosestPointTo(Point2D p)
        {
            double minError = double.MaxValue;
            Point2D closest = new Point2D();

            for (int i = 0; i < this.Count - 1; i++)
            {
                var segment = new Line2D(this[i], this[i + 1]);
                var projected = segment.ClosestPointTo(p, true);
                double error = p.DistanceTo(projected);
                if (error < minError)
                {
                    minError = error;
                    closest = projected;
                }
            }
            return closest;
        }

        // IEnumerable<Point2D>
        public IEnumerator<Point2D> GetEnumerator()
        {
            return this._points.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}