using Asp.Versioning;
using MediatR;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.V2.CreateAndValidation.Command;

/// <summary>
/// 
/// </summary>
/// <param name="ApiVersion"></param>
/// <param name="Request"></param>
public sealed record CreateToValidateCommand(ApiVersion ApiVersion, HttpRequest Request) : IRequest<QBCHProcessingTransaction>;


