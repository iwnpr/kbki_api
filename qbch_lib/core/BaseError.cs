namespace QBCH_lib.core;

public abstract class BaseError
{
    public int Code { get; private set; }
    public string Message { get; private set; }

    protected BaseError(int code, string msq)
    {
        Code = code;
        Message = msq;
    }
};


