using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MongoDB.Bson;
using Tone.Core.Data;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel;

namespace Tone.TelematicsKernel.Helpers
{
    public static class DeviceBuilder
    {
        #region BuildFuelRules

        public static List<FuelRule<object>> BuildFuelRules(List<FuelRule<object>> fuelRules)
        {
            var ruleList = new List<FuelRule<object>>();
            foreach (var rule in fuelRules)
            {
                var newRule = new FuelRule<object>();

                if (rule.RuleType == FuelRuleType.SearchingDrain || rule.RuleType == FuelRuleType.SearchingRefuel)
                    FuelRule<object>.SetBoolRule(newRule, rule);
                else
                    FuelRule<object>.SetDoubleRule(newRule, rule);

                ruleList.Add(newRule);
            }

            return ruleList;
        }

        #endregion

        #region BuildFuelSources

        /// <summary>
        /// 
        /// </summary>
        public static List<FuelSource> BuildFuelSources(IDevice device, List<FuelSource> fuelSourceList)
        {
            var currentFuelSources = device.FuelSetting.FuelSources;

            foreach (var fuelSource in fuelSourceList)
            {
                var currentSource = (fuelSource.EntityStatus != null && fuelSource.EntityStatus != EntityStatus.New && fuelSource.Id != null)
                    ? currentFuelSources.FirstOrDefault(s => s.Id.ToString() == fuelSource.Id.ToString())
                    : null;

                switch (fuelSource.EntityStatus)
                {
                    case EntityStatus.New:
                        fuelSource.EntityStatus = null;
                        fuelSource.Id = ObjectId.GenerateNewId();
                        currentFuelSources.Add(fuelSource);
                        break;
                    case EntityStatus.Modify:
                        if (currentSource != null)
                            SetFuelSourceItem(currentSource, fuelSource);
                        break;
                    case EntityStatus.Delete:
                        if (currentSource != null)
                            currentFuelSources.Remove(currentSource);
                        break;
                }
            }

            var priorityFuelSource = fuelSourceList.FirstOrDefault(f => f.IsPriority);
            if (priorityFuelSource != null)
                SetPriority(currentFuelSources.Where(f => f.Id != priorityFuelSource.Id && f.IsPriority).ToList());

            return currentFuelSources;
        }

        #endregion
        

        //todo Два нижних метода нужно будет объединить
        //
        public static List<DeviceParameterBase> BuildDeviceParameters(IDevice device,
            List<CalibrationSetting> settingsList)
        {
            var currentParameters = device.GetParamsDictionary();

            foreach (var t in settingsList)
            {
                DeviceParameterBase currentValue;
                if (!currentParameters.TryGetValue(t.Type, out currentValue)) continue;

                var currentMapping = currentValue as CalibrationParameter;
                if (currentMapping == null) continue;

                currentMapping.IsEnabled = t.Enabled;
                currentMapping.Coefficient1 = t.Coefficient1;
                currentMapping.Coefficient2 = t.Coefficient2;

                currentParameters[t.Type] = currentMapping;
            }

            return currentParameters.Values.ToList();
        }

        public static List<DeviceParameterBase> BuildDeviceParameters(IDevice device,
            List<DeviceParameter> settingsList)
        {
            var currentParameters = device.GetParamsDictionary();

            foreach (var t in settingsList)
            {
                DeviceParameterBase currentValue;
                if (!currentParameters.Any() || !currentParameters.TryGetValue(t.Type, out currentValue))
                    continue;

                currentValue.ViewEnabled = t.ViewEnabled;
                currentParameters[t.Type] = currentValue;
            }

            return currentParameters.Values.ToList();
        }

        #region BuildDeviceParameters

        public static List<DeviceParameterBase> BuildDeviceParameters(IDevice device, List<DeviceParameterBase> parameterList)
        {
            var currentParameterList = device.Mapping;
	        currentParameterList.RemoveAll(x => !parameterList.Select(y => y.SourceName).Contains(x.SourceName));

	        var currentParameters = currentParameterList.ToDictionary(x => x.SourceName);

			foreach (var p in parameterList)
            {
	            if (currentParameters.ContainsKey(p.SourceName))
	            {
		            var currentParameter = currentParameters[p.SourceName];
		            currentParameter.SourceName = p.SourceName;
		            currentParameter.Unit = p.Unit;
		            currentParameter.IsBool = p.IsBool;
		            currentParameter.ViewName = p.ViewName;
		            currentParameter.IsAvailable = p.IsAvailable;
		            currentParameter.IsExtended = true;
	            }
	            else
				{
					if (p.IsBool)
						currentParameters.Add(p.SourceName, new BoolParameter
						{
							SourceName = p.SourceName,
							Unit = p.Unit,
							IsBool = true,
							ViewName = p.ViewName,
							IsExtended = true,
							IsAvailable = p.IsAvailable
						});
					else
						currentParameters.Add(p.SourceName, new CalibrationParameter
						{
							SourceName = p.SourceName,
							Unit = p.Unit,
							IsBool = false,
							ViewName = p.ViewName,
							IsExtended = true,
							IsAvailable = p.IsAvailable
						});
				}
            }

            return currentParameters.Values.ToList();
        }
        #endregion

        private static void SetPriority(List<FuelSource> currentFuelSources)
        {
            if (currentFuelSources == null || !currentFuelSources.Any())
                return;
            currentFuelSources.ForEach(fuelSource => { fuelSource.IsPriority = false; });
        }

        private static void SetFuelSourceItem(FuelSource currentfuelSource, FuelSource fuelSource)
        {
            currentfuelSource = Mapper.Map(fuelSource, currentfuelSource);
            currentfuelSource.TaringTable = fuelSource.TaringTable;
            currentfuelSource.ErrorTable = fuelSource.ErrorTable;
            currentfuelSource.EntityStatus = null;
            if(currentfuelSource.Id==null)
                currentfuelSource.Id= ObjectId.GenerateNewId();
        }
    }
}
