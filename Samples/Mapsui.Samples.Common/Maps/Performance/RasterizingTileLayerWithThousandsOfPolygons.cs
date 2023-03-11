﻿using System;
using System.Collections.Generic;
using Mapsui.Layers;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Samples.Common.Utilities;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using NetTopologySuite.Geometries;

#pragma warning disable IDISP001 // Dispose created
#pragma warning disable IDISP004 // Don't ignore created IDisposable

namespace Mapsui.Samples.Common.Maps.Performance;

public class RasterizingTileLayerWithThousandsOfPolygons : IMapControlSample
{
    static RasterizingTileLayerWithThousandsOfPolygons()
    {
        ShapeFilesDeployer.CopyEmbeddedResourceToFile("countries.shp");
        ShapeFilesDeployer.CopyEmbeddedResourceToFile("cities.shp");
    }

    public string Name => "RasterizingTileLayer with Thousands of Polygons";
    public string Category => "Performance";

    public void Setup(IMapControl mapControl)
    {
        mapControl.Map = CreateMap();
    }

    public static Map CreateMap()
    {
        var map = new Map();

        map.Layers.Add(Tiling.OpenStreetMap.CreateTileLayer());
        map.Layers.Add(new RasterizingTileLayer(CreatePolygonLayer()));
        var home = Mercator.FromLonLat(15, 54);
        map.Home = n => n.NavigateTo(home, map.Resolutions[5]);

        return map;
    }
    public static ILayer CreatePolygonLayer()
    {
        return new Layer("Polygons")
        {
            DataSource = new Mapsui.Nts.Providers.GeometrySimplifyProvider(new MemoryProvider(CreatePolygon().ToFeatures())),
            Style = new VectorStyle
            {
                Fill = new Brush(Color.Red),
            }
        };
    }

    public static ILayer CreateLayer()
    {
        return new Layer("Polygons")
        {
            DataSource = new MemoryProvider(CreatePolygon().ToFeatures()),
            Style = new VectorStyle
            {
                Fill = new Brush(Color.Red),
            }
        };
    }
    private static List<Polygon> CreatePolygon()
    {
        var result = new List<Polygon>();

        Polygon polygon1;
        int factor = 0;

        for (int i = 0; i < 900000; i++)
        {
            factor = i - 100*(int)Math.Round((double)(i / 100));
            polygon1 = new Polygon(
                new LinearRing(new[] {
                    new Coordinate(1000*(factor-1), 1000*(factor-1)-(Math.Round((double)(i/100))*1000)),
                    new Coordinate(1000*(factor-1), 1000*(factor)-(Math.Round((double)(i/100))*1000)),
                    new Coordinate(1000*(factor), 1000*(factor)-(Math.Round((double)(i/100))*1000)),
                    new Coordinate(1000*(factor), 1000*(factor-1)-(Math.Round((double)(i/100))*1000)),
                    new Coordinate(1000*(factor-1), 1000*(factor-1)-(Math.Round((double)(i/100))*1000))
                }));
            
            result.Add(polygon1);
        }
        return result;
    }
}
