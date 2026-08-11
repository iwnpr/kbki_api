using Asp.Versioning;
using MediatR;
using qbch_lib.domain.aggregate.V3;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.Command;

/// <summary>
/// Команда создания и валидации транзакции
/// </summary>
/// <param name="ApiVersion">Версия API.</param>
/// <param name="Request">HTTP-запрос.</param>
public sealed record CreateToValidateCommandV3(ApiVersion ApiVersion, HttpRequest Request) : IRequest<QBCHProcessingTransactionV3>;
