using MediatR;
using qbch_lib.domain.aggregate.V3;

namespace QBCH_api.QBCHProcessing.V3.StoreProcessingData.Event;

public record QBCHProcessingCompleteV3(QBCHProcessingTransactionV3 Transaction) : INotification;
