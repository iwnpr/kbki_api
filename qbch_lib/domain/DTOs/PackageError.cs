namespace QBCH_lib.domain.DTOs;
/// <summary>
/// 
/// </summary>
/// <param name="Id"></param>
/// <param name="error_code"></param>
/// <param name="error_message"></param>
public sealed record PackageError
(
    int Id,
    int error_code,
    string error_message
);
