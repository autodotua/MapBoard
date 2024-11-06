using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Data;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using static MapBoard.Util.GeometryUtility.CentripetalCatmullRom;
using System.Diagnostics;

namespace MapBoard.Util
{
    public static class GeometryUtility
    {
        /// <summary>
        /// 确保一个图形是简单的单部分图形
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static IEnumerable<Geometry> EnsureSinglePart(this Geometry geometry)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    yield return geometry;
                    break;

                case GeometryType.Polyline:
                    var line = geometry as Polyline;

                    foreach (var part in line.Parts)
                    {
                        yield return new Polyline(part);
                    }
                    break;

                case GeometryType.Polygon:
                    var polygon = geometry as Polygon;

                    foreach (var part in polygon.Parts)
                    {
                        yield return new Polygon(part);
                    }
                    break;

                case GeometryType.Multipoint:
                    foreach (var point in (geometry as Multipoint).Points)
                    {
                        yield return point;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 如果图形存在M或Z，那么移除
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        //public static Geometry RemoveZAndM(this Geometry geometry)
        //{
        //    if (!geometry.HasM && !geometry.HasZ)
        //    {
        //        return geometry;
        //    }
        //    switch (geometry.GeometryType)
        //    {
        //        case GeometryType.Point:
        //            var point = geometry as MapPoint;
        //            return new MapPoint(point.X, point.Y, geometry.SpatialReference);

        //        case GeometryType.Polyline:
        //        case GeometryType.Polygon:
        //            List<IEnumerable<MapPoint>> parts = new List<IEnumerable<MapPoint>>();
        //            foreach (var part in (geometry as Multipart).Parts)
        //            {
        //                parts.Add(part.Points.Select(p => RemoveZAndM(p) as MapPoint));
        //            }
        //            if (geometry.GeometryType == GeometryType.Polyline)
        //            {
        //                return new Polyline(parts);
        //            }
        //            else
        //            {
        //                return new Polygon(parts);
        //            }

        //        case GeometryType.Multipoint:
        //            return new Multipoint((geometry as Multipoint).Points.Select(p => RemoveZAndM(p) as MapPoint));

        //        default:
        //            throw new NotSupportedException();
        //    }
        //}

        /// <summary>
        /// 获取米为单位的长度
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static double GetLength(this Geometry geometry)
        {
            return GeometryEngine.LengthGeodetic(geometry, null, GeodeticCurveType.Geodesic);
        }

        /// <summary>
        /// 获取平方米为单位的面积
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static double GetArea(this Geometry geometry)
        {
            return GeometryEngine.AreaGeodetic(geometry, null, GeodeticCurveType.Geodesic);
        }

        /// <summary>
        /// 获取两个点之间的大地距离
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double GetDistance(MapPoint p1, MapPoint p2)
        {
            return GeometryEngine.DistanceGeodetic(p1, p2, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic).Distance;
        }

        /// <summary>
        /// 默认椭球半长轴
        /// </summary>
        public static double SemiMajorAxis { get; set; } = 6378137;

        /// <summary>
        /// 默认椭球半短轴
        /// </summary>
        public static double SemiMinorAxis { get; set; } = 6356752.3142;

        /// <summary>
        /// 默认椭球扁率
        /// </summary>
        public static double Flattening { get; set; } = 1 / 298.2572236;

        /// <summary>
        /// 计算两个点的距离和角度信息
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static GeodeticCurveInfo CalculateGeodeticCurve(MapPoint start, MapPoint end)
        {
            double a = SemiMajorAxis;
            double b = SemiMinorAxis;
            double f = Flattening;

            // get parameters as radians
            double phi1 = Angle.FromDegree(start.Y).Radians;
            double lambda1 = Angle.FromDegree(start.X).Radians;
            double phi2 = Angle.FromDegree(end.Y).Radians;
            double lambda2 = Angle.FromDegree(end.X).Radians;

            // calculations
            double a2 = a * a;
            double b2 = b * b;
            double a2b2b2 = (a2 - b2) / b2;

            double omega = lambda2 - lambda1;

            double tanphi1 = Math.Tan(phi1);
            double tanU1 = (1.0 - f) * tanphi1;
            double U1 = Math.Atan(tanU1);
            double sinU1 = Math.Sin(U1);
            double cosU1 = Math.Cos(U1);

            double tanphi2 = Math.Tan(phi2);
            double tanU2 = (1.0 - f) * tanphi2;
            double U2 = Math.Atan(tanU2);
            double sinU2 = Math.Sin(U2);
            double cosU2 = Math.Cos(U2);

            double sinU1sinU2 = sinU1 * sinU2;
            double cosU1sinU2 = cosU1 * sinU2;
            double sinU1cosU2 = sinU1 * cosU2;
            double cosU1cosU2 = cosU1 * cosU2;

            // eq. 13
            double lambda = omega;

            // intermediates we'll need to compute 's'
            double A = 0.0;
            double B = 0.0;
            double sigma = 0.0;
            double deltasigma = 0.0;
            double lambda0;
            bool converged = false;

            for (int i = 0; i < 20; i++)
            {
                lambda0 = lambda;

                double sinlambda = Math.Sin(lambda);
                double coslambda = Math.Cos(lambda);

                // eq. 14
                double sin2sigma = (cosU2 * sinlambda * cosU2 * sinlambda) + Math.Pow(cosU1sinU2 - sinU1cosU2 * coslambda, 2.0);
                double sinsigma = Math.Sqrt(sin2sigma);

                // eq. 15
                double cossigma = sinU1sinU2 + (cosU1cosU2 * coslambda);

                // eq. 16
                sigma = Math.Atan2(sinsigma, cossigma);

                // eq. 17    Careful!  sin2sigma might be almost 0!
                double sinalpha = (sin2sigma == 0) ? 0.0 : cosU1cosU2 * sinlambda / sinsigma;
                double alpha = Math.Asin(sinalpha);
                double cosalpha = Math.Cos(alpha);
                double cos2alpha = cosalpha * cosalpha;

                // eq. 18    Careful!  cos2alpha might be almost 0!
                double cos2sigmam = cos2alpha == 0.0 ? 0.0 : cossigma - 2 * sinU1sinU2 / cos2alpha;
                double u2 = cos2alpha * a2b2b2;

                double cos2sigmam2 = cos2sigmam * cos2sigmam;

                // eq. 3
                A = 1.0 + u2 / 16384 * (4096 + u2 * (-768 + u2 * (320 - 175 * u2)));

                // eq. 4
                B = u2 / 1024 * (256 + u2 * (-128 + u2 * (74 - 47 * u2)));

                // eq. 6
                deltasigma = B * sinsigma * (cos2sigmam + B / 4 * (cossigma * (-1 + 2 * cos2sigmam2) - B / 6 * cos2sigmam * (-3 + 4 * sin2sigma) * (-3 + 4 * cos2sigmam2)));

                // eq. 10
                double C = f / 16 * cos2alpha * (4 + f * (4 - 3 * cos2alpha));

                // eq. 11 (modified)
                lambda = omega + (1 - C) * f * sinalpha * (sigma + C * sinsigma * (cos2sigmam + C * cossigma * (-1 + 2 * cos2sigmam2)));

                // see how much improvement we got
                double change = Math.Abs((lambda - lambda0) / lambda);

                if ((i > 1) && (change < 0.0000000000001))
                {
                    converged = true;
                    break;
                }
            }

            // eq. 19
            double s = b * A * (sigma - deltasigma);
            Angle alpha1;
            Angle alpha2;

            // didn't converge?  must be N/S
            if (!converged)
            {
                if (phi1 > phi2)
                {
                    alpha1 = Angle.Half;
                    alpha2 = Angle.Zero;
                }
                else if (phi1 < phi2)
                {
                    alpha1 = Angle.Zero;
                    alpha2 = Angle.Half;
                }
                else
                {
                    alpha1 = Angle.Empty;
                    alpha2 = Angle.Empty;
                }
            }

            // else, it converged, so do the math
            else
            {
                double radians;
                alpha1 = 0;
                alpha2 = 0;

                // eq. 20
                radians = Math.Atan2(cosU2 * Math.Sin(lambda), (cosU1sinU2 - sinU1cosU2 * Math.Cos(lambda)));
                if (radians < 0.0) radians += Math.PI * 2;
                alpha1 = Angle.FromRadians(radians);

                // eq. 21
                radians = Math.Atan2(cosU1 * Math.Sin(lambda), (-sinU1cosU2 + cosU1sinU2 * Math.Cos(lambda))) + Math.PI;
                if (radians < 0.0) radians += Math.PI * 2;
                alpha2 = Angle.FromRadians(radians);
            }

            if (alpha1 >= 360.0) alpha1 -= 360.0;
            if (alpha2 >= 360.0) alpha2 -= 360.0;

            return new GeodeticCurveInfo(s, alpha1, alpha2);
        }

        /// <summary>
        /// 计算从一个点向一个方向行进指定距离的终点位置
        /// </summary>
        /// <param name="start"></param>
        /// <param name="startBearing"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static MapPoint CalculateEndingGlobalCoordinates(MapPoint start, Angle startBearing, double distance)
        {
            double a = SemiMajorAxis;
            double b = SemiMinorAxis;
            double aSquared = a * a;
            double bSquared = b * b;
            double f = Flattening;
            double phi1 = Angle.FromDegree(start.Y).Radians;
            double alpha1 = startBearing.Radians;
            double cosAlpha1 = Math.Cos(alpha1);
            double sinAlpha1 = Math.Sin(alpha1);
            double s = distance;
            double tanU1 = (1.0 - f) * Math.Tan(phi1);
            double cosU1 = 1.0 / Math.Sqrt(1.0 + tanU1 * tanU1);
            double sinU1 = tanU1 * cosU1;

            // eq. 1
            double sigma1 = Math.Atan2(tanU1, cosAlpha1);

            // eq. 2
            double sinAlpha = cosU1 * sinAlpha1;

            double sin2Alpha = sinAlpha * sinAlpha;
            double cos2Alpha = 1 - sin2Alpha;
            double uSquared = cos2Alpha * (aSquared - bSquared) / bSquared;

            // eq. 3
            double A = 1 + (uSquared / 16384) * (4096 + uSquared * (-768 + uSquared * (320 - 175 * uSquared)));

            // eq. 4
            double B = (uSquared / 1024) * (256 + uSquared * (-128 + uSquared * (74 - 47 * uSquared)));

            // iterate until there is a negligible change in sigma
            double deltaSigma;
            double sOverbA = s / (b * A);
            double sigma = sOverbA;
            double sinSigma;
            double prevSigma = sOverbA;
            double sigmaM2;
            double cosSigmaM2;
            double cos2SigmaM2;

            for (; ; )
            {
                // eq. 5
                sigmaM2 = 2.0 * sigma1 + sigma;
                cosSigmaM2 = Math.Cos(sigmaM2);
                cos2SigmaM2 = cosSigmaM2 * cosSigmaM2;
                sinSigma = Math.Sin(sigma);
                double cosSignma = Math.Cos(sigma);

                // eq. 6
                deltaSigma = B * sinSigma * (cosSigmaM2 + (B / 4.0) * (cosSignma * (-1 + 2 * cos2SigmaM2)
                    - (B / 6.0) * cosSigmaM2 * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM2)));

                // eq. 7
                sigma = sOverbA + deltaSigma;

                // break after converging to tolerance
                if (Math.Abs(sigma - prevSigma) < 0.0000000000001) break;

                prevSigma = sigma;
            }

            sigmaM2 = 2.0 * sigma1 + sigma;
            cosSigmaM2 = Math.Cos(sigmaM2);
            cos2SigmaM2 = cosSigmaM2 * cosSigmaM2;

            double cosSigma = Math.Cos(sigma);
            sinSigma = Math.Sin(sigma);

            // eq. 8
            double phi2 = Math.Atan2(sinU1 * cosSigma + cosU1 * sinSigma * cosAlpha1,
                                     (1.0 - f) * Math.Sqrt(sin2Alpha + Math.Pow(sinU1 * sinSigma - cosU1 * cosSigma * cosAlpha1, 2.0)));

            // eq. 9
            // This fixes the pole crossing defect spotted by Matt Feemster.  When a path
            // passes a pole and essentially crosses a line of latitude twice - once in
            // each direction - the longitude calculation got messed up.  Using Atan2
            // instead of Atan fixes the defect.  The change is in the next 3 lines.
            //double tanLambda = sinSigma * sinAlpha1 / (cosU1 * cosSigma - sinU1*sinSigma*cosAlpha1);
            //double lambda = Math.Atan(tanLambda);
            double lambda = Math.Atan2(sinSigma * sinAlpha1, cosU1 * cosSigma - sinU1 * sinSigma * cosAlpha1);

            // eq. 10
            double C = (f / 16) * cos2Alpha * (4 + f * (4 - 3 * cos2Alpha));

            // eq. 11
            double L = lambda - (1 - C) * f * sinAlpha * (sigma + C * sinSigma * (cosSigmaM2 + C * cosSigma * (-1 + 2 * cos2SigmaM2)));

            // eq. 12
            double alpha2 = Math.Atan2(sinAlpha, -sinU1 * sinSigma + cosU1 * cosSigma * cosAlpha1);

            // build result
            Angle latitude = new Angle();
            Angle longitude = new Angle();

            latitude.Radians = phi2;
            longitude.Radians = Angle.FromDegree(start.X).Radians + L;

            //endBearing = new Angle();
            //endBearing.Radians = alpha2;
            return new MapPoint(longitude.Degrees, latitude.Degrees, SpatialReferences.Wgs84);
        }

        /// <summary>
        /// 获取一个图形的所有结点
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IEnumerable<MapPoint> GetPoints(this Geometry geometry)
        {
            switch (geometry)
            {
                case MapPoint point:
                    yield return point;
                    break;

                case Multipoint multipoint:
                    foreach (var point in multipoint.Points)
                    {
                        yield return point;
                    }
                    break;

                case Multipart multipart:
                    foreach (var part in multipart.Parts)
                    {
                        foreach (var point in part.Points)
                        {
                            yield return point;
                        }
                    }
                    break;

                default:
                    throw new ArgumentException("不支持的Geometry类型");
            }
        }

        /// <summary>
        /// 转为4326的坐标系
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static T ToWgs84<T>(this T geometry) where T : Geometry
        {
            return GeometryEngine.Project(geometry, SpatialReferences.Wgs84) as T;
        }

        /// <summary>
        /// 转为3857的坐标系
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static T ToWebMercator<T>(this T geometry) where T : Geometry
        {
            return GeometryEngine.Project(geometry, SpatialReferences.WebMercator) as T;
        }

        /// <summary>
        /// 将3857的点的X坐标正规化到[-20037508.34,20037508.34]
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static MapPoint RegularizeWebMercatorPoint(this MapPoint point)
        {
            if (point.SpatialReference != null && point.SpatialReference.Wkid != 3857)
            {
                throw new ArgumentException("输入点的坐标系错误，必须为3857");
            }
            double a = 20037508.34;
            double x = point.X;
            if (x <= a && x >= -a)
            {
                return point;
            }
            x += a;
            if (x > 2 * a)
            {
                x %= (2 * a);
                x = x - a;
            }
            else //<0
            {
                x = x % (2 * a) + a;
            }
            Debug.Assert(x >= -a && x <= a);
            return new MapPoint(x, point.Y, SpatialReferences.WebMercator);
        }

        /// <summary>
        /// 创建图形的副本
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="geometry"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static T Clone<T>(this T geometry) where T : Geometry
        {
            if (geometry.HasM || geometry.HasZ)
            {
                throw new ArgumentException("不支持含有Z或M的图形");
            }
            switch (geometry)
            {
                case MapPoint p:
                    return new MapPoint(p.X, p.Y, p.SpatialReference) as T;

                case Multipoint pp:
                    return new Multipoint(pp.Points) as T;

                case Polyline l:
                    return new Polyline(l.Parts) as T;

                case Polygon a:
                    return new Polygon(a.Parts) as T;

                case Envelope e:
                    return new Envelope(e.XMin, e.YMin, e.XMax, e.YMax, e.SpatialReference) as T;

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 确保获得有效的图形，例如线必须包含两个点以上。
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns>若图形有效，返回原图形；否则，若部分有效，返回有效的部分，否则返回null。</returns>
        /// <exception cref="NotSupportedException">不支持的图形类型</exception>
        public static Geometry EnsureValid(this Geometry geometry)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    return geometry;

                case GeometryType.Polyline:
                case GeometryType.Polygon:

                    if ((geometry as Multipart).Parts.All(p => IsValid(p, (int)geometry.Dimension + 1)))
                    {
                        return geometry;
                    }
                    var parts = (geometry as Multipart).Parts.Where(p => IsValid(p, (int)geometry.Dimension + 1));
                    if (!parts.Any())
                    {
                        return null;
                    }
                    return geometry.GeometryType == GeometryType.Polyline ?
                        new Polyline(parts, geometry.SpatialReference) as Geometry
                        : new Polygon(parts, geometry.SpatialReference);

                case GeometryType.Multipoint:
                    if ((geometry as Multipoint).Points.Count >= 1)
                    {
                        return geometry;
                    }
                    return null;

                default:
                    throw new NotSupportedException();
            }
        }

        private static bool IsValid(ReadOnlyPart part, int dimension)
        {
            return dimension switch
            {
                3 => part.PointCount >= 3,
                2 => part.PointCount >= 2,
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        /// 平滑
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="pointsPerSegment">两个节点之间生成多少新点</param>
        /// <param name="level">平滑等级，0最拟合，1一般，2最平滑</param>
        /// <param name="minSmoothAngle">最小需要平滑的角度。若某三个节点组成的角小于该角度，那么会对中间点左右两侧分别进行平滑然后拼接</param>
        /// <returns></returns>
        public static Geometry Smooth(Geometry geometry, int pointsPerSegment, int level, double minSmoothAngle)
        {
            if (level < 0 || level > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }
            if (pointsPerSegment <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pointsPerSegment));
            }

            //CentripetalCatmullRom中数量是算上头尾的
            pointsPerSegment += 2;
            var levelType = (CatmullRomType)level;

            if (geometry is Multipart m)
            {
                List<IEnumerable<MapPoint>> newParts = new List<IEnumerable<MapPoint>>();
                foreach (var part in m.Parts)
                {
                    if (part.PointCount <= 2)
                    {
                        newParts.Add(part.Points);
                        continue;
                    }
                    var partPoints = part.Points.ToArray();
                    double[] angles1 = new double[part.PointCount - 1]; //第1个点到第22个点与{x=0,y>0}形成的夹角
                    double[] angles2 = new double[part.PointCount - 1]; //第2个点到第1个点与{x=0,y>0}形成的夹角
                    double[] angles = new double[part.PointCount - 1]; //第1-2个点、2-3个点的线端形成的夹角
                    //计算两点之间形成的线与{x=0,y>0}形成的夹角
                    for (int i = 1; i < part.PointCount; i++)
                    {
                        var info = GeometryEngine.DistanceGeodetic(partPoints[i - 1], partPoints[i], null, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                        angles1[i - 1] = info.Azimuth1; //-180~180
                        angles2[i - 1] = info.Azimuth2; //-180~180
                    }
                    bool hasAngleSmallerThanMin = false;
                    //计算每两条线之间形成的夹角
                    for (int i = 1; i < angles1.Length; i++)
                    {
                        double angle = angles2[i] - angles1[i - 1];
                        if (angle < 0)
                        {
                            angle = -angle;
                        }
                        if (angle > 180)
                        {
                            angle = 360 - angle;
                        }
                        angles[i] = angle;
                        if (angle < minSmoothAngle)
                        {
                            hasAngleSmallerThanMin = true;
                        }
                    }

                    if (hasAngleSmallerThanMin)
                    {
                        List<MapPoint> points = new List<MapPoint>
                        {
                            partPoints[0]
                        };
                        int startIndex = 0;
                        for (int i = 1; i < part.PointCount; i++)
                        {
                            if (i < angles.Length && angles[i] >= minSmoothAngle) //不是最后一个点，且角度大于阈值
                            {
                                continue;
                            }
                            if (i - startIndex == 1)//只有两个点
                            {
                                points.Add(partPoints[i]);
                                startIndex = i;
                            }
                            else
                            {
                                var tmpPoints = Interpolate(partPoints[startIndex..(i + 1)], pointsPerSegment, levelType);
                                points.AddRange(tmpPoints.Skip(1));
                                startIndex = i;
                            }
                        }
                        newParts.Add(points);
                    }
                    else
                    {
                        newParts.Add(Interpolate(part.Points.ToList(), pointsPerSegment, levelType));
                    }
                }



                switch (geometry)
                {
                    case Polyline:
                        return new Polyline(newParts);

                    case Polygon:
                        return new Polygon(newParts);
                }
            }
            throw new NotSupportedException("仅支持折线和多边形");
        }
        /// <summary>
        /// Catmull–Rom样条
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/questions/9489736/catmull-rom-curve-with-no-cusps-and-no-self-intersections/19283471#19283471
        /// </remarks>
        internal class CentripetalCatmullRom
        {
            public static List<MapPoint> Interpolate(IList<MapPoint> coordinates, int pointsPerSegment, CatmullRomType curveType)
            {
                List<MapPoint> vertices = new List<MapPoint>();
                foreach (MapPoint c in coordinates)
                {
                    vertices.Add(c.Clone());
                }
                if (pointsPerSegment < 2)
                {
                    throw new Exception("The pointsPerSegment parameter must be greater than 2, since 2 points is just the linear segment.");
                }
                if (vertices.Count < 3)
                {
                    return vertices;
                }
                bool isClosed = GeometryEngine.Intersects(vertices[0], vertices[^1]);
                if (isClosed)
                {
                    MapPoint p2 = vertices[1].Clone();
                    MapPoint pn1 = vertices[^2].Clone();

                    vertices.Insert(0, pn1);
                    vertices.Add(p2);
                }
                else
                {
                    double dx = vertices[1].X - vertices[0].X;
                    double dy = vertices[1].Y - vertices[0].Y;


                    MapPoint start = new MapPoint(vertices[0].X - dx, vertices[0].Y - dy);

                    int n = vertices.Count - 1;
                    dx = vertices[n].X - vertices[n - 1].X;
                    dy = vertices[n].Y - vertices[n - 1].Y;
                    MapPoint end = new MapPoint(vertices[n].X + dx, vertices[n].Y + dy);

                    vertices.Insert(0, start);

                    vertices.Add(end);
                }

                List<MapPoint> result = new List<MapPoint>();
                for (int i = 0; i < vertices.Count - 3; i++)
                {
                    List<MapPoint> points = Interpolate(vertices, i, pointsPerSegment, curveType);
                    if (result.Count > 0)
                    {
                        points.RemoveAt(0);
                    }

                    result.AddRange(points);
                }
                return result;
            }

            public enum CatmullRomType
            {
                /// <summary>
                /// α=0，拟合度最好，但容易出现过拟合/尖角/闭合
                /// </summary>
                Uniform = 0,

                /// <summary>
                /// α=1/2，一般拟合
                /// </summary>
                Centripetal = 1,

                /// <summary>
                /// α=1，最平滑，但拟合较差
                /// </summary>
                Chordal = 2,
            }

            private static List<MapPoint> Interpolate(IList<MapPoint> points, int index, int pointsPerSegment, CatmullRomType curveType)
            {
                List<MapPoint> result = new List<MapPoint>();
                double[] x = new double[4];
                double[] y = new double[4];
                double[] time = new double[4];
                for (int i = 0; i < 4; i++)
                {
                    x[i] = points[index + i].X;
                    y[i] = points[index + i].Y;
                    time[i] = i;
                }

                double tstart = 1;
                double tend = 2;
                if (!curveType.Equals(CatmullRomType.Uniform))
                {
                    double total = 0;
                    for (int i = 1; i < 4; i++)
                    {
                        double dx = x[i] - x[i - 1];
                        double dy = y[i] - y[i - 1];
                        if (curveType.Equals(CatmullRomType.Centripetal))
                        {
                            total += Math.Pow(dx * dx + dy * dy, .25);
                        }
                        else
                        {
                            total += Math.Pow(dx * dx + dy * dy, .5);
                        }
                        time[i] = total;
                    }
                    tstart = time[1];
                    tend = time[2];
                }
                int segments = pointsPerSegment - 1;
                result.Add(points[index + 1]);
                for (int i = 1; i < segments; i++)
                {
                    double xi = Interpolate(x, time, tstart + i * (tend - tstart) / segments);
                    double yi = Interpolate(y, time, tstart + i * (tend - tstart) / segments);
                    result.Add(new MapPoint(xi, yi));
                }
                result.Add(points[index + 2]);
                return result;
            }

            private static double Interpolate(double[] p, double[] time, double t)
            {
                double L01 = p[0] * (time[1] - t) / (time[1] - time[0]) + p[1] * (t - time[0]) / (time[1] - time[0]);
                double L12 = p[1] * (time[2] - t) / (time[2] - time[1]) + p[2] * (t - time[1]) / (time[2] - time[1]);
                double L23 = p[2] * (time[3] - t) / (time[3] - time[2]) + p[3] * (t - time[2]) / (time[3] - time[2]);
                double L012 = L01 * (time[2] - t) / (time[2] - time[0]) + L12 * (t - time[0]) / (time[2] - time[0]);
                double L123 = L12 * (time[3] - t) / (time[3] - time[1]) + L23 * (t - time[1]) / (time[3] - time[1]);
                double C12 = L012 * (time[2] - t) / (time[2] - time[1]) + L123 * (t - time[1]) / (time[2] - time[1]);
                return C12;
            }
        }
    }
}