using Asp.Versioning;
using MediatR;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.Command;

/// <summary>
/// Команда создания и валидации транзакции API 3.0.
/// </summary>
/// <param name="ApiVersion">Версия API.</param>
/// <param name="Request">HTTP-запрос.</param>
public sealed record CreateToValidateCommandV3(ApiVersion ApiVersion, HttpRequest Request) : IRequest<QBCHProcessingTransaction>;
