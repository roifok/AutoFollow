using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Zeta.Bot.Pathfinding;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.SNO;
using Point = System.Windows.Point;

namespace AutoFollow.Resources
{
    public class MathUtil
    {
        //public float MinimumDistanceFromPointToLineSegment(Vector3 v, Vector3 w, Vector3 p)
        //{
        //    // Return minimum distance between line segment vw and point p
        //    const float l2 = length_squared(v, w);  // i.e. |w-v|^2 -  avoid a sqrt
        //    if (l2 == 0.0) return distance(p, v);   // v == w case
        //                                            // Consider the line extending the segment, parameterized as v + t (w - v).
        //                                            // We find projection of point p onto the line. 
        //                                            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
        //    const float t = dot(p - v, w - v) / l2;
        //    if (t < 0.0) return distance(p, v);       // Beyond the 'v' end of the segment
        //    else if (t > 1.0) return distance(p, w);  // Beyond the 'w' end of the segment
        //    const vec2 projection = v + t * (w - v);  // Projection falls on the segment
        //    return distance(p, projection);
        //}

        public class LineSegment
        {
            private static float Sqr(float x)
            {
                return x * x;
            }

            private static float DistSqr(Vector2 v, Vector2 w)
            {
                return Sqr(v.X - w.X) + Sqr(v.Y - w.Y);
            }

            private static float DistToSegmentSquared(Vector2 p, Vector2 v, Vector2 w)
            {
                var l2 = DistSqr(v, w);
                if (l2 == 0) return DistSqr(p, v);
                var t = ((p.X - v.X) * (w.X - v.X) + (p.Y - v.Y) * (w.Y - v.Y)) / l2;
                if (t < 0) return DistSqr(p, v);
                if (t > 1) return DistSqr(p, w);
                return DistSqr(p, new Vector2
                {
                    X = v.X + t * (w.X - v.X),
                    Y = v.Y + t * (w.Y - v.Y)
                });
            }

            public static double DistToSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
            {
                return Math.Sqrt(DistToSegmentSquared(point, lineStart, lineEnd));
            }

            public static double DistToSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
            {
                return Math.Sqrt(DistToSegmentSquared(point.ToVector2(), lineStart.ToVector2(), lineEnd.ToVector2()));
            }
        }




        internal Tuple<double, double, double> Quartiles(double[] afVal)
        {
            int iSize = afVal.Length;
            int iMid = iSize / 2; //this is the mid from a zero based index, eg mid of 7 = 3;

            double fQ1 = 0;
            double fQ2 = 0;
            double fQ3 = 0;

            if (iSize % 2 == 0)
            {
                //================ EVEN NUMBER OF POINTS: =====================
                //even between low and high point
                fQ2 = (afVal[iMid - 1] + afVal[iMid]) / 2;

                int iMidMid = iMid / 2;

                //easy split 
                if (iMid % 2 == 0)
                {
                    fQ1 = (afVal[iMidMid - 1] + afVal[iMidMid]) / 2;
                    fQ3 = (afVal[iMid + iMidMid - 1] + afVal[iMid + iMidMid]) / 2;
                }
                else
                {
                    fQ1 = afVal[iMidMid];
                    fQ3 = afVal[iMidMid + iMid];
                }
            }
            else if (iSize == 1)
            {
                //================= special case, sorry ================
                fQ1 = afVal[0];
                fQ2 = afVal[0];
                fQ3 = afVal[0];
            }
            else
            {
                //odd number so the median is just the midpoint in the array.
                fQ2 = afVal[iMid];

                if ((iSize - 1) % 4 == 0)
                {
                    //======================(4n-1) POINTS =========================
                    int n = (iSize - 1) / 4;
                    fQ1 = (afVal[n - 1] * .25) + (afVal[n] * .75);
                    fQ3 = (afVal[3 * n] * .75) + (afVal[3 * n + 1] * .25);
                }
                else if ((iSize - 3) % 4 == 0)
                {
                    //======================(4n-3) POINTS =========================
                    int n = (iSize - 3) / 4;

                    fQ1 = (afVal[n] * .75) + (afVal[n + 1] * .25);
                    fQ3 = (afVal[3 * n + 1] * .25) + (afVal[3 * n + 2] * .75);
                }
            }

            return new Tuple<double, double, double>(fQ1, fQ2, fQ3);
        }

        public static class Clustering
        {
            //https://visualstudiomagazine.com/Articles/2013/12/01/K-Means-Data-Clustering-Using-C.aspx?Page=1

            public static int[] Cluster(double[][] rawData, int numClusters)
            {
                // k-means clustering
                // index of return is tuple ID, cell is cluster ID
                // ex: [2 1 0 0 2 2] means tuple 0 is cluster 2, tuple 1 is cluster 1, tuple 2 is cluster 0, tuple 3 is cluster 0, etc.
                // an alternative clustering DS to save space is to use the .NET BitArray class
                double[][] data = Normalized(rawData); // so large values don't dominate

                bool changed = true; // was there a change in at least one cluster assignment?
                bool success = true; // were all means able to be computed? (no zero-count clusters)

                // init clustering[] to get things started
                // an alternative is to initialize means to randomly selected tuples
                // then the processing loop is
                // loop
                //    update clustering
                //    update means
                // end loop
                int[] clustering = InitClustering(data.Length, numClusters, 0); // semi-random initialization
                double[][] means = Allocate(numClusters, data[0].Length); // small convenience

                int maxCount = data.Length * 10; // sanity check
                int ct = 0;
                while (changed == true && success == true && ct < maxCount)
                {
                    ++ct; // k-means typically converges very quickly
                    success = UpdateMeans(data, clustering, means); // compute new cluster means if possible. no effect if fail
                    changed = UpdateClustering(data, clustering, means); // (re)assign tuples to clusters. no effect if fail
                }
                // consider adding means[][] as an out parameter - the final means could be computed
                // the final means are useful in some scenarios (e.g., discretization and RBF centroids)
                // and even though you can compute final means from final clustering, in some cases it
                // makes sense to return the means (at the expense of some method signature uglinesss)
                //
                // another alternative is to return, as an out parameter, some measure of cluster goodness
                // such as the average distance between cluster means, or the average distance between tuples in 
                // a cluster, or a weighted combination of both
                return clustering;
            }

            private static double[][] Normalized(double[][] rawData)
            {
                // normalize raw data by computing (x - mean) / stddev
                // primary alternative is min-max:
                // v' = (v - min) / (max - min)

                // make a copy of input data
                double[][] result = new double[rawData.Length][];
                for (int i = 0; i < rawData.Length; ++i)
                {
                    result[i] = new double[rawData[i].Length];
                    Array.Copy(rawData[i], result[i], rawData[i].Length);
                }

                for (int j = 0; j < result[0].Length; ++j) // each col
                {
                    double colSum = 0.0;
                    for (int i = 0; i < result.Length; ++i)
                        colSum += result[i][j];
                    double mean = colSum / result.Length;
                    double sum = 0.0;
                    for (int i = 0; i < result.Length; ++i)
                        sum += (result[i][j] - mean) * (result[i][j] - mean);
                    double sd = sum / result.Length;
                    for (int i = 0; i < result.Length; ++i)
                        result[i][j] = (result[i][j] - mean) / sd;
                }
                return result;
            }

            private static int[] InitClustering(int numTuples, int numClusters, int randomSeed)
            {
                // init clustering semi-randomly (at least one tuple in each cluster)
                // consider alternatives, especially k-means++ initialization,
                // or instead of randomly assigning each tuple to a cluster, pick
                // numClusters of the tuples as initial centroids/means then use
                // those means to assign each tuple to an initial cluster.
                Random random = new Random(randomSeed);
                int[] clustering = new int[numTuples];
                for (int i = 0; i < numClusters; ++i) // make sure each cluster has at least one tuple
                    clustering[i] = i;
                for (int i = numClusters; i < clustering.Length; ++i)
                    clustering[i] = random.Next(0, numClusters); // other assignments random
                return clustering;
            }

            private static double[][] Allocate(int numClusters, int numColumns)
            {
                // convenience matrix allocator for Cluster()
                double[][] result = new double[numClusters][];
                for (int k = 0; k < numClusters; ++k)
                    result[k] = new double[numColumns];
                return result;
            }

            private static bool UpdateMeans(double[][] data, int[] clustering, double[][] means)
            {
                // returns false if there is a cluster that has no tuples assigned to it
                // parameter means[][] is really a ref parameter

                // check existing cluster counts
                // can omit this check if InitClustering and UpdateClustering
                // both guarantee at least one tuple in each cluster (usually true)
                int numClusters = means.Length;
                int[] clusterCounts = new int[numClusters];
                for (int i = 0; i < data.Length; ++i)
                {
                    int cluster = clustering[i];
                    ++clusterCounts[cluster];
                }

                for (int k = 0; k < numClusters; ++k)
                    if (clusterCounts[k] == 0)
                        return false; // bad clustering. no change to means[][]

                // update, zero-out means so it can be used as scratch matrix 
                for (int k = 0; k < means.Length; ++k)
                    for (int j = 0; j < means[k].Length; ++j)
                        means[k][j] = 0.0;

                for (int i = 0; i < data.Length; ++i)
                {
                    int cluster = clustering[i];
                    for (int j = 0; j < data[i].Length; ++j)
                        means[cluster][j] += data[i][j]; // accumulate sum
                }

                for (int k = 0; k < means.Length; ++k)
                    for (int j = 0; j < means[k].Length; ++j)
                        means[k][j] /= clusterCounts[k]; // danger of div by 0
                return true;
            }

            private static bool UpdateClustering(double[][] data, int[] clustering, double[][] means)
            {
                // (re)assign each tuple to a cluster (closest mean)
                // returns false if no tuple assignments change OR
                // if the reassignment would result in a clustering where
                // one or more clusters have no tuples.

                int numClusters = means.Length;
                bool changed = false;

                int[] newClustering = new int[clustering.Length]; // proposed result
                Array.Copy(clustering, newClustering, clustering.Length);

                double[] distances = new double[numClusters]; // distances from curr tuple to each mean

                for (int i = 0; i < data.Length; ++i) // walk thru each tuple
                {
                    for (int k = 0; k < numClusters; ++k)
                        distances[k] = Distance(data[i], means[k]); // compute distances from curr tuple to all k means

                    int newClusterID = MinIndex(distances); // find closest mean ID
                    if (newClusterID != newClustering[i])
                    {
                        changed = true;
                        newClustering[i] = newClusterID; // update
                    }
                }

                if (changed == false)
                    return false; // no change so bail and don't update clustering[][]

                // check proposed clustering[] cluster counts
                int[] clusterCounts = new int[numClusters];
                for (int i = 0; i < data.Length; ++i)
                {
                    int cluster = newClustering[i];
                    ++clusterCounts[cluster];
                }

                for (int k = 0; k < numClusters; ++k)
                    if (clusterCounts[k] == 0)
                        return false; // bad clustering. no change to clustering[][]

                Array.Copy(newClustering, clustering, newClustering.Length); // update
                return true; // good clustering and at least one change
            }

            private static double Distance(double[] tuple, double[] mean)
            {
                // Euclidean distance between two vectors for UpdateClustering()
                // consider alternatives such as Manhattan distance
                double sumSquaredDiffs = 0.0;
                for (int j = 0; j < tuple.Length; ++j)
                    sumSquaredDiffs += Math.Pow((tuple[j] - mean[j]), 2);
                return Math.Sqrt(sumSquaredDiffs);
            }

            private static int MinIndex(double[] distances)
            {
                // index of smallest value in array
                // helper for UpdateClustering()
                int indexOfMin = 0;
                double smallDist = distances[0];
                for (int k = 0; k < distances.Length; ++k)
                {
                    if (distances[k] < smallDist)
                    {
                        smallDist = distances[k];
                        indexOfMin = k;
                    }
                }
                return indexOfMin;
            }
        }

        public static Vector3 Centroid(List<Vector3> points)
        {
            var result = points.Aggregate(Vector3.Zero, (current, point) => current + point);
            result /= points.Count();
            return result;
        }

        public static Vector3 CenterOfBoundingRectangle(List<Vector3> points)
        {
            float xmin = 0, xmax = 0, ymin = 0, ymax = 0, zmin = 0, zmax = 0;
            foreach (var point in points)
            {
                if (point.X < xmin) xmin = point.X;
                if (point.X > xmax) xmax = point.X;
                if (point.Y < ymin) ymin = point.Y;
                if (point.Y > ymax) ymax = point.Y;
                if (point.Z < zmin) zmin = point.Z;
                if (point.Z > zmax) zmax = point.Z;
            }
            return new Vector3(xmin + ((xmax - xmin) / 2), ymin + ((ymax - ymin) / 2), zmin + ((zmax - zmin) / 2));
        }

        /// <summary>
        /// Method to compute the centroid of a polygon. This does NOT work for a complex polygon.
        /// </summary>
        /// <param name="poly">points that define the polygon</param>
        /// <returns>centroid point, or PointF.Empty if something wrong</returns>
        public static PointF Centroid(List<PointF> poly)
        {
            float accumulatedArea = 0.0f;
            float centerX = 0.0f;
            float centerY = 0.0f;

            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                float temp = poly[i].X * poly[j].Y - poly[j].X * poly[i].Y;
                accumulatedArea += temp;
                centerX += (poly[i].X + poly[j].X) * temp;
                centerY += (poly[i].Y + poly[j].Y) * temp;
            }

            if (accumulatedArea < 1E-7f)
                return PointF.Empty;  // Avoid division by zero

            accumulatedArea *= 3f;
            return new PointF(centerX / accumulatedArea, centerY / accumulatedArea);
        }

        internal static bool PositionIsInCircle(Vector3 position, Vector3 center, float radius)
        {
            if (center.Distance2DSqr(position) < (Math.Pow((double)radius, (double)radius)))
                return true;
            return false;
        }

        public static IList<T> RandomShuffle<T>(IList<T> list)
        {
            var rng = new Random();
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        public static List<Vector3> GetCirclePoints(int points, double radius, Vector3 center)
        {
            var result = new List<Vector3>();
            var slice = 2 * Math.PI / points;
            for (var i = 0; i < points; i++)
            {
                var angle = slice * i;
                var newX = (int)(center.X + radius * Math.Cos(angle));
                var newY = (int)(center.Y + radius * Math.Sin(angle));

                var newpoint = new Vector3(newX, newY, center.Z);
                result.Add(newpoint);
            }
            return result;
        }

        public static float FixAngleTo360(float angleDegrees)
        {
            var x = Math.IEEERemainder(angleDegrees, 360);
            if (x < 0)
                x += 360;
            return (float)x;
        }


        /// <summary>
        /// Creates a rectangle based on two points.
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <returns>Rectangle</returns>
        public static RectangleF GetRectangle(Vector3 p1, Vector3 p2)
        {
            float top = Math.Min(p1.Y, p2.Y);
            float bottom = Math.Max(p1.Y, p2.Y);
            float left = Math.Min(p1.X, p2.X);
            float right = Math.Max(p1.X, p2.X);
            RectangleF rect = RectangleF.FromLTRB(left, top, right, bottom);
            return rect;
        }

        //Normalizes any number to an arbitrary range 
        //by assuming the range wraps around when going below min or above max 
        public static double Normalise(double value, double start, double end)
        {
            var width = end - start;
            var offsetValue = value - start;   // value relative to 0
            return offsetValue - (Math.Floor((offsetValue / width) * width)) + start;
        }


        public static bool IsPointWithinGeometry(Geometry geom, Vector3 point)
        {
            return geom.FillContains(new Point(point.X, point.Y));
        }

        // Return True if the point is in the polygon.
        public bool PointInPolygon(float x, float y, IList<Vector2> points)
        {
            // Get the angle between the point and the
            // first and last vertices.
            int maxPoint = points.Count - 1;
            var pointsArray = points.ToArray();
            float totalAngle = GetAngle(
                pointsArray[maxPoint].X, pointsArray[maxPoint].Y,
                x, y,
                pointsArray[0].X, pointsArray[0].Y);

            // Add the angles from the point
            // to each other pair of vertices.
            for (int i = 0; i < maxPoint; i++)
            {
                totalAngle += GetAngle(
                    pointsArray[i].X, pointsArray[i].Y,
                    x, y,
                    pointsArray[i + 1].X, pointsArray[i + 1].Y);
            }

            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            return (Math.Abs(totalAngle) > 0.000001);
        }

        // Return the angle ABC.
        // Return a value between PI and -PI.
        // Note that the value is the opposite of what you might
        // expect because Y coordinates increase downward.
        public static float GetAngle(float ax, float ay,
            float bx, float @by, float cx, float cy)
        {
            // Get the dot product.
            float dotProduct = DotProduct(ax, ay, bx, @by, cx, cy);

            // Get the cross product.
            float crossProduct = CrossProductLength(ax, ay, bx, @by, cx, cy);

            // Calculate the angle.
            return (float)Math.Atan2(crossProduct, dotProduct);
        }

        // Return the dot product AB · BC.
        // Note that AB · BC = |AB| * |BC| * Cos(theta).
        private static float DotProduct(float ax, float ay,
            float bx, float @by, float cx, float cy)
        {
            // Get the vectors' coordinates.
            float bAx = ax - bx;
            float bAy = ay - @by;
            float bCx = cx - bx;
            float bCy = cy - @by;

            // Calculate the dot product.
            return (bAx * bCx + bAy * bCy);
        }

        // Return the cross product AB x BC.
        // The cross product is a vector perpendicular to AB
        // and BC having length |AB| * |BC| * Sin(theta) and
        // with direction given by the right-hand rule.
        // For two vectors in the X-Y plane, the result is a
        // vector with X and Y components 0 so the Z component
        // gives the vector's length and direction.
        public static float CrossProductLength(float ax, float ay,
            float bx, float @by, float cx, float cy)
        {
            // Get the vectors' coordinates.
            float bAx = ax - bx;
            float bAy = ay - @by;
            float bCx = cx - bx;
            float bCy = cy - @by;

            // Calculate the Z coordinate of the cross product.
            return (bAx * bCy - bAy * bCx);
        }

        internal static bool PositionIsInsideArc(Vector3 position, Vector3 center, float radius, float rotation, float arcDegrees)
        {
            if (PositionIsInCircle(position, center, radius))
            {
                return GetIsFacingPosition(position, center, rotation, arcDegrees);
            }
            return false;
        }

        internal static bool GetIsFacingPosition(Vector3 position, Vector3 center, float rotation, float arcDegrees)
        {
            var directionVector = GetDirectionVectorFromRotation(rotation);
            if (directionVector != Vector2.Zero)
            {
                Vector3 u = position - center;
                u.Z = 0f;
                Vector3 v = new Vector3(directionVector.X, directionVector.Y, 0f);
                bool result = ((MathEx.ToDegrees(Vector3.AngleBetween(u, v)) <= arcDegrees) ? 1 : 0) != 0;
                return result;
            }
            else
                return false;
        }

        public static double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        internal static Vector2 GetDirectionVectorFromRotation(double rotation)
        {
            return new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));
        }


        #region Angle Finding
        /// <summary>
        /// Find the angle between two vectors. This will not only give the angle difference, but the direction.
        /// For example, it may give you -1 radian, or 1 radian, depending on the direction. Angle given will be the 
        /// angle from the FromVector to the DestVector, in radians.
        /// </summary>
        /// <param name="fromVector">Vector to start at.</param>
        /// <param name="destVector">Destination vector.</param>
        /// <param name="destVectorsRight">Right vector of the destination vector</param>
        /// <returns>Signed angle, in radians</returns>        
        /// <remarks>All three vectors must lie along the same plane.</remarks>
        public static double GetSignedAngleBetween2DVectors(Vector3 fromVector, Vector3 destVector, Vector3 destVectorsRight)
        {
            fromVector.Z = 0;
            destVector.Z = 0;
            destVectorsRight.Z = 0;

            fromVector.Normalize();
            destVector.Normalize();
            destVectorsRight.Normalize();

            float forwardDot = Vector3.Dot(fromVector, destVector);
            float rightDot = Vector3.Dot(fromVector, destVectorsRight);

            // Keep dot in range to prevent rounding errors
            forwardDot = MathEx.Clamp(forwardDot, -1.0f, 1.0f);

            double angleBetween = Math.Acos(forwardDot);

            if (rightDot < 0.0f)
                angleBetween *= -1.0f;

            return angleBetween;
        }
        public float UnsignedAngleBetweenTwoV3(Vector3 v1, Vector3 v2)
        {
            v1.Z = 0;
            v2.Z = 0;
            v1.Normalize();
            v2.Normalize();
            double angle = (float)Math.Acos(Vector3.Dot(v1, v2));
            return (float)angle;
        }
        /// <summary>
        /// Returns the Degree angle of a target location
        /// </summary>
        /// <param name="vStartLocation"></param>
        /// <param name="vTargetLocation"></param>
        /// <returns></returns>
        public static float FindDirectionDegree(Vector3 vStartLocation, Vector3 vTargetLocation)
        {
            return (float)RadianToDegree(NormalizeRadian((float)Math.Atan2(vTargetLocation.Y - vStartLocation.Y, vTargetLocation.X - vStartLocation.X)));
        }
        public static double FindDirectionRadian(Vector3 start, Vector3 end)
        {
            double radian = Math.Atan2(end.Y - start.Y, end.X - start.X);

            if (radian < 0)
            {
                double mod = -radian;
                mod %= Math.PI * 2d;
                mod = -mod + Math.PI * 2d;
                return mod;
            }
            return (radian % (Math.PI * 2d));
        }
        public Vector3 GetDirection(Vector3 origin, Vector3 destination)
        {
            Vector3 direction = destination - origin;
            direction.Normalize();
            return direction;
        }
        #endregion

        public static bool IntersectsPath(Vector3 obstacle, float radius, Vector3 start, Vector3 destination)
        {
            // fake-it to 2D
            obstacle.Z = 0;
            start.Z = 0;
            destination.Z = 0;

            return MathEx.IntersectsPath(obstacle, radius, start, destination);
        }

        public static bool TrinityIntersectsPath(Vector3 start, Vector3 obstacle, Vector3 destination, float distanceToObstacle = -1, float distanceToDestination = -1)
        {
            var toObstacle = distanceToObstacle >= 0 ? distanceToObstacle : start.Distance2D(obstacle);
            var toDestination = distanceToDestination >= 0 ? distanceToDestination : start.Distance2D(destination);

            if (toDestination > 500)
                return false;

            var relativeAngularVariance = GetRelativeAngularVariance(start, obstacle, destination);

            // Angular Variance at 20yd distance
            const int angularVarianceBase = 45;

            // Halve/Double required angle every 20yd; 60* @ 15yd, 11.25* @ 80yd
            var angularVarianceThreshold = Math.Min(angularVarianceBase / (toDestination / 20), 90);

            //Logger.Log("DistToObj={0} DistToDest={1} relativeAV={2} AVThreshold={3} Result={4}", 
            //    toObstacle, toDestination, relativeAngularVariance, angularVarianceThreshold, 
            //    toObstacle < toDestination && relativeAngularVariance <= angularVarianceThreshold);

            if (toObstacle < toDestination)
            {
                // If the angle between lines (A) from start to obstacle and (B) from start to destination
                // are small enough then we know both targets are in the same-ish direction from start.
                if (relativeAngularVariance <= angularVarianceThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        public static Vector2 GetDirectionVector(Vector3 start, Vector3 end)
        {
            return new Vector2(end.X - start.X, end.Y - start.Y);
        }

        public static double Normalize180(double angleA, double angleB)
        {
            //Returns an angle in the range -180 to 180
            double diffangle = (angleA - angleB) + 180d;
            diffangle = (diffangle / 360.0);
            diffangle = ((diffangle - Math.Floor(diffangle)) * 360.0d) - 180d;
            return diffangle;
        }
        public static float NormalizeRadian(float radian)
        {
            if (radian < 0)
            {
                double mod = -radian;
                mod %= Math.PI * 2d;
                mod = -mod + Math.PI * 2d;
                return (float)mod;
            }
            return (float)(radian % (Math.PI * 2d));
        }

        public static double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }


        public static double GetRelativeAngularVariance(Vector3 origin, Vector3 destA, Vector3 destB)
        {
            float fDirectionToTarget = NormalizeRadian((float)Math.Atan2(destA.Y - origin.Y, destA.X - origin.X));
            float fDirectionToObstacle = NormalizeRadian((float)Math.Atan2(destB.Y - origin.Y, destB.X - origin.X));
            return AbsAngularDiffernce(RadianToDegree(fDirectionToTarget), RadianToDegree(fDirectionToObstacle));
        }
        public static double AbsAngularDiffernce(double angleA, double angleB)
        {
            return 180d - Math.Abs(180d - Math.Abs(angleA - angleB));
        }

        public static string GetHeadingToPoint(Vector3 targetPoint)
        {
            return GetHeading(FindDirectionDegree(ZetaDia.Me.Position, targetPoint));
        }

        /// <summary>
        /// Gets string heading NE,S,NE etc
        /// </summary>
        /// <param name="headingDegrees">heading in degrees</param>
        /// <returns></returns>
        public static string GetHeading(float headingDegrees)
        {
            var directions = new string[] {
              //"n", "ne", "e", "se", "s", "sw", "w", "nw", "n"
                "s", "se", "e", "ne", "n", "nw", "w", "sw", "s"
            };

            var index = (((int)headingDegrees) + 23) / 45;
            return directions[index].ToUpper();
        }


        /// <summary>
        /// Gets the center of a given Navigation Zone
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        internal static Vector3 GetNavZoneCenter(NavZone zone)
        {
            float x = zone.ZoneMin.X + ((zone.ZoneMax.X - zone.ZoneMin.X) / 2);
            float y = zone.ZoneMin.Y + ((zone.ZoneMax.Y - zone.ZoneMin.Y) / 2);
            return new Vector3(x, y, 0);
        }

        /// <summary>
        /// Gets the center of a given Navigation Cell
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        internal static Vector3 GetNavCellCenter(NavCell cell, NavZone zone)
        {
            return GetNavCellCenter(cell.Min, cell.Max, zone);
        }

        //private bool IsTouching(Vector3 p1, Vector3 p2)
        //{
        //    if (p1.X + p1.Width < p2.X)
        //        return false;
        //    if (p2.X + p2.Width < p1.X)
        //        return false;
        //    if (p1.Y + p1.Height < p2.Y)
        //        return false;
        //    if (p2.Y + p2.Height < p1.Y)
        //        return false;
        //    return true;
        //}

        /// <summary>
        /// Gets the center of a given box with min/max, adjusted for the Navigation Zone
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        internal static Vector3 GetNavCellCenter(Vector3 min, Vector3 max, NavZone zone)
        {
            float x = zone.ZoneMin.X + min.X + ((max.X - min.X) / 2);
            float y = zone.ZoneMin.Y + min.Y + ((max.Y - min.Y) / 2);
            float z = min.Z + ((max.Z - min.Z) / 2);

            return new Vector3(x, y, z);
        }


        public static Vector3 GetEstimatedPosition(Vector3 startPosition, double headingRadians, double time, double targetVelocity)
        {
            double x = startPosition.X + targetVelocity * time * Math.Sin(headingRadians);
            double y = startPosition.Y + targetVelocity * time * Math.Cos(headingRadians);
            return new Vector3((float)x, (float)y, 0);
        }

        /// <summary>
        /// Utility for Predictive Firing 
        /// </summary>
        public class Intercept
        {

            /*
                Intercept intercept = new Intercept();

                intercept.calculate (
                        ourRobotPositionX,
                        ourRobotPositionY,
                        currentTargetPositionX,
                        currentTargetPositionY,
                        curentTargetHeading_deg,
                        currentTargetVelocity,
                        bulletPower,
                        0 // Angular velocity
                );

                // Helper function that converts any angle into  
                // an angle between +180 and -180 degrees.
                    double turnAngle = normalRelativeAngle(intercept.bulletHeading_deg - robot.getGunHeading());

                // Move gun to target angle
                    robot.setTurnGunRight (turnAngle);

                    if (Math.abs (turnAngle) 
                        <= intercept.angleThreshold) {
                  // Ensure that the gun is pointing at the correct angle
                  if ((intercept.impactPoint.x > 0)
                                && (intercept.impactPoint.x < getBattleFieldWidth())
                                && (intercept.impactPoint.y > 0)
                                && (intercept.impactPoint.y < getBattleFieldHeight())) {
                    // Ensure that the predicted impact point is within 
                            // the battlefield
                            fire(bulletPower);
                        }
                    }
                }                          
             */

            public Vector2 ImpactPoint = new Vector2(0, 0);
            public double BulletHeadingDeg;

            protected Vector2 BulletStartingPoint = new Vector2();
            protected Vector2 TargetStartingPoint = new Vector2();
            public double TargetHeading;
            public double TargetVelocity;
            public double BulletPower;
            public double AngleThreshold;
            public double distance;

            protected double ImpactTime;
            protected double AngularVelocityRadPerSec;

            public void Calculate(
                    // Initial bullet position x coordinate 
                    double xb,
                    // Initial bullet position y coordinate
                    double yb,
                    // Initial target position x coordinate
                    double xt,
                    // Initial target position y coordinate
                    double yt,
                    // Target heading
                    double tHeading,
                    // Target velocity
                    double vt,
                    // Power of the bullet that we will be firing
                    double bPower,
                    // Angular velocity of the target
                    double angularVelocityDegPerSec,
                    // target object's radius
                    double targetsRadius
            )
            {
                AngularVelocityRadPerSec = DegreeToRadian(angularVelocityDegPerSec);

                BulletStartingPoint = new Vector2((float)xb, (float)yb);
                TargetStartingPoint = new Vector2((float)xt, (float)yt);

                TargetHeading = tHeading;
                TargetVelocity = vt;
                BulletPower = bPower;
                double vb = 20 - 3 * BulletPower;

                // Start with initial guesses at 10 and 20 ticks
                ImpactTime = GetImpactTime(10, 20, 0.01);
                ImpactPoint = GetEstimatedPosition(ImpactTime);

                double dX = (ImpactPoint.X - BulletStartingPoint.X);
                double dY = (ImpactPoint.Y - BulletStartingPoint.Y);

                distance = Math.Sqrt(dX * dX + dY * dY);

                BulletHeadingDeg = RadianToDegree(Math.Atan2(dX, dY));
                AngleThreshold = RadianToDegree(Math.Atan(targetsRadius / distance));
            }

            protected Vector2 GetEstimatedPosition(double time)
            {
                double x = TargetStartingPoint.X + TargetVelocity * time * Math.Sin(DegreeToRadian(TargetHeading));
                double y = TargetStartingPoint.Y + TargetVelocity * time * Math.Cos(DegreeToRadian(TargetHeading));
                return new Vector2((float)x, (float)y);
            }

            private double F(double time)
            {

                double vb = 20 - 3 * BulletPower;

                Vector2 targetPosition = GetEstimatedPosition(time);
                double dX = (targetPosition.X - BulletStartingPoint.X);
                double dY = (targetPosition.Y - BulletStartingPoint.Y);

                return Math.Sqrt(dX * dX + dY * dY) - vb * time;
            }

            private double GetImpactTime(double t0,
                    double t1, double accuracy)
            {

                double x = t1;
                double lastX = t0;
                int iterationCount = 0;
                double lastfX = F(lastX);

                while ((Math.Abs(x - lastX) >= accuracy)
                        && (iterationCount < 15))
                {

                    iterationCount++;
                    double fX = F(x);

                    if ((fX - lastfX) == 0.0)
                    {
                        break;
                    }

                    double nextX = x - fX * (x - lastX) / (fX - lastfX);
                    lastX = x;
                    x = nextX;
                    lastfX = fX;
                }

                return x;
            }

        }

        public class CircularIntercept : Intercept
        {

            protected new Vector2 GetEstimatedPosition(double time)
            {
                if (Math.Abs(AngularVelocityRadPerSec)
                        <= DegreeToRadian(0.1))
                {
                    return base.GetEstimatedPosition(time);
                }

                double initialTargetHeading = DegreeToRadian(TargetHeading);
                double finalTargetHeading = initialTargetHeading
                        + AngularVelocityRadPerSec * time;
                double x = TargetStartingPoint.X - TargetVelocity
                        / AngularVelocityRadPerSec * (Math.Cos(finalTargetHeading)
                        - Math.Cos(initialTargetHeading));
                double y = TargetStartingPoint.Y - TargetVelocity
                        / AngularVelocityRadPerSec
                        * (Math.Sin(initialTargetHeading)
                        - Math.Sin(finalTargetHeading));

                return new Vector2((float)x, (float)y);
            }

        }

        public static float SnapAngle(float rotationToSnap)
        {
            if (rotationToSnap == 0)
                return 0.0f;

            var modRot = rotationToSnap % PiOver4;

            double finalRot;

            if (modRot < RoundedPiOver8)
            {
                if (modRot < -RoundedPiOver8)
                    finalRot = (rotationToSnap + -PiOver4 - modRot);
                else
                    finalRot = (rotationToSnap - modRot);
            }
            else
                finalRot = (rotationToSnap + PiOver4 - modRot);

            return (float)Math.Round(finalRot, 3);
        }


        public const double PiOver8 = (double)(Math.PI / 8.0);
        public static double RoundedPiOver8 = Math.Round(PiOver8, 6, MidpointRounding.AwayFromZero);


        public static double Barycentric(double value1, double value2, double value3, double amount1, double amount2)
        {
            return value1 + (value2 - value1) * amount1 + (value3 - value1) * amount2;
        }

        public static double CatmullRom(double value1, double value2, double value3, double value4, double amount)
        {
            // Using formula from http://www.mvps.org/directx/articles/catmull/
            // Internally using doubles not to lose precission
            double amountSquared = amount * amount;
            double amountCubed = amountSquared * amount;
            return (double)(0.5 * (2.0 * value2 +
                (value3 - value1) * amount +
                (2.0 * value1 - 5.0 * value2 + 4.0 * value3 - value4) * amountSquared +
                (3.0 * value2 - value1 - 3.0 * value3 + value4) * amountCubed));
        }

        public static double Clamp(double value, double min, double max)
        {
            // First we check to see if we're greater than the max
            value = (value > max) ? max : value;

            // Then we check to see if we're less than the min.
            value = (value < min) ? min : value;

            // There's no check to see if min > max.
            return value;
        }

        public static float ToRadians(float degrees)
        {
            // This method uses double precission internally,
            // though it returns single double
            // Factor = pi / 180
            return (float)(degrees * 0.017453292519943295769236907684886);
        }

        public static float WrapAngle(float angle)
        {
            angle = (float)Math.IEEERemainder((float)angle, 6.2831854820251465);
            if (angle <= -3.14159274f)
            {
                angle += 6.28318548f;
            }
            else
            {
                if (angle > 3.14159274f)
                {
                    angle -= 6.28318548f;
                }
            }
            return angle;
        }

        public static bool IsPowerOfTwo(int value)
        {
            return (value > 0) && ((value & (value - 1)) == 0);
        }


        public const float E = 2.718282f;
        public const float Log2E = 1.442695f;
        public const float Log10E = 0.4342945f;
        public const float Pi = 3.141593f;
        public const float TwoPi = 6.283185f;
        public const float PiOver2 = 1.570796f;
        public const float PiOver4 = 0.7853982f;

        public static float ToDegrees(float radians)
        {
            return radians * 57.29578f;
        }

        public static float Distance(float value1, float value2)
        {
            return Math.Abs(value1 - value2);
        }

        public static float Min(float value1, float value2)
        {
            return Math.Min(value1, value2);
        }

        public static float Max(float value1, float value2)
        {
            return Math.Max(value1, value2);
        }

        public static float Clamp(float value, float min, float max)
        {
            value = (double)value > (double)max ? max : value;
            value = (double)value < (double)min ? min : value;
            return value;
        }

        public static float Lerp(float value1, float value2, float amount)
        {
            return value1 + (value2 - value1) * amount;
        }

        public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2)
        {
            return (float)((double)value1 + (double)amount1 * ((double)value2 - (double)value1) + (double)amount2 * ((double)value3 - (double)value1));
        }

        public static float SmoothStep(float value1, float value2, float amount)
        {
            float num = Clamp(amount, 0.0f, 1f);
            return MathHelper.Lerp(value1, value2, (float)((double)num * (double)num * (3.0 - 2.0 * (double)num)));
        }

        public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            return (float)(0.5 * (2.0 * (double)value2 + (-(double)value1 + (double)value3) * (double)amount + (2.0 * (double)value1 - 5.0 * (double)value2 + 4.0 * (double)value3 - (double)value4) * (double)num1 + (-(double)value1 + 3.0 * (double)value2 - 3.0 * (double)value3 + (double)value4) * (double)num2));
        }

        public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
        {
            float num1 = amount;
            float num2 = num1 * num1;
            float num3 = num1 * num2;
            float num4 = (float)(2.0 * (double)num3 - 3.0 * (double)num2 + 1.0);
            float num5 = (float)(-2.0 * (double)num3 + 3.0 * (double)num2);
            float num6 = num3 - 2f * num2 + num1;
            float num7 = num3 - num2;
            return (float)((double)value1 * (double)num4 + (double)value2 * (double)num5 + (double)tangent1 * (double)num6 + (double)tangent2 * (double)num7);
        }

        public static float Flip(float value, float origin)
        {
            return origin - (value - origin);
        }

        public static Vector2 GetNextLinePoint(int x, int y, int x2, int y2)
        {
            //http://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm

            int w = x2 - x;
            int h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1;
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }

            return new Vector2(x, y);
        }

    }

    public static class VectorExt
    {
        private const double DegToRad = Math.PI / 180;

        public static Vector2 Rotate(this Vector2 v, double degrees)
        {
            return v.RotateRadians(degrees * DegToRad);
        }

        public static Vector2 RotateRadians(this Vector2 v, double radians)
        {
            var ca = Math.Cos(radians);
            var sa = Math.Sin(radians);
            return new Vector2((float)(ca * v.X - sa * v.Y), (float)(sa * v.X + ca * v.Y));
        }

        public static Vector3 Rotate(this Vector3 v, double degrees)
        {
            return v.RotateRadians(degrees * DegToRad);
        }

        public static Vector3 RotateRadians(this Vector3 v, double radians)
        {
            var ca = Math.Cos(radians);
            var sa = Math.Sin(radians);
            return new Vector3((float)(ca * v.X - sa * v.Y), (float)(sa * v.X + ca * v.Y), v.Z);
        }

        public static Point Rotate(this Point p, Point origin, double degrees)
        {
            var theta = degrees * DegToRad;
            var x = Math.Cos(theta) * (p.X - origin.X) - Math.Sin(theta) * (p.Y - origin.Y) + origin.X;
            var y = Math.Sin(theta) * (p.X - origin.X) + Math.Cos(theta) * (p.Y - origin.Y) + origin.Y;
            return new Point(x, y);
        }

        public static Point RotateRadians(this Point v, double radians)
        {
            var ca = Math.Cos(radians);
            var sa = Math.Sin(radians);
            return new Point((float)(ca * v.X - sa * v.Y), (float)(sa * v.X + ca * v.Y));
        }

        public static Point FlipX(this Point p, Point origin)
        {
            return new Point(origin.X - (p.X - origin.X), p.Y);
        }

        public static Point FlipY(this Point p, Point origin)
        {
            return new Point(p.X, origin.Y - (p.Y - origin.Y));
        }

        public static Point FlipBoth(this Point p, Point origin)
        {
            return new Point(origin.X - (p.X - origin.X), origin.Y - (p.Y - origin.Y));
        }




    }

    //Originally: http://www.artiom.pro/2013/06/c-find-intersections-of-two-line-by.html

    //public class LineEquation
    //{
    //    public LineEquation(Point start, Point end)
    //    {
    //        Start = start;
    //        End = end;

    //        A = End.Y - Start.Y;
    //        B = Start.X - End.X;
    //        C = A * Start.X + B * Start.Y;
    //    }

    //    public Point Start { get; private set; }
    //    public Point End { get; private set; }

    //    public double A { get; private set; }
    //    public double B { get; private set; }
    //    public double C { get; private set; }

    //    public Point? GetIntersectionWithLine(LineEquation otherLine)
    //    {
    //        double determinant = A * otherLine.B - otherLine.A * B;

    //        if (determinant.IsZero()) //lines are parallel
    //            return default(Point?);

    //        //Cramer's Rule

    //        double x = (otherLine.B * C - B * otherLine.C) / determinant;
    //        double y = (A * otherLine.C - otherLine.A * C) / determinant;

    //        Point intersectionPoint = new Point(x, y);

    //        return intersectionPoint;
    //    }

    //    public Point? GetIntersectionWithLineSegment(LineEquation otherLine)
    //    {
    //        Point? intersectionPoint = GetIntersectionWithLine(otherLine);

    //        if (intersectionPoint.HasValue &&
    //            intersectionPoint.Value.IsBetweenTwoPoints(otherLine.Start, otherLine.End))
    //            return intersectionPoint;

    //        return default(Point?);
    //    }

    //    //i didnt review this one for correctness
    //    public LineEquation GetIntersectionWithLineForRay(Rect rectangle)
    //    {
    //        LineEquation intersectionLine;

    //        if (Start == End)
    //            return null;

    //        IEnumerable<LineEquation> lines = rectangle.LineSegments();
    //        intersectionLine = new LineEquation(new Point(0, 0), new Point(0, 0));
    //        var intersections = new Dictionary<LineEquation, Point>();
    //        foreach (LineEquation equation in lines)
    //        {
    //            Point? intersectionPoint = GetIntersectionWithLineSegment(equation);

    //            if (intersectionPoint.HasValue)
    //                intersections[equation] = intersectionPoint.Value;
    //        }

    //        if (!intersections.Any())
    //            return null;

    //        var intersectionPoints = new SortedDictionary<double, Point>();
    //        foreach (var intersection in intersections)
    //        {
    //            if (End.IsBetweenTwoPoints(Start, intersection.Value) ||
    //                intersection.Value.IsBetweenTwoPoints(Start, End))
    //            {
    //                double distanceToPoint = Start.DistanceToPoint(intersection.Value);
    //                intersectionPoints[distanceToPoint] = intersection.Value;
    //            }
    //        }

    //        if (intersectionPoints.Count == 1)
    //        {
    //            Point endPoint = intersectionPoints.First().Value;
    //            intersectionLine = new LineEquation(Start, endPoint);

    //            return intersectionLine;
    //        }

    //        if (intersectionPoints.Count == 2)
    //        {
    //            Point start = intersectionPoints.First().Value;
    //            Point end = intersectionPoints.Last().Value;
    //            intersectionLine = new LineEquation(start, end);

    //            return intersectionLine;
    //        }

    //        return null;
    //    }

    //    public override string ToString()
    //    {
    //        return "[" + Start + "], [" + End + "]";
    //    }
    //}

    public class LineEquation
    {
        public LineEquation(Point start, Point end)
        {
            Start = start;
            End = end;

            IsVertical = Math.Abs(End.X - start.X) < 0.00001f;
            M = (End.Y - Start.Y) / (End.X - Start.X);
            A = -M;
            B = 1;
            C = Start.Y - M * Start.X;
        }

        public bool IsVertical { get; private set; }

        public double M { get; private set; }

        public Point Start { get; private set; }
        public Point End { get; private set; }

        public double A { get; private set; }
        public double B { get; private set; }
        public double C { get; private set; }

        public bool IntersectsWithLine(LineEquation otherLine, out Point intersectionPoint)
        {
            intersectionPoint = new Point(0, 0);
            if (IsVertical && otherLine.IsVertical)
                return false;
            if (IsVertical || otherLine.IsVertical)
            {
                intersectionPoint = GetIntersectionPointIfOneIsVertical(otherLine, this);
                return true;
            }
            double delta = A * otherLine.B - otherLine.A * B;
            bool hasIntersection = Math.Abs(delta - 0) > 0.0001f;
            if (hasIntersection)
            {
                double x = (otherLine.B * C - B * otherLine.C) / delta;
                double y = (A * otherLine.C - otherLine.A * C) / delta;
                intersectionPoint = new Point(x, y);
            }
            return hasIntersection;
        }

        private static Point GetIntersectionPointIfOneIsVertical(LineEquation line1, LineEquation line2)
        {
            LineEquation verticalLine = line2.IsVertical ? line2 : line1;
            LineEquation nonVerticalLine = line2.IsVertical ? line1 : line2;

            double y = (verticalLine.Start.X - nonVerticalLine.Start.X) *
                       (nonVerticalLine.End.Y - nonVerticalLine.Start.Y) /
                       ((nonVerticalLine.End.X - nonVerticalLine.Start.X)) +
                       nonVerticalLine.Start.Y;
            double x = line1.IsVertical ? line1.Start.X : line2.Start.X;
            return new Point(x, y);
        }

        public bool IntersectWithSegementOfLine(LineEquation otherLine, out Point intersectionPoint)
        {
            bool hasIntersection = IntersectsWithLine(otherLine, out intersectionPoint);
            if (hasIntersection)
                return intersectionPoint.IsBetweenTwoPoints(otherLine.Start, otherLine.End);
            return false;
        }

        public override string ToString()
        {
            return "[" + Start + "], [" + End + "]";
        }
    }

    public static class DoubleExtensions
    {
        //SOURCE: https://github.com/mathnet/mathnet-numerics/blob/master/src/Numerics/Precision.cs
        //        https://github.com/mathnet/mathnet-numerics/blob/master/src/Numerics/Precision.Equality.cs
        //        http://referencesource.microsoft.com/#WindowsBase/Shared/MS/Internal/DoubleUtil.cs
        //        http://stackoverflow.com/questions/2411392/double-epsilon-for-equality-greater-than-less-than-less-than-or-equal-to-gre

        /// <summary>
        /// The smallest positive number that when SUBTRACTED from 1D yields a result different from 1D.
        /// The value is derived from 2^(-53) = 1.1102230246251565e-16, where IEEE 754 binary64 &quot;double precision&quot; floating point numbers have a significand precision that utilize 53 bits.
        ///
        /// This number has the following properties:
        /// (1 - NegativeMachineEpsilon) &lt; 1 and
        /// (1 + NegativeMachineEpsilon) == 1
        /// </summary>
        public const double NegativeMachineEpsilon = 1.1102230246251565e-16D; //Math.Pow(2, -53);

        /// <summary>
        /// The smallest positive number that when ADDED to 1D yields a result different from 1D.
        /// The value is derived from 2 * 2^(-53) = 2.2204460492503131e-16, where IEEE 754 binary64 &quot;double precision&quot; floating point numbers have a significand precision that utilize 53 bits.
        ///
        /// This number has the following properties:
        /// (1 - PositiveDoublePrecision) &lt; 1 and
        /// (1 + PositiveDoublePrecision) &gt; 1
        /// </summary>
        public const double PositiveMachineEpsilon = 2D * NegativeMachineEpsilon;

        /// <summary>
        /// The smallest positive number that when SUBTRACTED from 1D yields a result different from 1D.
        ///
        /// This number has the following properties:
        /// (1 - NegativeMachineEpsilon) &lt; 1 and
        /// (1 + NegativeMachineEpsilon) == 1
        /// </summary>
        public static readonly double MeasuredNegativeMachineEpsilon = MeasureNegativeMachineEpsilon();

        private static double MeasureNegativeMachineEpsilon()
        {
            double epsilon = 1D;

            do
            {
                double nextEpsilon = epsilon / 2D;

                if ((1D - nextEpsilon) == 1D) //if nextEpsilon is too small
                    return epsilon;

                epsilon = nextEpsilon;
            }
            while (true);
        }

        /// <summary>
        /// The smallest positive number that when ADDED to 1D yields a result different from 1D.
        ///
        /// This number has the following properties:
        /// (1 - PositiveDoublePrecision) &lt; 1 and
        /// (1 + PositiveDoublePrecision) &gt; 1
        /// </summary>
        public static readonly double MeasuredPositiveMachineEpsilon = MeasurePositiveMachineEpsilon();

        private static double MeasurePositiveMachineEpsilon()
        {
            double epsilon = 1D;

            do
            {
                double nextEpsilon = epsilon / 2D;

                if ((1D + nextEpsilon) == 1D) //if nextEpsilon is too small
                    return epsilon;

                epsilon = nextEpsilon;
            }
            while (true);
        }

        const double DefaultDoubleAccuracy = NegativeMachineEpsilon * 10D;

        public static bool IsClose(this double value1, double value2)
        {
            return IsClose(value1, value2, DefaultDoubleAccuracy);
        }

        public static bool IsClose(this double value1, double value2, double maximumAbsoluteError)
        {
            if (double.IsInfinity(value1) || double.IsInfinity(value2))
                return value1 == value2;

            if (double.IsNaN(value1) || double.IsNaN(value2))
                return false;

            double delta = value1 - value2;

            //return Math.Abs(delta) <= maximumAbsoluteError;

            if (delta > maximumAbsoluteError ||
                delta < -maximumAbsoluteError)
                return false;

            return true;
        }

        public static bool LessThan(this double value1, double value2)
        {
            return (value1 < value2) && !IsClose(value1, value2);
        }

        public static bool GreaterThan(this double value1, double value2)
        {
            return (value1 > value2) && !IsClose(value1, value2);
        }

        public static bool LessThanOrClose(this double value1, double value2)
        {
            return (value1 < value2) || IsClose(value1, value2);
        }

        public static bool GreaterThanOrClose(this double value1, double value2)
        {
            return (value1 > value2) || IsClose(value1, value2);
        }

        public static bool IsOne(this double value)
        {
            double delta = value - 1D;

            //return Math.Abs(delta) <= PositiveMachineEpsilon;

            if (delta > PositiveMachineEpsilon ||
                delta < -PositiveMachineEpsilon)
                return false;

            return true;
        }

        public static bool IsZero(this double value)
        {
            //return Math.Abs(value) <= PositiveMachineEpsilon;

            if (value > PositiveMachineEpsilon ||
                value < -PositiveMachineEpsilon)
                return false;

            return true;
        }
    }

    public static class PointExtensions
    {
        public static double DistanceToPoint(this Point point, Point point2)
        {
            return Math.Sqrt((point2.X - point.X) * (point2.X - point.X) + (point2.Y - point.Y) * (point2.Y - point.Y));
        }

        public static double SquaredDistanceToPoint(this Point point, Point point2)
        {
            return (point2.X - point.X) * (point2.X - point.X) + (point2.Y - point.Y) * (point2.Y - point.Y);
        }

        public static bool IsBetweenTwoPoints(this Point targetPoint, Point point1, Point point2)
        {
            double minX = Math.Min(point1.X, point2.X);
            double minY = Math.Min(point1.Y, point2.Y);
            double maxX = Math.Max(point1.X, point2.X);
            double maxY = Math.Max(point1.Y, point2.Y);

            double targetX = targetPoint.X;
            double targetY = targetPoint.Y;

            return minX.LessThanOrClose(targetX)
                  && targetX.LessThanOrClose(maxX)
                  && minY.LessThanOrClose(targetY)
                  && targetY.LessThanOrClose(maxY);
        }
    }

    public static class RectExtentions
    {
        //improved name from original
        public static IEnumerable<LineEquation> LineSegments(this Rect rectangle)
        {
            var lines = new List<LineEquation>
            {
                new LineEquation(new Point(rectangle.X, rectangle.Y),
                                 new Point(rectangle.X, rectangle.Y + rectangle.Height)),

                new LineEquation(new Point(rectangle.X, rectangle.Y + rectangle.Height),
                                 new Point(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height)),

                new LineEquation(new Point(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height),
                                 new Point(rectangle.X + rectangle.Width, rectangle.Y)),

                new LineEquation(new Point(rectangle.X + rectangle.Width, rectangle.Y),
                                 new Point(rectangle.X, rectangle.Y)),
            };

            return lines;
        }

        //improved from original at http://www.codeproject.com/Tips/403031/Extension-methods-for-finding-centers-of-a-rectang

        /// <summary>
        /// Returns the center point of the rectangle
        /// </summary>
        /// <param name="r"></param>
        /// <returns>Center point of the rectangle</returns>
        public static Point Center(this Rect r)
        {
            return new Point(r.Left + (r.Width / 2D), r.Top + (r.Height / 2D));
        }
        /// <summary>
        /// Returns the center right point of the rectangle
        /// i.e. the right hand edge, centered vertically.
        /// </summary>
        /// <param name="r"></param>
        /// <returns>Center right point of the rectangle</returns>
        public static Point CenterRight(this Rect r)
        {
            return new Point(r.Right, r.Top + (r.Height / 2D));
        }
        /// <summary>
        /// Returns the center left point of the rectangle
        /// i.e. the left hand edge, centered vertically.
        /// </summary>
        /// <param name="r"></param>
        /// <returns>Center left point of the rectangle</returns>
        public static Point CenterLeft(this Rect r)
        {
            return new Point(r.Left, r.Top + (r.Height / 2D));
        }
        /// <summary>
        /// Returns the center bottom point of the rectangle
        /// i.e. the bottom edge, centered horizontally.
        /// </summary>
        /// <param name="r"></param>
        /// <returns>Center bottom point of the rectangle</returns>
        public static Point CenterBottom(this Rect r)
        {
            return new Point(r.Left + (r.Width / 2D), r.Bottom);
        }
        /// <summary>
        /// Returns the center top point of the rectangle
        /// i.e. the topedge, centered horizontally.
        /// </summary>
        /// <param name="r"></param>
        /// <returns>Center top point of the rectangle</returns>
        public static Point CenterTop(this Rect r)
        {
            return new Point(r.Left + (r.Width / 2D), r.Top);
        }
    }

}
