using Microsoft.Extensions.Configuration;
using QBCH_lib.qcb_xml.v3_0.Enums;
using QBCH_lib.Services.Implementations;

namespace ModuleTest
{
    [TestClass]
    public class TicketServiceTest
    {
        /// <summary>
        /// Проверка создания тикетов с ошибками
        /// </summary>
        [TestMethod]
        public void TicketCreatingTest()
        {
            for (int i = 0; i < 10; i++)
            {
                var psrn = "1057747734934";
                var code = Random.Shared.Next(0, 1000);
                var text = Guid.NewGuid().ToString();

                var myConfiguration = new Dictionary<string, string>
                {
                    {"Bureau:PSRN", psrn}
                };

                var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

                var _ticketService = new TicketService(configuration);
                var ticket = _ticketService.CreateResult(ResponseType.Error, code.ToString(), text);

                Assert.IsNotNull(ticket);
                Assert.AreEqual(ticket.ОГРН, "1057747734934");
                Assert.AreEqual(ticket.Версия, "1.2");
            }
        }
    }
}