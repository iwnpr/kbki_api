using System;
using QBCH_lib.core;

namespace QBCH_lib.domain.entities;

/// <summary>
/// Вложение
/// </summary>
public sealed class Attachment : Entity
{
    /// <summary>
    /// Подписанное тело запроса RAW
    /// </summary>
    public byte[]? SignedRequestBody { get; private set; }

    /// <summary>
    /// Тело запроса без подписи
    /// </summary>
    public byte[]? RequestBody { get; private set; }

    /// <summary>
    /// Отпечаток подписи файла
    /// </summary>
    public string? SignThumbprint { get; private set; }

    /// <summary>
    /// ИНН подписи файла
    /// </summary>
    public string? SignINN { get; private set; }

    /// <summary>
    /// ОГРН подписи файла
    /// </summary>
    public string? SignOGRN { get; private set; }

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="id">Идентификатор</param>
    protected Attachment(Guid id) : base(id)
    {

    }

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <param name="signedRequest"></param>
    protected Attachment(Guid id, byte[] signedRequest) : base(id)
    {
        SignedRequestBody = signedRequest;
    }

    /// <summary>
    /// Создать вложение
    /// </summary>
    /// <param name="signedRequest"></param>
    /// <returns></returns>
    public static Attachment Create(byte[] signedRequest)
    {
        if (signedRequest is null)
        {
            return new Attachment(Guid.NewGuid());
        }

        return new Attachment(Guid.NewGuid(), signedRequest);
    }

    /// <summary>
    /// Уставновить данные серта
    /// </summary>
    /// <param name="signThumbprint"></param>
    /// <param name="signInn"></param>
    /// <param name="signOgrn"></param>
    public void SetSignCertificateData(string? signThumbprint, string? signInn, string? signOgrn)
    {
        SignThumbprint = signThumbprint;
        SignINN = signInn;
        SignOGRN = signOgrn;
    }

    /// <summary>
    /// Установить данные тела запроса
    /// </summary>
    /// <param name="unsignedRequestBody"></param>
    public void SetRequestBody(byte[] unsignedRequestBody)
    {
        if (unsignedRequestBody != null && RequestBody is null)
        {
            RequestBody = unsignedRequestBody;
        }
    }
}

