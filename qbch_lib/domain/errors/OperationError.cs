using System;

namespace qbch_lib.domain.errors;

/// <summary>
/// Внутренний сбой операции, передаваемый через Result вместо исключения.
/// Отличается от ошибки входных данных и сохраняет причину для прежней обработки сбоя.
/// </summary>
public sealed class OperationError : AnswerErrorCode
{
    /// <summary>Создает результат сбоя с исходным сообщением исключения.</summary>
    /// <param name="exception">Причина сбоя; используется для диагностики, не выбрасывается повторно.</param>
    public OperationError(Exception exception) : base(99, exception.Message)
    {
        Exception = exception;
    }

    /// <summary>Исходное исключение для записи диагностики.</summary>
    public Exception Exception { get; }
}
