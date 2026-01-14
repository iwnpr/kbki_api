namespace Crypto_lib.Model
{
    /// <summary>
    /// Субъект сертификата
    /// </summary>
    public class ParsedSubject
    {
        /// <summary>
        /// Страна
        /// </summary>
        public string? CountryName { get; set; }

        /// <summary>
        /// Регион
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Местонахождение
        /// </summary>
        public string? Locality { get; set; }

        /// <summary>
        /// Наименование организации
        /// </summary>
        public string? OrganizationName { get; set; }

        /// <summary>
        /// Департамент
        /// </summary>
        public string? OrganizationalUnitName { get; set; }

        /// <summary>
        /// Доменное имени или имя хоста
        /// </summary>
        public string? CommonName { get; set; }

        /// <summary>
        /// Эл.почта
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Улица
        /// </summary>
        public string? Street { get; set; }

        /// <summary>
        /// Инн
        /// </summary>
        public string? Inn { get; set; }

        /// <summary>
        /// Инн
        /// </summary>
        public string? InnLE { get; set; }

        /// <summary>
        /// ОГРН или ОГРНИП
        /// </summary>
        public string? Ogrn { get; set; }


        /// <summary>
        /// ОГРН или ОГРНИП
        /// </summary>
        public string? OgrnIP { get; set; }

        /// <summary>
        /// СНИЛС
        /// </summary>
        public string? Snils { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        public string? OwnerLastName { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string? OwnerFirstName { get; set; }
    }
}
