using MediatR;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.V3.StoreProcessingData.Event;

public record QBCHProcessingCompleteV3(QBCHProcessingTransaction Transaction) : INotification;
