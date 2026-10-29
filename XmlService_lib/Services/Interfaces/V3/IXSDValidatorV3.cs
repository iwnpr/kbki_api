using QBCH_lib.core;

namespace XmlService_lib.Services.Interfaces.V3;

public interface IXSDValidatorV3
{
    Result<bool> Validate(byte[]? xml, string schemaName);
    Result<T> ValidateAndDeserialize<T>(byte[]? xml, string schemaName) where T : class;
}
