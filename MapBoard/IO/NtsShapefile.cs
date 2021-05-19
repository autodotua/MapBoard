using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Feature = NetTopologySuite.Features.Feature;
using Geometry = NetTopologySuite.Geometries.Geometry;
using GeometryType = Esri.ArcGISRuntime.Geometry.GeometryType;

namespace MapBoard.Main.IO
{
    public class NtsShapefile
    {
        public static void TestCreate2Dshape(string fileName, GeometryType type, Field[] fields)
        {
            Dictionary<GeometryType, ShapeGeometryType> esriGeometryType2NtsGeometryType = new Dictionary<GeometryType, ShapeGeometryType>()
            {
                [GeometryType.Point] = ShapeGeometryType.Point,
                [GeometryType.Multipoint] = ShapeGeometryType.MultiPoint,
                [GeometryType.Polyline] = ShapeGeometryType.LineString,
                [GeometryType.Polygon] = ShapeGeometryType.Polygon
            };
            int srid = 4326;
            var sequenceFactory = new NetTopologySuite.Geometries.Implementation.DotSpatialAffineCoordinateSequenceFactory(Ordinates.XY);
            var geomFactory = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), srid, sequenceFactory);

            var tmpFile = Path.GetTempFileName();
            ShapefileWriter shpWriter = new ShapefileWriter(geomFactory, tmpFile, esriGeometryType2NtsGeometryType[type]);

            ShapefileDataWriter dataWriter = new ShapefileDataWriter(fileName, geomFactory, Encoding.UTF8);

            List<Feature> features = new List<Feature>();
            AttributesTable att = new AttributesTable();
            foreach (var field in fields)
            {
                switch (field.FieldType)
                {
                    case FieldType.OID:
                        att.Add(field.Name, 0);
                        break;

                    case FieldType.Int16:
                    case FieldType.Int32:
                        att.Add(field.Name, int.MaxValue);
                        break;

                    case FieldType.Float32:
                    case FieldType.Float64:
                        att.Add(field.Name, 0d);
                        break;

                    case FieldType.Date:
                        att.Add(field.Name, DateTime.Now);
                        break;

                    case FieldType.Text:
                        att.Add(field.Name, new string('c', field.Length));
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
            Geometry geom;
            switch (type)
            {
                case GeometryType.Point:
                    geom = geomFactory.CreatePoint(new Coordinate(0, 0));
                    break;

                case GeometryType.Polyline:
                    geom = geomFactory.CreateLineString(new[] { new Coordinate(0, 0), new Coordinate(1, 1) });
                    break;

                case GeometryType.Polygon:
                    geom = geomFactory.CreatePolygon(new[] { new Coordinate(0, 0), new Coordinate(1, 1), new Coordinate(0, 1), new Coordinate(0, 0) });
                    break;

                case GeometryType.Multipoint:
                    geom = geomFactory.CreateMultiPointFromCoords(new[] { new Coordinate(0, 0), new Coordinate(1, 1) });
                    break;

                default:
                    throw new NotSupportedException();
            }

            features.Add(new Feature(shpWriter.Factory.CreateGeometry(geom), att));

            var outDbaseHeader = ShapefileDataWriter.GetHeader(features[0], 1, Encoding.UTF8);
            dataWriter.Header = outDbaseHeader;
            dataWriter.Write(features);

            File.WriteAllText(fileName + ".prj", ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84.WKT);
            File.WriteAllText(fileName + ".cpg", "UTF-8");

            shpWriter.Close();
            File.Delete(tmpFile + ".shp");
            File.Delete(tmpFile + ".shx");
        }
    }
}