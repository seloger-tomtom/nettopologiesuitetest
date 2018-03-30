using GeoAPI.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Triangulate.QuadEdge;

namespace geographyTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string polygonString =
                @"POLYGON((-28.7890625 46.42781304588344,-15.95703125 54.51262233957747,-0.3125 59.881625088941696,19.7265625 53.21704316159465,17.44140625 26.41817755694856,4.609375 40.03328029340769,6.015625 27.670622439182004,-12.96875 31.19093926426022,-23.69140625 21.60303052366203,-28.701171875 34.65737205086236,-31.9091796875 40.80277883990915,-28.7890625 46.42781304588344))";
            string pointInteriorString = @"POINT(-5.5859375 49.48720849371433)";
            string pointExteriorString = @"POINT(26.25244140625 47.149463048973175)";
            string complexInteriorPointString = @"POINT(-22.26318359375 27.51422522437252)";
            string complexExteriorPointString = @"POINT(6.56494140625 36.72665444506848)";

            WKTReader reader = new WKTReader();
            var polygon = reader.Read(polygonString);
            var pointInterior = (IPoint) reader.Read(pointInteriorString);
            var pointExterior = (IPoint) reader.Read(pointExteriorString);
            var complexInteriorPoint = (IPoint) reader.Read(complexInteriorPointString);
            var complexExteriorPoint = (IPoint) reader.Read(complexExteriorPointString);

            var test = (ILinearRing) polygon.Boundary;

            var listTest = new List<int>();
            for (int i = 0; i < 100000; i++)
            {
                listTest.Add(i);
            }

            Parallel.ForEach(listTest, (i) =>
            {
                var polygonEnvelope = test.Contains(pointInterior);
                var polygonEnvelope2 = test.Contains(complexInteriorPoint);
                var polygonEnvelope3 = test.Contains(pointExterior);
                var polygonEnvelope4 = test.Contains(complexExteriorPoint);

                Console.WriteLine($"{i} souldBeTruePI : {polygonEnvelope}");
                Console.WriteLine($"{i} souldBeFalsePE : {polygonEnvelope2}");
                Console.WriteLine($"{i} souldBeTruePIC : {polygonEnvelope3}");
                Console.WriteLine($"{i} souldBeFalsePEC : {polygonEnvelope4}");
            });

            //Console.Read();

            Parallel.ForEach(listTest, (i) =>
            {
                var polygonEnvelope = CoordinateExtensions.Contains(test, pointInterior);
                var polygonEnvelope2 = CoordinateExtensions.Contains(test, complexInteriorPoint);
                var polygonEnvelope3 = CoordinateExtensions.Contains(test, pointExterior);
                var polygonEnvelope4 = CoordinateExtensions.Contains(test, complexExteriorPoint);

                Console.WriteLine($"{i} souldBeTruePI : {polygonEnvelope}");
                Console.WriteLine($"{i} souldBeFalsePE : {polygonEnvelope2}");
                Console.WriteLine($"{i} souldBeTruePIC : {polygonEnvelope3}");
                Console.WriteLine($"{i} souldBeFalsePEC : {polygonEnvelope4}");
            });
         
            Console.Read();
        }
    }

    public static class CoordinateExtensions
    {
        public static bool Contains(this IGeometry geo, IPoint point)
        {
            if (geo.GeometryType.ToLower() == "multipolygon")
            {
            }
            else
            {
            }


            return Contains((ILinearRing) geo, point);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ring"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool Contains(this ILinearRing ring, IPoint point)
        {
            var matrix = ring.Coordinates;
            if (!matrix.InMaxBounds(point)) return false;


            var centerPointSegment = new LineSegment(ring.Centroid.CoordinateSequence.GetCoordinate(0),
                point.CoordinateSequence.GetCoordinate(0));
            var centerCoordinate = centerPointSegment.P0;
            var pointCoordinate = centerPointSegment.P1;
            var middleCoordinate = centerPointSegment.MidPoint;

            List<LineSegment> segments = new List<LineSegment>(matrix.Length - 1);
            for (int i = 0; i < matrix.LongLength - 1; i++)
            {
                segments.Add(new LineSegment(matrix[i], matrix[i + 1]));
            }

            var comparer = new CoordinatesComparer();

            //selection de la face du repere orthogonal
            int pointPosition = comparer.Compare(centerCoordinate, pointCoordinate);

            //recuperation des segments correspondant a la face
            var segmentsInSelectedCartesian = segments.Where(segment =>
                    comparer.Compare(centerCoordinate, segment.P0) == pointPosition ||
                    comparer.Compare(centerCoordinate, segment.P1) == pointPosition).OrderBy(segment => segment.MaxY)
                .ThenBy(segment => segment.MaxX).ToList();
            segmentsInSelectedCartesian.Add(
                new LineSegment(segmentsInSelectedCartesian[segmentsInSelectedCartesian.Count - 1].P1,
                    middleCoordinate));
            segmentsInSelectedCartesian.Add(new LineSegment(middleCoordinate, segmentsInSelectedCartesian[0].P0));

            var newPolygonCoordinates = segmentsInSelectedCartesian
                .SelectMany(segment => new List<Coordinate>() {segment.P0, segment.P1}).ToArray();
            var newPolygon = Geometry.DefaultFactory.CreatePolygon(newPolygonCoordinates);
            if (newPolygon.NumPoints > 10)
            {
                newPolygon.Boundary.Contains(point);
            }

            return newPolygon.Contains(point);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool InMaxBounds(this IEnumerable<Coordinate> matrix, IPoint point)
        {
            var ys = matrix.Select(coordinate => coordinate.Y).ToList();
            var xs = matrix.Select(coordinate => coordinate.X).ToList();

            var xMax = xs.Max();
            var xMin = xs.Min();
            var yMax = ys.Max();
            var yMin = ys.Min();

            if (point.X > xMax || point.X < xMin || point.Y > yMax || point.Y < yMin)
            {
                return false;
            }

            return true;
        }
        //public static bool Contains(this IEnumerable<Coordinate> matrix, IPoint center, IPoint point)
        //{
        //    if (matrix == null) return false;

        //    var ys = matrix.Select(coordinate => coordinate.Y).ToList();
        //    var xs = matrix.Select(coordinate => coordinate.X).ToList();

        //    var xMax = xs.Max();
        //    var xMin = xs.Min();
        //    var yMax = ys.Max();
        //    var yMin = ys.Min();

        //    if (point.X > xMax || point.X < xMin || point.Y > yMax || point.Y < yMin)
        //    {
        //        return false;
        //    }

        //    var comparer = new CoordinatesComparer();

        //    var coordinates = matrix.Where(coordinate => comparer.Compare(center.CoordinateSequence.GetCoordinate(0), coordinate) == 2).Distinct();

        //    //Ajout des point debut et fin pour fermer le polygon spliter
        //    //var first = matrix.(p => coordinates.Any(c => c.Equals(p)));
        //    //var test = Intersection(point.CoordinateSequence.GetCoordinate(0).Middle(centerCoordinate),first , matrix.ElementAt())

        //    List<Coordinate> partPolygon = new List<Coordinate>()
        //    {
        //        new Coordinate(center.X, coordinates.First().Y)
        //    };
        //    partPolygon.AddRange(coordinates);
        //    partPolygon.Add(new Coordinate(coordinates.Last().X, center.Y));

        //    //if (pointPosition > 0)
        //    //{
        //    //    for (int i = 0; i < partPolygon.Count - 1; i++)
        //    //    {
        //    //        if (AngleIsPositive(partPolygon[i], partPolygon[i + 1], point.CoordinateSequence.GetCoordinate(0)))
        //    //        {
        //    //            return false;
        //    //        }
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    for (int i = 0; i < partPolygon.Count - 1; i++)
        //    //    {
        //    //        if (!AngleIsPositive(partPolygon[i], partPolygon[i + 1], point.CoordinateSequence.GetCoordinate(0)))
        //    //        {
        //    //            return false;
        //    //        }
        //    //    }
        //    //}

        //    return true;
        //}

        public static bool AngleIsPositive(this LineSegment segment, Coordinate c)
        {
            return Angle(segment, c) > 0;
        }

        public static double Angle(this LineSegment segment, Coordinate c)
        {
            var vector1 = Vector2D.Create(segment.P0, segment.P1);
            var vector2 = Vector2D.Create(segment.P0, c);

            double angleTo = vector1.AngleTo(vector2);

            return angleTo;
        }

        ///// <summary>
        ///// calcule les coodinées du mileu d'un segment
        ///// </summary>
        ///// <param name="src"> coordonnées de debut du segment </param>
        ///// <param name="coordinate"> coordonnées de la fin du segment</param>
        ///// <returns></returns>
        //public static Coordinate Middle(this Coordinate src, Coordinate coordinate)
        //{
        //    double x = (src.X + coordinate.X) / 2;
        //    double y = (src.Y + coordinate.Y) / 2;

        //    var middle = new Coordinate(x, y);
        //    return middle;
        //}

        public static Coordinate Intersection(this Coordinate a, Coordinate b, Coordinate c, double rotation)
        {
            var lineIntersector = new NonRobustLineIntersector();
            lineIntersector.ComputeIntersection(a, b, c);
            var coordinate = lineIntersector.GetIntersection(0);
            var intersection = lineIntersector.GetIntersection(1);
            return null;
        }
    }

    public class CoordinatesComparer : IComparer<Coordinate>
    {
        public int Compare(Coordinate x, Coordinate y)
        {
            switch (x.CompareTo(y))
            {
                // si le point y est plus grand
                case -1:
                    if (x.Y > y.Y)
                    {
                        return -2;
                    }

                    return 2;
                // si le point y est plus petit
                case 1:
                    if (x.Y > y.Y)
                    {
                        return -1;
                    }

                    return 1;
                default: return 0;
            }


            return 0;
        }
    }
}