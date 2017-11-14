using Tone.Core;
using Tone.Core.Subsystems.TelematicsKernel;

namespace Tone.TelematicsKernel
{
    public class TelematicsKernelSubsystem : ITelematicsKernelSubsystem
    {
        public ITelematicsKernelSubsystemModules Modules { get; }

        public ITelematicsKernelSubsystemRepositories Repositories { get; }

        public ITelematicsKernelSubsystemProviders Providers { get; }

        /// <summary>
        /// Контекст запроса
        /// </summary>
        private IRequestContext _rc;

        public TelematicsKernelSubsystem(ITelematicsKernelSubsystemModules modules, ITelematicsKernelSubsystemRepositories repositories,
            ITelematicsKernelSubsystemProviders providers)
        {
            Modules = modules;
            Repositories = repositories;
            Providers = providers;
        }

        /// <summary>
        /// Установка связи с контекстом запроса
        /// </summary>
        /// <param name="requestContext"></param>
        public void SetRequestContext(IRequestContext requestContext)
        {
            _rc = requestContext;
            Modules.SetRequestContext(_rc); // Устанавливаем контекст запроса для модулей
            Repositories.SetRequestContext(_rc); // Устанавливаем контекст запроса для репозиториев
        }

        public void InitInterconnections()
        {
            Modules.InitInterconnections();
        }
    }
}
