using System;
using Nancy.Extensions;
using Nancy.Security;
using ServiceStack.Text;
using Tone.Core;
using Tone.Core.Data;
using Tone.Core.Data.Account;
using Tone.Core.Enums;
using Tone.Core.Exceptions;
using Tone.Core.Subsystems.BusinessObjects;
using Tone.Core.Subsystems.Imitation;
using Tone.Core.Subsystems.Messaging;
using Tone.Core.Subsystems.Reports;
using Tone.Core.Subsystems.Security;
using Tone.Core.Subsystems.TechSupport;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Web.Base;

namespace Tone.TelematicsKernel.Nancy
{
    public abstract class NancyTelematicsKernelSubsystemModule : PlatformModuleNancy
    {
        /// <summary>
        /// Модули подсистемы Телематика
        /// </summary>
        /// Ссылка для упрощения программного кода в теле модуля
        /// 
        protected ITelematicsKernelSubsystemModules Modules => RequestContext.PlatformComponents.TelematicsKernelSubsystem.Modules;

        /// <summary>
        /// Репозитории подсистемы Телематика
        /// </summary>
        /// Ссылка для упрощения программного кода в теле модуля
        /// 
        protected ITelematicsKernelSubsystemRepositories Repositories => RequestContext.PlatformComponents.TelematicsKernelSubsystem.Repositories;

        public NancyTelematicsKernelSubsystemModule(
            NancyAuthorization nancyAuthorization = NancyAuthorization.On) : this(string.Empty, nancyAuthorization) { }

        public NancyTelematicsKernelSubsystemModule(string modulePath, 
            NancyAuthorization nancyAuthorization = NancyAuthorization.On) : 
            base(modulePath, nancyAuthorization)
        {
            BuildUpSubsystemReferences(new Type[]
             {
                typeof(ISecuritySubsystem),
                typeof(IBusinessObjectsSubsystem),
                typeof(ITelematicsKernelSubsystem),
                typeof(IReportsSubsystem)
             });
        }

        /// <summary>
        /// Parse Request Body to Json
        /// </summary>
        /// <returns></returns>
        protected JsonObject GetJsonObject()
        {
            return JsonObject.Parse(Request.Body.AsString());
        }

        /// <summary>
        /// Check Validation Result
        /// </summary>
        /// <param name="validation"></param>
        /// <returns></returns>
        protected bool CheckValidation(ValidateResult validation)
        {
            if (!validation.IsValid)
                throw new BadFieldsException(validation);

            return true;
        }
    }
}
