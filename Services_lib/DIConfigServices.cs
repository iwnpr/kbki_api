using Application_lib;
using Microsoft.Extensions.DependencyInjection;
using Services_lib.CertManagement;
using Services_lib.Ticket;
using Services_lib.Xml;
using Services_lib.QBCH;
using Services_lib.QBCHRequisits;

namespace Services_lib
{
    public static class DIConfigServices
    {
        /// <summary>
        /// Расширение для конфигурирования адаптеров
        /// </summary>
        /// <param name="services">Сервисы</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection DIAddServices(this IServiceCollection services)
        {
            services.AddTransient<ICertManagementService, CertManagementService>();
            services.AddTransient<IXmlService, XmlService>();
            services.AddTransient<ITicketService, TicketService>();
            services.AddSingleton<IQBCHRequisitsService, QBCHRequisitsService>();
            services.AddTransient<IQBCHService, QBCHService>();

            return services;
        }
    }
}
