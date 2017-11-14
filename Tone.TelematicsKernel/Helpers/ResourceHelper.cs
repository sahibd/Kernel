//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Tone.Core.Data;
//using Tone.Core.Enums;
//using Tone.Core.Subsystems.BusinessObjects;
//using Tone.Core.Subsystems.BusinessObjects.Model;
//using Tone.Core.Subsystems.BusinessObjects.Repositories;
//using Tone.Core.Subsystems.Security.Model;
//using Tone.Core.Subsystems.TelematicsKernel;

//namespace Tone.TelematicsKernel.Helpers
//{
//    public class ResourceHelper
//    {
//        private readonly AccountSettings _accountSettings;
//        private readonly IEmployeesRepository _employeesRepository;
//        private readonly IVehicleRepository _vehicleRepository;
//        private readonly IOmGroupsRepository _groupsRepository;
//        private readonly List<VehicleViewCardArgs> _vehicleViewSortedFields;
//        private readonly List<EmployeeViewCardArgs> _employeeViewSortedFields;
//        private const string Symbol = " | ";

//        public ResourceHelper(AccountSettings accountSettings,ITelematicsKernelSubsystemRepositories repositories)
//        {
//            _accountSettings = accountSettings ?? AccountSettings.GetDefault();

//            _groupsRepository = repositories.OmGroups;
//            _employeesRepository = repositories.Employees;
//            _vehicleRepository = repositories.Vehicles;

//            _vehicleViewSortedFields = GetVehicleViewSortedFields();
//            _employeeViewSortedFields = GetEmployeeViewSortedFields();
//        }


//        #region GenerateEmployeeResourceViewCard

//        public async Task<ResourceViewCard> GenerateEmployeeResourceViewCard(IDevice device)
//        {
//            var employee = await _employeesRepository.GetById(device.ResourceId);
//            if (employee == null || _employeeViewSortedFields == null)
//                return null;

//            var employeeResourceViewCard = new ResourceViewCard()
//            {
//                Id = employee.Id,
//                AvatarUri = _employeeViewSortedFields.Contains(EmployeeViewCardArgs.AvatarUri) ? employee.AvatarUri : null,
//                PrimaryTitle = null,
//                SecondaryTitle = null,
//                IsOnline = false,
//                RelatedResourceType = RelatedResourceType.Employee,
//                IsDeactivated = employee.IsDeactivated,
//                DeactivatedDate = employee.DeactivatedDate
//            };

//            var isAvatarExists = employeeResourceViewCard.AvatarUri != null;
//            var idx = isAvatarExists ? 1 : 0;
//            var count = isAvatarExists ? 1 : 0;

//            foreach (var args in _employeeViewSortedFields)
//            {
//                if (args == EmployeeViewCardArgs.AvatarUri)
//                    continue;

//                var result = _employeeViewSortedFields.Contains(args) ? await GetEmployeeiewCardData(args, employee, device) : null;
//                if (idx == count)
//                {
//                    employeeResourceViewCard.PrimaryTitle = result;
//                    idx++;
//                    continue;
//                }
//                if (idx > count)
//                    employeeResourceViewCard.SecondaryTitle += (result != null) ? result + Symbol : null;
//            }

//            employeeResourceViewCard.SecondaryTitle = !string.IsNullOrEmpty(employeeResourceViewCard.SecondaryTitle) ? TrimEnd(employeeResourceViewCard.SecondaryTitle) : null;
//            return employeeResourceViewCard;
//        }

//        #endregion


//        #region GenerateVehicleResourceViewCard

//        public async Task<ResourceViewCard> GenerateVehicleResourceViewCard(IDevice device)
//        {
//            var vehicle = await _vehicleRepository.GetById(device.ResourceId);
//            if (vehicle == null || _vehicleViewSortedFields == null)
//                return null;

//            var vehicleResourceViewCard = new ResourceViewCard
//            {
//                Id = vehicle.Id,
//                AvatarUri = _vehicleViewSortedFields.Contains(VehicleViewCardArgs.AvatarUri) ? vehicle.AvatarUri : null,
//                PrimaryTitle = null,
//                SecondaryTitle = null,
//                IsOnline = false,
//                RelatedResourceType = RelatedResourceType.Vehicle,
//                IsDeactivated = vehicle.IsDeactivated,
//                DeactivatedDate = vehicle.DeactivatedDate,
//            };

//            var isAvatarExists = vehicleResourceViewCard.AvatarUri != null;
//            var idx = isAvatarExists ? 1 : 0;
//            var count = isAvatarExists ? 1 : 0;

//            foreach (var args in _vehicleViewSortedFields)
//            {
//                if (args == VehicleViewCardArgs.AvatarUri)
//                    continue;

//                var result = _vehicleViewSortedFields.Contains(args) ? await GetVehicleViewCardData(args, vehicle, device) : null;
//                if (idx == count)
//                {
//                    vehicleResourceViewCard.PrimaryTitle = result;
//                    idx++;
//                    continue;
//                }
//                if (idx > count)
//                    vehicleResourceViewCard.SecondaryTitle += (result != null) ? result + Symbol : null;
//            }
//            vehicleResourceViewCard.SecondaryTitle = !string.IsNullOrEmpty(vehicleResourceViewCard.SecondaryTitle) ? TrimEnd(vehicleResourceViewCard.SecondaryTitle) : null;
//            return vehicleResourceViewCard;
//        }

//        #endregion

//        #region Extended Methods

//        public List<VehicleViewCardArgs> GetVehicleViewSortedFields()
//        {
//            var result = _accountSettings.ObjectViewSettings.VehicleViewFields.GetArgsSorted();
//            return result;
//        }

//        public List<EmployeeViewCardArgs> GetEmployeeViewSortedFields()
//        {
//            var result = _accountSettings.ObjectViewSettings.EmployeeViewFields.GetArgsSorted();
//            return result;
//        }

//        private async Task<string> GetEmployeeiewCardData(EmployeeViewCardArgs args, IEmployee employee, IDevice device)
//        {
//            switch (args)
//            {
//                case EmployeeViewCardArgs.FullName:
//                    return employee.GetName();
//                case EmployeeViewCardArgs.Phone:
//                    return employee.Phone;
//                case EmployeeViewCardArgs.Type:
//                    return employee.Type.ToString();
//                case EmployeeViewCardArgs.Group:
//                    return await GetGroup(employee.GroupId);
//                case EmployeeViewCardArgs.Device:
//                    return GetDevice(device);
//                case EmployeeViewCardArgs.None:
//                case EmployeeViewCardArgs.AvatarUri:
//                    return null;
//                default:
//                    return null;
//            }
//        }

//        private async Task<string> GetGroup(object groupId)
//        {
//            if (groupId == null)
//                return null;
//            var group = await _groupsRepository.GetById(groupId);
//            return group?.Name;
//        }

//        private string GetDevice(IDevice device) => device?.Model;


//        private async Task<string> GetVehicleViewCardData(VehicleViewCardArgs args, IVehicle vehicle, IDevice device)
//        {
//            switch (args)
//            {
//                case VehicleViewCardArgs.Name:
//                    return vehicle.Name;
//                case VehicleViewCardArgs.MarkModelNumber:
//                    return GetMarkModelNumber(vehicle);
//                case VehicleViewCardArgs.GarageNumber:
//                    return vehicle.GarageNumber;
//                case VehicleViewCardArgs.Group:
//                    return await GetGroup(vehicle.GroupId);
//                case VehicleViewCardArgs.Device:
//                    return GetDevice(device);
//                case VehicleViewCardArgs.None:
//                case VehicleViewCardArgs.AvatarUri:
//                    return null;
//                default:
//                    return null;
//            }
//        }

//        private string GetMarkModelNumber(IVehicle vehicle)
//        {
//            string result = vehicle.Mark;
//            result += GetFieldData(vehicle.Model);
//            result += GetFieldData(vehicle.Number);
//            return result;
//        }

//        private string GetFieldData(string fieldData) => !string.IsNullOrEmpty(fieldData) ? Symbol + fieldData : "";

//        private string TrimEnd(string title) => title != null && title.EndsWith(Symbol) ? title.TrimEnd(' ', '|', ' ') : title;

//        #endregion
//    }
//}
