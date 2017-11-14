using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Tone.Core;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Core.Test;
using Tone.Core.Test.Factories;
using Tone.TelematicsKernel.MappingProfiles;
using Tone.TelematicsKernel.Test.Fixtures;
using Xunit;
using Tone.Core.WebContext;
using Tone.Validation;
using Tone.Core.Enums;

namespace Tone.TelematicsKernel.Test
{
    public class GeozoneModuleCoreTests : PlatformTestBase
    {
        private readonly IGeozoneModuleCore _geozoneModule;
        private readonly IGeozoneRepository _geozoneRepository;
        private readonly GeozoneCreateModelFixture _createRequestFixture;
        private readonly GeozoneUpdateModelFixture _updateRequestFixture;


        /// <summary>
        /// Constructor
        /// </summary>
        public GeozoneModuleCoreTests()
        {
            var testContext = new TelematicsKernelTestContext(AccountVariant.Default);

            _geozoneModule = testContext.GeozoneModule;
            _geozoneRepository = testContext.GeozoneRepository;
            _createRequestFixture = new GeozoneCreateModelFixture();
            _updateRequestFixture = new GeozoneUpdateModelFixture();
        }

        #region PlatformTestBase Methods

        protected override void TestCleanup()
        {
            DropCollections();
        }

        protected override void InitMapper()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<TelematicDeviceMappingProfile>();
                cfg.AddProfile<GeozoneMappingProfile>();
            });
        }

        protected override void SetMocks()
        {

        }

        /// <summary>
        /// Drop Collections Data
        /// </summary>
        private async void DropCollections()
        {
            await _geozoneRepository.Drop();
        }

        #endregion

        internal async Task<string> CreateGeozone()
        {
            var request = _createRequestFixture.Create();
            var employeeViewModel = await _geozoneModule.Create(request);
            return employeeViewModel.Data.Id.ToString();
        }

        internal async Task<GeozoneViewModel> CreateGeozoneFull()
        {
            var request = _createRequestFixture.Create();
            var employeeViewModel = await _geozoneModule.Create(request);
            return employeeViewModel.Data;
        }


        #region Create Geozone


        /// <summary>
        /// Успешное создание Геозоны
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_Success()
        {
            //Arrange
            var request = _createRequestFixture.Create();

            //Act
            var result = await _geozoneModule.Create(request);

            //Assert
            Assert.Equal(result.Code, ResultCode.Ok);
            Assert.NotNull(result.Data);
        }

        /// <summary>
        /// Нуспешное создание Геозоны - пустое поле Name
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_NotValidName_ValidationException()
        {
            //Arrange
            var request = _createRequestFixture
                .WithName(null)
                .Create();

            // Act
            Func<Task<PlatformResponse<GeozoneViewModel>>> func = async () => await _geozoneModule.Create(request);

            // Assert
            var ex = await Assert.ThrowsAsync<PlatformValidationException>(func);
            Assert.Contains("Name", ex.ErrorsStock.ToString());
        }

        /// <summary>
        /// Нуспешное создание Геозоны - пустое поле Color
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_NotValidColor_ValidationException()
        {
            //Arrange
            var request = _createRequestFixture
                .WithColor(null)
                .Create();

            // Act
            Func<Task<PlatformResponse<GeozoneViewModel>>> func = async () => await _geozoneModule.Create(request);

            // Assert
            var ex = await Assert.ThrowsAsync<PlatformValidationException>(func);
            Assert.Contains("Color", ex.ErrorsStock.ToString());
        }


        /// <summary>
        /// Нуспешное создание Геозоны - пустое поле Points
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_Points_ValidationException()
        {
            //Arrange
            var request = _createRequestFixture
                .WithPoints(null)
                .Create();

            // Act
            Func<Task<PlatformResponse<GeozoneViewModel>>> func = async () => await _geozoneModule.Create(request);

            // Assert
            var ex = await Assert.ThrowsAsync<PlatformValidationException>(func);
            Assert.Contains("Points", ex.ErrorsStock.ToString());
        }

        /// <summary>
        /// Нуспешное создание Геозоны - пустое поле Type
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_NotValidType_ValidationException()
        {
            //Arrange
            var request = _createRequestFixture
                .WithGeozoneType((GeozoneType) 75)
                .Create();

            // Act
            Func<Task<PlatformResponse<GeozoneViewModel>>> func = async () => await _geozoneModule.Create(request);

            // Assert
            var ex = await Assert.ThrowsAsync<PlatformValidationException>(func);
            Assert.Contains("Type", ex.ErrorsStock.ToString());
        }

        /// <summary>
        /// Нуспешное создание Геозоны - некорректное поле Color
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_NotValidColor_BadFields()
        {
            //Arrange
            var request = _createRequestFixture
                .WithColor("color")
                .Create();

            //Act
            var result = await _geozoneModule.Create(request);

            //Assert
            Assert.Equal(result.Code, ResultCode.BadFields);
        }

        /// <summary>
        /// Нуспешное создание Геозоны - некорректное поле Radius
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_NotValidRadius_BadFields()
        {
            //Arrange
            var request = _createRequestFixture
                .WithRadius(-27)
                .WithGeozoneType(GeozoneType.Round)
                .Create();

            //Act
            var result = await _geozoneModule.Create(request);

            //Assert
            Assert.Equal(result.Code, ResultCode.BadFields);
        }

        /// <summary>
        /// Нуспешное создание Геозоны - некорректное поле BufferThickness
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_NotValidBufferThickness_BadFields()
        {
            //Arrange
            var request = _createRequestFixture
                .WithBufferThickness(-27)
                .Create();

            //Act
            var result = await _geozoneModule.Create(request);

            //Assert
            Assert.Equal(result.Code, ResultCode.BadFields);
        }

        /// <summary>
        /// Нуспешное создание Геозоны - некорректное поле Square
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_NotValidSquare_BadFields()
        {
            //Arrange
            var request = _createRequestFixture
                .WithSquare(-27)
                .Create();

            //Act
            var result = await _geozoneModule.Create(request);

            //Assert
            Assert.Equal(result.Code, ResultCode.BadFields);
        }

        /// <summary>
        /// Нуспешное создание Геозоны - некорректное поле Points
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Create_NotValidPointsPolygon_BadFields()
        {
            //Arrange
            var pointsList = new List<PointModel>
            {
                new PointModel() {Longitude = 55.708446, Latitude = 37.654082},
                new PointModel() {Longitude = 57.708446, Latitude = 39.654082},
                new PointModel() {Longitude = 59.708446, Latitude = 41.654082},
            };

            var request = _createRequestFixture
                .WithPoints(pointsList.ToArray())
                .WithGeozoneType(GeozoneType.Polygon)
                .Create();

            //Act
            var result = await _geozoneModule.Create(request);

            //Assert
            Assert.Equal(result.Code, ResultCode.BadFields);
        }
        

        #endregion

        #region Update Geozone

        /// <summary>
        /// Успешное обновление Геозоны
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Update_Success()
        {
            //Arrange
            var id = await CreateGeozone();
            var request = _updateRequestFixture
                .WithId(id)
                .Create();

            //Act
            var result = await _geozoneModule.Update(request);

            //Assert
            Assert.Equal(result.Code, ResultCode.Ok);
            Assert.NotNull(result.Data);
        }

        /// <summary>
        /// Неупешное обновление Геозоны - пустое поле Name
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Update_NotValidName_ValidationException()
        {
            //Arrange
            var id = await CreateGeozone();
            var request = _updateRequestFixture
                .WithId(id)
                .WithName(null)
                .Create();

            // Act
            Func<Task<PlatformResponse<GeozoneViewModel>>> func = async () => await _geozoneModule.Update(request);

            // Assert
            var ex = await Assert.ThrowsAsync<PlatformValidationException>(func);
            Assert.Contains("Name", ex.ErrorsStock.ToString());
        }

        #endregion

        #region Delete Geozone

        /// <summary>
        /// Успешное удаление Геозоны
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Delete_Success()
        {
            var id1 = "59005fb98f2e293040fa9f43";
            var result1 = await _geozoneModule.Delete(id1);

            //Arrange
            var id = await CreateGeozone();


            //Act
            var result = await _geozoneModule.Delete(id);

            //Assert
            Assert.Equal(result.Code, ResultCode.Ok);
        }

        /// <summary>
        /// Неуспешное удаление Геозоны - не существует по переданному Id
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Detele_EmptyId_NotFound()
        {
            //Arrange
            var id = MongoDbObjectIdFactory.CreateRandomId();

            //Act
            var result = await _geozoneModule.Delete(id);

            //Assert
            Assert.Equal(result.Code, ResultCode.NotFound);
        }

        /// <summary>
        /// Неуспешное удаление Геозоны - некорректный Id
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_Detele_WrongId_BadFormat()
        {
            //Arrange
            var id = "1123456789";

            //Act
            var result = await _geozoneModule.Delete(id);

            //Assert
            Assert.Equal(result.Code, ResultCode.BadFormat);
        }

        #endregion

        #region Get Geozone

        /// <summary>
        /// Успешное получение Геозоны по Id
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_GetById_Success()
        {
            //Arrange
            var id = await CreateGeozone();

            //Act
            var result = await _geozoneModule.GetGeozone(id);

            //Assert
            Assert.Equal(result.Code, ResultCode.Ok);
            Assert.NotNull(result.Data);
        }

        /// <summary>
        /// Неуспешное получение Геозоны - не существует по переданному Id
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_GetById_NotFound()
        {
            //Arrange
            var id = MongoDbObjectIdFactory.CreateRandomId();

            //Act
            var result = await _geozoneModule.GetGeozone(id);

            //Assert
            Assert.Equal(result.Code, ResultCode.NotFound);
        }

        /// <summary>
        /// Неуспешное получение Геозоны - некорректный Id
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_GetById_BadFormat()
        {
            //Arrange
            var id = "123456789";

            //Act
            var result = await _geozoneModule.GetGeozone(id);

            //Assert
            Assert.Equal(result.Code, ResultCode.BadFormat);
        }

        /// <summary>
        /// Успешное получение всех Геозон
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_GetAll_Success()
        {
            //Arrange
            await CreateGeozone();
            await CreateGeozone();
            await CreateGeozone();

            //Act
            var result = await _geozoneModule.GetAll(null, null);

            //Assert
            Assert.Equal(result.Code, ResultCode.Ok);
            Assert.Equal(3, result.Data.Length);
        }

        #endregion

        /// <summary>
        /// Успешное получение координат Геозоны
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Geozone_IntersectPoint_Success()
        {
            //Arrange
            var geozone = await CreateGeozoneFull();

            //Act
            var result = await _geozoneModule.IntersectPoint(geozone.Id, geozone.Points[0]);

            //Assert
            Assert.Equal(result.Code, ResultCode.Ok);
            Assert.True(result.Data);
        }
    }
}