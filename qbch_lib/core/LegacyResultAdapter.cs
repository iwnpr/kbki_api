using System;
using System.Runtime.ExceptionServices;
using qbch_lib.domain.errors;

namespace QBCH_lib.core;

/// <summary>
/// Адаптирует Result к прежним вызовам, в которых сбои обрабатываются через catch.
/// Используется только вызывающими компонентами; XML-сервисы возвращают Result напрямую.
/// </summary>
public static class LegacyResultAdapter
{
    /// <summary>Возвращает значение, включая допустимый null, либо выбрасывает ошибку операции.</summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="result">Результат вызова сервиса.</param>
    /// <returns>Значение успешного результата.</returns>
    public static T GetValueOrThrow<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return result.Value;

        result.Error.ThrowIfOperationFailed();
        throw new InvalidOperationException(result.Error?.Message ?? "Операция завершилась без результата.");
    }

    /// <summary>Передает внутренний сбой прежнему обработчику исключений, сохраняя исходный стек.</summary>
    /// <param name="error">Ошибка; ошибки входных данных и отсутствие ошибки пропускаются.</param>
    public static void ThrowIfOperationFailed(this BaseError? error)
    {
        if (error is OperationError failure)
            ExceptionDispatchInfo.Capture(failure.Exception).Throw();
    }
}
