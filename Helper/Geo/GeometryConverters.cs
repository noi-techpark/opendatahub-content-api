// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;

namespace Helper.Geo
{
    public static class GeometryProjectionHelper
    {
        public static Geometry Transform31254To4326(Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            var csFactory = new CoordinateSystemFactory();
            var ctFactory = new CoordinateTransformationFactory();

            // WKT Definition für EPSG:31254 (MGI / Austria GK West)
            var source = csFactory.CreateFromWkt(
                @"PROJCS[""MGI / Austria GK West"",
                GEOGCS[""MGI"",
                  DATUM[""Militar_Geographische_Institut"",
                    SPHEROID[""Bessel 1841"",6377397.155,299.1528128],
                    TOWGS84[577.326,90.129,463.919,5.137,1.474,5.297,2.4232]],
                  PRIMEM[""Greenwich"",0],
                  UNIT[""degree"",0.0174532925199433]],
                PROJECTION[""Transverse_Mercator""],
                PARAMETER[""latitude_of_origin"",0],
                PARAMETER[""central_meridian"",10.3333333333333],
                PARAMETER[""scale_factor"",1],
                PARAMETER[""false_easting"",150000],
                PARAMETER[""false_northing"",-5000000],
                UNIT[""metre"",1]]");

            var target = GeographicCoordinateSystem.WGS84;

            var transform = ctFactory
                .CreateFromCoordinateSystems(source, target)
                .MathTransform;

            var clone = geometry.Copy();

            clone.Apply(new MathTransformFilter(transform));
            clone.SRID = 4326;

            return clone;
        }
    }

    internal class MathTransformFilter : ICoordinateSequenceFilter
    {
        private readonly MathTransform _transform;

        public MathTransformFilter(MathTransform transform)
        {
            _transform = transform;
        }

        public bool Done => false;
        public bool GeometryChanged => true;

        public void Filter(CoordinateSequence seq, int i)
        {
            var ordinates = new[] { seq.GetX(i), seq.GetY(i) };
            _transform.Transform(ordinates);

            seq.SetX(i, ordinates[0]);
            seq.SetY(i, ordinates[1]);
        }
    }
}
