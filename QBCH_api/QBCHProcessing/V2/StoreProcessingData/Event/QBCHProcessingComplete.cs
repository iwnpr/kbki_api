using MediatR;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.V2.StoreProcessingData.Event;

/// <summary>
/// 
/// </summary>
/// <param name="Transaction"></param>
public sealed record QBCHProcessingComplete(QBCHProcessingTransaction Transaction) : INotification;

