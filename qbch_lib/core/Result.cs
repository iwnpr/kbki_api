using System.Diagnostics.CodeAnalysis;

namespace QBCH_lib.core;

/// <summary>
/// 
/// </summary>
public class Result
{
    /// <summary>
    /// 
    /// </summary>
    public bool IsSuccess { get; }
    /// <summary>
    /// 
    /// </summary>
    public BaseError? Error { get; }

    private Result(BaseError? error = null)
    {
        if (error is null)
        {
            IsSuccess = true;
        }
        else
        {
            IsSuccess = false;
            Error = error;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Result Success() => new();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    public static Result Failure(BaseError error) => new(error);
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class Result<T>
{
    [MemberNotNullWhen(returnValue: true, nameof(Value))]
    public bool IsSuccess { get; }
    public T? Value { get; }
    public BaseError? Error { get; }

    private Result(bool isSuccess, T? value, BaseError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public T FromResult() => Value;
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(BaseError error) => new(false, default, error);
}
