using Asp.Versioning;
using MediatR;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.Command;

/// <summary>
/// Команда создания и валидации транзакции
/// </summary>
/// <param name="ApiVersion">Версия API.</param>
/// <param name="Request">HTTP-запрос.</param>
public sealed record CreateToValidateCommand(ApiVersion ApiVersion, HttpRequest Request) : IRequest<QBCHProcessingTransaction>;
