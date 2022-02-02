﻿using System.Linq;
using Mapsui.Extensions;
using Mapsui.Nts;
using Mapsui.Projections;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NUnit.Framework;

namespace Mapsui.Tests.Projections
{
    [TestFixture]
    public class ProjectionTests
    {
        private WKTReader _wktReader = new WKTReader();

        [Test]
        public void MultiPolygonAllVerticesTest()
        {
            // arrange
            var geomety = _wktReader.Read("MULTIPOLYGON (((40 40, 20 45, 45 30, 40 40)), ((20 35, 10 30, 10 10, 30 5, 45 20, 20 35), (30 20, 20 15, 20 25, 30 20)))");
            const int numberOfVectices = 14;

            // act
            var enumeration = geomety.Coordinates;

            // assert
            Assert.AreEqual(numberOfVectices, enumeration.Count());
        }

        [Test]
        public void MultiLineStringAllVerticesTest()
        {
            // arrange
            var geomety = _wktReader.Read("MULTILINESTRING ((10 10, 20 20, 10 40), (40 40, 30 30, 40 20, 30 10))");
            const int numberOfVectices = 7;

            // act
            var enumeration = geomety.Coordinates;

            // assert
            Assert.AreEqual(numberOfVectices, enumeration.Count());
        }

        [Test]
        public void AllVerticesTransformTest()
        {
            // arrange
            var multiPolygon = (MultiPolygon)_wktReader.Read("MULTIPOLYGON (((40 40, 20 45, 45 30, 40 40)), ((20 35, 10 30, 10 10, 30 5, 45 20, 20 35), (30 20, 20 15, 20 25, 30 20)))");
            var copiedMultiPolygon = multiPolygon.Copy();
            using var feature = new GeometryFeature(copiedMultiPolygon);
            var projection = new Projection();

            // act
            projection.Project("EPSG:4326", "EPSG:3857", feature);

            // assert
            var vertices = multiPolygon.Coordinates.ToList();
            var copiedVertices = copiedMultiPolygon.Coordinates.ToList();
            for (var i = 0; i < vertices.Count; i++)
            {
                Assert.AreNotEqual(vertices[i].X, copiedVertices[i].X);
                Assert.AreNotEqual(vertices[i].Y, copiedVertices[i].Y);
            }
        }
    }
}
