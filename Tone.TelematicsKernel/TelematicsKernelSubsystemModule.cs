using Tone.Core.Subsystems;
using Tone.Core.Subsystems.TelematicsKernel;

namespace Tone.TelematicsKernel
{
    public class TelematicsKernelSubsystemModule : BaseModuleCore
    {
        /// <summary>
        /// Модули подсистемы Имитационное тестирование
        /// </summary>
        /// Ссылка для упрощения программного кода в теле модуля
        /// 
        protected ITelematicsKernelSubsystemModules Modules => rc.PlatformComponents.TelematicsKernelSubsystem.Modules;

        /// <summary>
        /// Репозитории подсистемы Имитационное тестирование
        /// </summary>
        /// Ссылка для упрощения программного кода в теле модуля
        /// 
        protected ITelematicsKernelSubsystemRepositories Repositories => rc.PlatformComponents.TelematicsKernelSubsystem.Repositories;

        
        // Провайдеры подсистемы Имитационное тестирование
        // Ссылка для упрощения программного кода в теле модуля
        //protected ISecuritySubsystemProviders Providers => rc.PlatformComponents.SecuritySubsystem.Providers;
    }
}
