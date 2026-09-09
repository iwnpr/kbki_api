using System.Diagnostics;

namespace QBCH_lib.Diagnostics;

/// <summary>
/// Контекст замеров обращений к БД. Живёт в рамках одного запроса
/// </summary>
public sealed class DbTimingContext
{
    private readonly object _sync = new();

    private double _elapsedMilliseconds;

    /// <summary>
    /// Количество обращений к БД, выполняющихся прямо сейчас.
    /// </summary>
    private int _activeOperations;

    /// <summary>
    /// Момент, когда началось текущее непрерывное обращение к БД (переход 0 -> 1 активных операций).
    /// </summary>
    private long _activeSinceTimestamp;

    /// <summary>
    /// Время выполнения запросов в БД, мс: фактическое время, в течение которого выполнялось хотя бы одно обращение.
    /// </summary>
    public double ElapsedMilliseconds
    {

        get
        {
            lock (_sync)
            {
                // Если обращения к БД ещё идут, добавляем незакрытый интервал.
                return _activeOperations == 0
                    ? _elapsedMilliseconds
                    : _elapsedMilliseconds + Stopwatch.GetElapsedTime(_activeSinceTimestamp).TotalMilliseconds;
            }
        }

    }

    /// <summary>
    /// Начинает замер обращения к БД.
    /// </summary>
    /// <returns>Секундомер обращения, который нужно передать в <see cref="StopOperation"/>.</returns>
    public Stopwatch StartOperation()
    {
        lock (_sync)
        {
            // Первое из одновременных обращений открывает интервал фактического времени.
            if (_activeOperations++ == 0)
            {
                _activeSinceTimestamp = Stopwatch.GetTimestamp();
            }
        }
        return Stopwatch.StartNew();
    }

    /// <summary>
    /// Завершает замер обращения к БД и учитывает его время.
    /// </summary>
    /// <param name="operation">Секундомер, полученный из <see cref="StartOperation"/>.</param>
    /// <returns>Время выполнения обращения к БД, мс.</returns>
    public double StopOperation(Stopwatch operation)
    {
        operation.Stop();

        lock (_sync)
        {
            // Последнее из одновременных обращений закрывает интервал фактического времени.
            if (--_activeOperations == 0)
            {
                _elapsedMilliseconds += Stopwatch.GetElapsedTime(_activeSinceTimestamp).TotalMilliseconds;
            }
        }

        return operation.Elapsed.TotalMilliseconds;
    }
}
