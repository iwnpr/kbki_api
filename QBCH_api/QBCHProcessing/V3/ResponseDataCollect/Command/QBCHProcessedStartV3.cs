using MediatR;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.V3.ResponseDataCollect.Command;

/// <summary>
/// Команда запуска сбора данных КБКИ для API 3.0.
/// </summary>
/// <param name="Transaction">Транзакция обработки.</param>
/// <param name="ImmediateResponseDeadlineMs">Порог выдачи синхронного ответа в миллисекундах.</param>
/// <param name="OurBureauPSRN">ОГРН нашего БКИ.</param>
public sealed record QBCHProcessedStartV3(
    QBCHProcessingTransaction Transaction,
    int ImmediateResponseDeadlineMs,
    string OurBureauPSRN) : IRequest<QBCHProcessingTransaction>;