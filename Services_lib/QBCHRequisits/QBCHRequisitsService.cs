using Application_lib;
using Domain.QBCHRequisitsService;
using Microsoft.Extensions.Configuration;

namespace Services_lib.QBCHRequisits
{

    /// <summary>
    /// Реквиизиты БКИ
    /// </summary>
    public class QBCHRequisitsService : IQBCHRequisitsService
    {
        /// <summary>
        /// Конфигурация
        /// </summary>
        private readonly IConfiguration _config;

        /// <summary>
        /// Список Бюро
        /// </summary>
        private List<QBCHRequisite> _bureauList { get; set; } = [];

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="config">Конфигурация</param>
        public QBCHRequisitsService(IConfiguration config)
        {
            _config = config;
            foreach (var item in _config.GetSection("QBCH").GetChildren())
            {
                _bureauList.Add(new QBCHRequisite()
                {
                    Name = item.GetValue<string>("Name"),
                    Thumbprint = item.GetValue<string>("Thumbprint"),
                    Url = item.GetValue<string>("Url"),
                    ogrn = item.GetValue<string>("Ogrn")
                });
            }
        }

        /// <summary>
        /// Получить список бюро
        /// </summary>
        /// <returns>Список бюро</returns>
        public List<QBCHRequisite> GetBureaList()
        {
            return _bureauList;
        }
    }
}
