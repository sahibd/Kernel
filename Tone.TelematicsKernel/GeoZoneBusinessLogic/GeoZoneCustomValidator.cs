using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Data.Account;
using Tone.Core.Enums;
using Tone.Core.Subsystems.BusinessObjects;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.Utils;

namespace Tone.TelematicsKernel.GeoZoneBusinessLogic
{
    internal class GeozoneCustomValidator
    {   
        internal GeozoneCustomValidator()
        {  
        }

        internal async Task<ValidateResult> ValidateGeozoneModel(GeozoneRequestModel createModel)
        {
            var validate = new ValidateResult("Geozone");

            // Validate Round Geozone
            if (createModel.Type == GeozoneType.Round && !createModel.Radius.HasValue)
                validate.Message(ValidationField.Parameter, "Geozone Radius is missing");

            // Validate Radius
            var validateRadius = await CheckRadius(createModel.Radius);
            if (!validateRadius.IsValid)
                validate.Message(ValidationField.Parameter, validateRadius.ToTextBlock());

            // Validate Square
            var validateSquare = await CheckSquare(createModel.Square);
            if (!validateSquare.IsValid)
                validate.Message(ValidationField.Parameter, validateRadius.ToTextBlock());

            // Validate BorderThickness
            var validateBorder = await CheckBorderThickness(createModel.BufferThickness);
            if (!validateBorder.IsValid)
                validate.Message(ValidationField.Parameter, validateBorder.ToTextBlock());

            // Validate Color
            var colorResult = GetGeozoneColor(createModel.Color);
            if (colorResult == null)
                validate.Message(ValidationField.Parameter, "Geozone Color is wrong");
            
            if (!validate.IsValid)
                return await Task.Run(() => validate);

            // Validate Points
            var requiredPointsCount = GeozoneTypePointCount.GetGeozoneTypePointCount(createModel.Type);
            if (requiredPointsCount.MinPointsCount == 0)
                validate.Message(ValidationField.Geozone, "Geozone's Points count not match with Geozone's Type");

            // Validate Points Count
            var pointsCount = createModel.Points?.Length;
            if (!(requiredPointsCount.MinPointsCount <= pointsCount && pointsCount <= requiredPointsCount.MaxPointsCount))
                validate.Message(ValidationField.Geozone, "Geozone's Points count not match with Geozone's Type");

            // Validate Points Logic
            if (validate.IsValid)
            {
                var validatePoints = await ValidatePoints(createModel.Points, createModel.Type);
                if (!validatePoints.IsValid)
                    validate.Message(ValidationField.Parameter, validatePoints.ToTextBlock());
            }

            return await Task.Run(() => validate);
        }

        /// <summary>
        /// Get Geozone's Color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        internal string GetGeozoneColor(string color) => ObjectColors.GetColors().FirstOrDefault(c => c == color);

        /// <summary>
        /// Проверка уникальности координат
        /// </summary>
        /// <param name="points"></param>
        /// <param name="geozoneType"></param>
        /// <returns></returns>
        internal async Task<ValidateResult> ValidatePoints(PointModel[] points, GeozoneType? geozoneType)
        {
            var validate = new ValidateResult("GeozonePoints");
            if (points == null || !points.Any())
            {
                validate.Message(ValidationField.Parameter, "Points array is empty");
                return await Task.Run(() => validate);
            }

            var uniqueCount = geozoneType == GeozoneType.Polygon ? 2 : 1;
            var uniquePointsList = points.Distinct().ToList();

            if (geozoneType == GeozoneType.Polygon && uniquePointsList.Count == points.Length)
            {
                validate.Message(ValidationField.Parameter, "Points array has wrong data");
                return await Task.Run(() => validate);
            }

            if ((geozoneType == GeozoneType.Round || geozoneType == GeozoneType.Linear) && uniquePointsList.Count == points.Length)
                return await Task.Run(() => validate);


            var checlList = new List<int>();
            foreach (var pointModel in uniquePointsList)
            {
                var uniquePointCount = points.Where(p => p.Latitude == pointModel.Latitude && p.Longitude == pointModel.Longitude).Count();
                if (uniquePointCount != 1)
                    checlList.Add(uniquePointCount);
            }

            if (!checlList.Any())
                return await Task.Run(() => validate);

            var isCorrect = checlList.Count(c => c >= uniqueCount);
            if (isCorrect != 1)
                validate.Message(ValidationField.Parameter, "Points array has wrong data");

            return await Task.Run(() => validate);
        }

        internal async Task<ValidateResult> CheckRadius(double? radius)
        {
            var validate = new ValidateResult("GeozoneRadius");
            if (radius == null)
                return await Task.Run(() => validate);

            double radiusResult;
            if (!double.TryParse(radius.Value.ToString(CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out radiusResult))
                validate.Message(ValidationField.Parameter, "Geozone Radius is wrong");

            if (radiusResult <= 0)
                validate.Message(ValidationField.Parameter, "Geozone Radius is wrong");
            return await Task.Run(() => validate);
        }

        internal async Task<ValidateResult> CheckBorderThickness(double? borderThickness)
        {
            var validate = new ValidateResult("GeozoneBorderThickness");
            if (borderThickness == null)
                return await Task.Run(() => validate);


            double borderThicknessResult;
            if (!double.TryParse(borderThickness.Value.ToString(CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out borderThicknessResult))
                validate.Message(ValidationField.Parameter, "Geozone BorderThickness is wrong");
            if (borderThicknessResult <= 0)
                validate.Message(ValidationField.Parameter, "Geozone BorderThickness is wrong");
            return await Task.Run(() => validate);
        }

        internal async Task<ValidateResult> CheckSquare(double? square)
        {
            var validate = new ValidateResult("GeozoneSquare");
            if (square == null)
                return await Task.Run(() => validate);
            double squareResult;
            if (!double.TryParse(square.Value.ToString(CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out squareResult))
                validate.Message(ValidationField.Parameter, "Geozone Square is wrong");
            if (squareResult <= 0)
                validate.Message(ValidationField.Parameter, "Geozone Square is wrong");
            return await Task.Run(() => validate);
        }        
    }
}
