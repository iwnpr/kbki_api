using Domain.QBCHModels.aggregate;
using MediatR;

namespace QBCH_api.QBCHProcessing.StoreProcessingData.Event;

/// <summary>
/// 
/// </summary>
/// <param name="Transaction"></param>
/// <param name="IsDlput"></param>
public sealed record QBCHProcessingComplete(QBCHProcessingTransaction Transaction, bool IsDlput = false) : INotification;

