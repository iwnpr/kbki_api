using Domain.QBCHModels.aggregate;
using MediatR;

namespace QBCH_api.QBCHProcessing.ResponseDataCollect.DlrequestProcessing;

/// <summary>
/// 
/// </summary>
/// <param name="Transaction"></param>
/// <param name="QBCHTimeout"></param>
/// <param name="TicketTimeout"></param>
/// <param name="ResponseTimeout"></param>
/// <param name="OurBureauPSRN"></param>
public sealed record SendRequestsToQBCH(QBCHProcessingTransaction Transaction,
                                        int QBCHTimeout,
                                        int TicketTimeout,
                                        int ResponseTimeout,
                                        string OurBureauPSRN
) : IRequest<QBCHProcessingTransaction>;



