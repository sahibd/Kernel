using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Enums;
using Tone.Core.Subsystems.BusinessObjects.Model;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.Test.Fixtures;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel.Test.Fixtures
{
    public class GeozoneCreateModelFixture : IFixture<GeozoneCreateModel>
    {
        private readonly GeozoneCreateModel _request;
        private Random _random = new Random();

        public GeozoneCreateModelFixture()
        {
            _request = BuildGeozone();
        }

        public GeozoneCreateModel Create()
        {
            return _request;
        }

        public GeozoneCreateModelFixture WithName(string name)
        {
            _request.Name = name;
            return this;
        }

        public GeozoneCreateModelFixture WithColor(string color)
        {
            _request.Color = color;
            return this;
        }

        public GeozoneCreateModelFixture WithGeozoneType(GeozoneType type)
        {
            _request.Type = type;
            return this;
        }

        public GeozoneCreateModelFixture WithRadius(double? radius)
        {
            _request.Radius = radius;
            return this;
        }

        public GeozoneCreateModelFixture WithBufferThickness(double bufferThickness)
        {
            _request.BufferThickness = bufferThickness;
            return this;
        }

        public GeozoneCreateModelFixture WithSquare(double? square)
        {
            _request.Square = square;
            return this;
        }

        public GeozoneCreateModelFixture WithPoints(PointModel[] points)
        {
            _request.Points = points;
            return this;
        }
        
        private GeozoneCreateModel BuildGeozone()
        {
            var geozoneModel = new GeozoneCreateModel()
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
