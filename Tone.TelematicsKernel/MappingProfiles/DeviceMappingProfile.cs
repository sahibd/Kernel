using MongoDB.Bson;
using System;
using System.ComponentModel;
using Tone.Core;
using Tone.Core.Data;
using Tone.Core.Data.NewTrackEvent;
using Tone.Core.Enums;
using Tone.Core.Extensions;
using Tone.Core.Subsystems.BusinessObjects;
using Tone.Core.Subsystems.BusinessObjects.Model;
using Tone.Core.Subsystems.TelematicsKernel;

namespace Tone.TelematicsKernel.MappingProfiles
{
    public sealed class TelematicDeviceMappingProfile : PlatformAutomapperProfile
    {
        public TelematicDeviceMappingProfile()
        {
            #region Mapping
            CreateMap<Tone.Data.Mongo.Model.Device, DeviceTelematicsViewModel>()
                .ForMember(d => d.SignalQuality, m => m.MapFrom(s => s.SignalLevel))
                .Ignore(d => d.RelatedResourceName)
                .Ignore(d => d.RelatedResource)
                .Ignore(d => d.IsActive)
                .Ignore(d => d.IsRelated);

            CreateMap<RelatedResourceModel, ResourceItem>()
                .ForMember(d => d.Id, m => m.MapFrom(s => (ObjectId) s.Id));

            CreateMap<IEmployee, RelatedResourceModel>()
                .ForMember(d => d.Name, m => m.MapFrom(s => s.GetName()))
                .ForMember(d => d.PrimaryTitle, m => m.MapFrom(s => s.GetName()))
                .ForMember(d => d.SecondaryTitle,
                    m => m.MapFrom(s => "ID " + new Random().Next(1000000, 10000001))) // todo заглушка todo: заглушка
                .ForMember(d => d.ResourceType, m => m.MapFrom(s => RelatedResourceType.Employee));

            CreateMap<IVehicle, RelatedResourceModel>()
                .ForMember(d => d.SecondaryTitle, m => m.MapFrom(s => s.Model + " " + s.Mark + " " + s.Number))
                .ForMember(d => d.PrimaryTitle, m => m.MapFrom(s => s.Name))
                .ForMember(d => d.ResourceType, m => m.MapFrom(s => RelatedResourceType.Vehicle));

            CreateMap<Tone.Data.Mongo.Model.Device, DeviceCustomersViewModel>()
                .ForMember(d => d.SignalQuality, m => m.MapFrom(s => s.SignalLevel))
                .Ignore(d => d.Customer)
                .Ignore(d => d.IsCustomerBound);

            CreateMap<ITrackEvent, RoadEventViewModel>()
                .ForMember(d => d.ObjectId, m => m.MapFrom(s => s.GetObjectIdValue()))
                .ForMember(d => d.ActualValue, m => m.MapFrom(s => s.GetActualValue()))
                .ForMember(d => d.ConditionalValue, m => m.MapFrom(s => s.GetConditionalValue()))
                .ForMember(d => d.EventTime, m => m.MapFrom(s => s.EventDeviceTime))
                .ForMember(d => d.Border, m => m.MapFrom(s => s.Border))
                .ForMember(d => d.RoadEventType, m => m.MapFrom(s => s.EventType))
                .Ignore(d => d.ObjectName)
                .Ignore(d => d.RelatedResourceName)
                .Ignore(d => d.RelatedResourceType);

            CreateMap<DeviceRequestModel, IDevice>()
                .Ignore(d => d.Id)
                .Ignore(d => d.SignalLevel)
                .Ignore(d => d.IsDeactivated)
                .Ignore(d => d.Commands)
                .Ignore(d => d.UpdateDate)
                .Ignore(d => d.ExtendedInfo)
                .Ignore(d => d.DeviceMetadata)
                .Ignore(d => d.CustomerId)
                .Ignore(d => d.UpdateAccountId)
                .Ignore(d => d.ResourceId)
                .Ignore(d => d.ResourceType)
                .Ignore(d => d.FuelSetting);

	        CreateMap<FuelSource, FuelSource>()
		        .Ignore(d => d.TaringTable)
		        .Ignore(d => d.ErrorTable);

	        #endregion
        }

        //private static string GetEnumValueDescription(Type enumType, string enumValue)
        //{
        //    var enumTypeInfo = enumType.GetMember(enumValue);
        //    if (enumTypeInfo.Length > 0)
        //    {
        //        var enumAttributes = enumTypeInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
        //        if (enumAttributes.Length > 0)
        //            return ((DescriptionAttribute)enumAttributes[0]).Description;
        //    }
        //    return "Описание отсутствует";
        //}

        //private static string GetEnumValueMeasureName(Type enumType, string enumValue)
        //{
        //    var enumTypeInfo = enumType.GetMember(enumValue);
        //    if (enumTypeInfo.Length > 0)
        //    {
        //        var enumAttributes = enumTypeInfo[0].GetCustomAttributes(typeof(MeasureNameAttribute), false);
        //        if (enumAttributes.Length > 0)
        //            return ((MeasureNameAttribute)enumAttributes[0]).Name;
        //    }
        //    return "";
        //}
    }
}