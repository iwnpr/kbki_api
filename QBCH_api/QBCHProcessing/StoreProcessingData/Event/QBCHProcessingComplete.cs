using MediatR;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.StoreProcessingData.Commands;

/// <summary>
/// 
/// </summary>
/// <param name="Transaction"></param>
public sealed record QBCHProcessingComplete(QBCHProcessingTransaction Transaction) : INotification;

