using MediatR;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.Processing.Command;

/// <summary>
/// 
/// </summary>
/// <param name="Transaction"></param>
/// <param name="QBCHTimeout"></param>
/// <param name="TicketTimeout"></param>
/// <param name="ResponseTimeout"></param>
/// <param name="OurBureauPSRN"></param>
public sealed record QBCHProcessedStart(QBCHProcessingTransaction Transaction,
                                        int QBCHTimeout,
                                        int TicketTimeout,
                                        int ResponseTimeout,
                                        string OurBureauPSRN
) : IRequest<QBCHProcessingTransaction>;



