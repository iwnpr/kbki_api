using Asp.Versioning;
using Domain.QBCHModels.aggregate;
using MediatR;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.DlRequestValidationMediatr;

/// <summary>
/// 
/// </summary>
/// <param name="ApiVersion"></param>
/// <param name="Request"></param>
public sealed record DlRequestAggregateMediatrInput(ApiVersion ApiVersion, HttpRequest Request) : IRequest<QBCHProcessingTransaction>;