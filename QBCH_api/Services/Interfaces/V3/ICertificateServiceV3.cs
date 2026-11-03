namespace QBCH_api.Services.Interfaces.V3;

/// <summary>Чтение и изменение состояния сертификатов API 3.0.</summary>
public interface ICertificateServiceV3
{
    /// <summary>Проверяет наличие сертификата в хранилище.</summary>
    /// <param name="cert">Байты сертификата.</param>
    /// <returns>Признак наличия сертификата.</returns>
    Task<bool> IsCertExistsV3(byte[] cert);
    /// <summary>Проверяет активность сертификата в хранилище.</summary>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <returns>Признак активности сертификата.</returns>
    Task<bool> IsCertActiveV3(string thumbprint);
    /// <summary>Получает количество действующих сертификатов абонента по переданному сертификату.</summary>
    /// <param name="cert">Байты сертификата для определения абонента.</param>
    /// <returns>Количество сертификатов; 0 для пустого массива.</returns>
    Task<int> GetActiveCertificatesCountV3(byte[] cert);
    /// <summary>Делает сертификат неактивным в хранилище.</summary>
    /// <param name="cert">Байты отзываемого сертификата.</param>
    /// <returns>Признак выполнения отзыва; false для пустого массива.</returns>
    Task<bool> SetCertificateInactiveV3(byte[] cert);
}
