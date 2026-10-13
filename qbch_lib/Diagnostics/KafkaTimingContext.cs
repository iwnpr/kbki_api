using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QBCH_lib.Diagnostics;

/// <summary>
/// Замеры отправок Kafka в рамках одного запроса, без ожидания в очереди.
/// </summary>
public sealed class KafkaTimingContext
{
    private readonly object _sync = new();
    private int _pendingOperations;
    private double _elapsedMilliseconds;
    private Action<double>? _onCompleted;

    /// <summary>
    /// Регистрирует отправку до постановки в очередь.
    /// </summary>
    public Operation RegisterOperation()
    {
        lock (_sync)
        {
            _pendingOperations++;
        }
        return new Operation(this);
    }

    /// <summary>
    /// После регистрации всех отправок задаёт действие, выполняемое по их завершении.
    /// Если отправки уже завершены или не выполнялись, действие вызывается сразу.
    /// </summary>
    public void OnCompleted(Action<double> callback)
    {
        double elapsed;
        lock (_sync)
        {
            if (_pendingOperations > 0)
            {
                _onCompleted = callback;
                return;
            }
            elapsed = _elapsedMilliseconds;
        }
        callback(elapsed);
    }

    private void CompleteOperation(double elapsedMilliseconds)
    {
        Action<double>? callback;
        double elapsed;
        lock (_sync)
        {
            _elapsedMilliseconds += elapsedMilliseconds;
            if (--_pendingOperations > 0)
                return;

            elapsed = _elapsedMilliseconds;
            callback = _onCompleted;
            _onCompleted = null;
        }
        callback?.Invoke(elapsed);
    }

    /// <summary>
    /// Замер одной отправки, включая повторы, ошибки и таймауты Kafka.
    /// </summary>
    public sealed class Operation
    {
        private readonly KafkaTimingContext _context;
        private int _completed;

        internal Operation(KafkaTimingContext context) => _context = context;

        /// <summary>
        /// Измеряет вызов отправки и учитывает его время даже при исключении.
        /// </summary>
        public async Task<bool> MeasureAsync(Func<Task<bool>> publish)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return await publish();
            }
            finally
            {
                stopwatch.Stop();
                Complete(stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Завершает замер нулевым временем, если отправка не выполнялась.
        /// </summary>
        public void Skip() => Complete(0);

        private void Complete(double elapsedMilliseconds)
        {
            if (Interlocked.Exchange(ref _completed, 1) == 0)
                _context.CompleteOperation(elapsedMilliseconds);
        }
    }
}