using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.Test.Fixtures;

namespace Tone.TelematicsKernel.Test.Fixtures
{
    public class GeozoneUpdateModelFixture : IFixture<GeozoneUpdateModel>
    {
        private readonly GeozoneUpdateModel _request;
        private Random _random = new Random();

        public GeozoneUpdateModelFixture()
        {
            _request = BuildGeozone();
        }

        public GeozoneUpdateModel Create()
        {
            return _request;
        }

        public GeozoneUpdateModelFixture WithName(string name)
        {
            _request.Name = name;
            return this;
        }

        public GeozoneUpdateModelFixture WithColor(string color)
        {
            _request.Color = color;
            return this;
        }

        public GeozoneUpdateModelFixture WithGeozoneType(GeozoneType type)
        {
            _request.Type = type;
            return this;
        }

        public GeozoneUpdateModelFixture WithRadius(double? radius)
        {
            _request.Radius = radius;
            return this;
        }

        public GeozoneUpdateModelFixture WithBufferThickness(double bufferThickness)
        {
            _request.BufferThickness = bufferThickness;
            return this;
        }

        public GeozoneUpdateModelFixture WithSquare(double? square)
        {
            _request.Square = square;
            return this;
        }

        public GeozoneUpdateModelFixture WithPoints(PointModel[] points)
        {
            _request.Points = points;
            return this;
        }

        public GeozoneUpdateModelFixture WithId(object id)
        {
            _request.Id = id.ToString();
            return this;
        }

        private GeozoneUpdateModel BuildGeozone()
        {
            var geozoneModel = new GeozoneUpdateModel()
            {
                Name = "Геозона_" + _random.Next(1, 101),
                Description = null,
                Color = "#b1f7ec",
                Type = GeozoneType.Polygon,
                Access = GeozoneAccessType.PrivateToUser,
                Radius = 10,
                BufferThickness = 2,
                Square = 35.78,
                Points = GeneratePoints()
            };
            return geozoneModel;
        }


        private PointModel[] GeneratePoints()
        {
            var pointsList = new List<PointModel>
            {
                new PointModel() {Longitude = 55.708446, Latitude = 37.654082},
                new PointModel() {Longitude = 57.708446, Latitude = 39.654082},
                new PointModel() {Longitude = 59.708446, Latitude = 41.654082},
                new PointModel() {Longitude = 55.708446, Latitude = 37.654082}
            };
            return pointsList.ToArray();
        }
    }
}
