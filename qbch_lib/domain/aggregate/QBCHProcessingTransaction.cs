using QBCH_lib.CommonTypes.Api;
using QBCH_lib.core;
using QBCH_lib.domain.DTOs;
using QBCH_lib.domain.entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace QBCH_lib.domain.aggregate;

/// <summary>
/// 
/// </summary>
public sealed class QBCHProcessingTransaction : AggregateRoot
{
    /// <summary>
    /// Имя сервиса
    /// </summary>
    public string ServiceName { get; private set; }
    /// <summary>
    /// Время запроса
    /// </summary>
    public string RequestTime { get; private set; }
    /// <summary>
    /// Статус обработки
    /// </summary>
    public QBCHProcessingStatus Status { get; private set; }
    /// <summary>
    /// Реквизиты КБКИ
    /// </summary>
    public List<QBCHRequisite> Requisites { get; private set; }
    /// <summary>
    /// Ошибки процессинга
    /// </summary>
    public List<core.Error> ProcessingErrors { get; private set; }
    /// <summary>
    /// Ошибки пакетного запроса
    /// </summary>
    public List<PackageError> PackageValidationErrors { get; private set; }
    /// <summary>
    /// Клиентский запрос
    /// </summary>
    public ClentRequest ClentRequest { get; private set; }
    /// <summary>
    /// Вложение
    /// </summary>
    public Attachment Attachment { get; private set; }
    /// <summary>
    /// Ответ
    /// </summary>
    public QBCHResponse Response { get; private set; }
    /// <summary>
    /// Время валидации
    /// </summary>
    public string? ValidateTime { get; private set; }
    /// <summary>
    /// Время ответа
    /// </summary>
    public string? ResponseTime { get; private set; }
    /// <summary>
    /// Время затраченное на валидацию
    /// </summary>
    public Stopwatch TimeElapsedForValidation { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="requestTime"></param>
    /// <param name="clentRequest"></param>
    /// <param name="attachment"></param>
    /// <param name="requisites"></param>
    protected QBCHProcessingTransaction(Guid id, DateTime requestTime, ClentRequest clentRequest, Attachment attachment, List<QBCHRequisite> requisites) : base(id)
    {
        RequestTime = requestTime.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        ProcessingErrors = [];
        PackageValidationErrors = [];
        ServiceName = "dlrequest";
        ClentRequest = clentRequest;
        Attachment = attachment;
        Status = QBCHProcessingStatus.Started;
        Requisites = requisites;
        TimeElapsedForValidation = Stopwatch.StartNew();
        Response = QBCHResponse.Create();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="requestTime"></param>
    /// <param name="clentRequest"></param>
    /// <param name="attachment"></param>
    /// <param name="requisites"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction Create(DateTime requestTime, ClentRequest clentRequest, Attachment attachment, List<QBCHRequisite> requisites)
    {
        return new QBCHProcessingTransaction(Guid.NewGuid(), requestTime, clentRequest, attachment, requisites);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dateTime"></param>
    private void SetValidateTime(DateTime dateTime)
    {
        ValidateTime ??= dateTime.ToString("dd.MM.yyyy HH:mm:ss:ffff");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dateTime"></param>
    private void SetResponseTime(DateTime dateTime)
    {
        ResponseTime ??= dateTime.ToString("dd.MM.yyyy HH:mm:ss:ffff");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    public void RiseCriticalError(core.Error error)
    {
        ProcessingErrors.Add(error);
        Status = QBCHProcessingStatus.Failure;
        SetValidateTime(DateTime.Now);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="requestNumber"></param>
    /// <param name="error"></param>
    public void SetPacakgeValidationError(int requestNumber, core.Error error)
    {
        PackageValidationErrors.Add(new PackageError(requestNumber, error.Code, error.Message));
    }

    /// <summary>
    /// 
    /// </summary>
    public void ValidationComplete()
    {
        if (Status.Equals(QBCHProcessingStatus.Started))
        {
            Status = QBCHProcessingStatus.Valid;
            SetValidateTime(DateTime.Now);
        }
    }

    /// <summary>
    ///  Установить сосстоние в Accepted 
    /// </summary>
    public void Accepted()
    {
        if (!Status.Equals(QBCHProcessingStatus.Failure))
        {
            Status = QBCHProcessingStatus.Accepted;
        }
    }

    /// <summary>
    /// Завершение обработки и формирование/подписание ответов
    /// </summary>
    /// <param name="response">Ответ</param>
    /// <param name="singnedResponse">Подписанный ответ</param>
    /// <returns></returns>
    public QBCHProcessingTransaction Complete(byte[] response, byte[] singnedResponse)
    {
        switch (Status)
        {
            // Формирование ответа  => Байты улетают в Response.ResponseXml
            case QBCHProcessingStatus.Valid:
                Response.SetResult(response, singnedResponse);
                break;
            // Формирование тикетов обобщенное  => Байты улетают в Response.TicketXml
            case QBCHProcessingStatus.Accepted:
                ProcessingErrors.Add(new(12, "Тикет"));
                Response.SetTicket(response, singnedResponse);
                break;
            case QBCHProcessingStatus.Failure:
                Response.SetTicket(response, singnedResponse);
                break;
        }

        SetValidateTime(DateTime.Now);
        SetResponseTime(DateTime.Now);

        return this;
    }

    /// <summary>
    /// Получить десериализованный запрос в нужной модели.
    /// </summary>
    public TRequest? GetRequest<TRequest>() where TRequest : class
    {
        return ClentRequest.RequestPayload as TRequest;
    }
}


/// <summary>
/// Статус процессинга
/// </summary>
public enum QBCHProcessingStatus
{
    /// <summary>
    /// Запущен
    /// </summary>
    Started,
    /// <summary>
    /// Валидный
    /// </summary>
    Valid,
    /// <summary>
    /// Тикет
    /// </summary>
    Accepted,
    /// <summary>
    /// Успешный ответ
    /// </summary>
    Success,
    /// <summary>
    /// Ошибочный ответ
    /// </summary>
    Failure
}