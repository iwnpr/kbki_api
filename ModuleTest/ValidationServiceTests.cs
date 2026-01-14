using Cache_lib.Implementations;
using Cache_lib.Interfaces;
using Crypto_lib.Service;
using KafkaService_lib.Services.Implementation;
using KafkaService_lib.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QBCH_api.Services.Implementations;
using QBCH_api.Services.Interfaces;
using Qbch_db_lib.Services.Implementations;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Services.Implementations;
using QBCH_lib.Services.Interfaces;
using QBCHService_lib.Services.Implementations;
using QBCHService_lib.Services.Interfaces;
using Serilog;
using StackExchange.Redis;
using XmlService_lib.Services.Implementations;
using XmlService_lib.Services.Interfaces;

namespace ModuleTest
{
    [TestClass]
    public class ValidationServiceTests
    {
        private readonly IValidationService _validationService = null!;

        /// <summary>
        /// Конструктор
        /// </summary>
        public ValidationServiceTests()
        {
            //Подключаем файл конфигурации
            IConfiguration _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json")
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton(_configuration);
            services.AddTransient<IValidationService, ValidationService>();
            services.AddTransient<ICryptoService, CryptoService>();
            services.AddTransient<IXmlService, XmlService>();
            services.AddTransient<IValidationService, ValidationService>();
            services.AddTransient<IRepository, Repository>();
            services.AddTransient<IQBCHService, QBCHService>();
            services.AddTransient<ITransformer, Transformer>();
            services.AddTransient<ITicketService, TicketService>();
            services.AddSingleton<ICompressService, CompressService>();
            services.AddTransient<ICacheService, CacheService>();
            services.AddSingleton<IBKIRequisitsHandler, BKIRequsits>();
            services.AddSingleton<IKafkaService, KafkaService>();
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(_configuration.GetConnectionString("Redis")));
            var serilog = new LoggerConfiguration().ReadFrom.Configuration(_configuration).CreateLogger();
            services.AddLogging(x => x.AddSerilog(serilog));
            services.AddMemoryCache();

            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);

            var serviceProvider = services.BuildServiceProvider();
            _validationService = serviceProvider.GetRequiredService<IValidationService>();
        }

        /// <summary>
        /// Валидация проверки даты
        /// </summary>
        [TestMethod]
        public void ValidateRequestDateTest()
        {
            for (int i = 0; i < 10; i++)
            {
                var day = RandomDay();
                bool IsToday = day == DateTime.Today;
                Assert.AreEqual(IsToday, _validationService.ValidateRequestDate(day, out _));
            }
        }

        /// <summary>
        /// Получить рандомную дату
        /// </summary>
        /// <returns>Рандомная дата</returns>
        private static DateTime RandomDay()
        {
            var gen = new Random();
            DateTime today = DateTime.Today;
            return today.AddDays(gen.Next(-3, 3));
        }
    }
}
