using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace QBCH_lib.CommonTypes.Api
{
    /// <summary>
    /// Интерфейс для взаимодействия с данными бюро в конфигах
    /// </summary>
    public interface IBKIRequisitsHandler
    {
        /// <summary>
        /// Получить список бюро
        /// </summary>
        /// <returns>Список бюро</returns>
        public List<QBCHRequisite> GetBureaList();
    }

    /// <summary>
    /// Реквиизиты БКИ
    /// </summary>
    public class BKIRequsits : IBKIRequisitsHandler
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
        public BKIRequsits(IConfiguration config)
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

    /// <summary>
    /// Реквизиты БКИ
    /// </summary>
    public class QBCHRequisite : Requisites
    {
        /// <summary>
        /// Наименование Бюро
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Id бюро
        /// </summary>
        public string? Thumbprint { get; set; }

        /// <summary>
        /// Базовый для запросов
        /// </summary>
        public string? Url { get; set; }
    }
}
