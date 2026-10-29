using QBCH_lib.core;

namespace XmlService_lib.Services.Interfaces.V3;

/// <summary>XSD-валидация и десериализация документов API 3.0.</summary>
public interface IXSDValidatorV3
{
    /// <summary>Проверяет соответствие XML выбранному набору XSD-схем.</summary>
    /// <param name="xml">Байты XML-документа.</param>
    /// <param name="schemaName">Имя набора схем API 3.0: dlrequest, dlput, qcb_result, qcb_answer или qcb_putanswer.</param>
    /// <returns>Успешный результат со значением true либо ошибка входных данных, загрузки схем или валидации.</returns>
    Result<bool> Validate(byte[]? xml, string schemaName);
    /// <summary>Проверяет XML по XSD и десериализует его при успешной проверке.</summary>
    /// <typeparam name="T">Тип объекта XML.</typeparam>
    /// <param name="xml">Байты XML-документа.</param>
    /// <param name="schemaName">Имя набора схем API 3.0: dlrequest, dlput, qcb_result, qcb_answer или qcb_putanswer.</param>
    /// <returns>Десериализованный объект либо ошибка валидации или десериализации.</returns>
    Result<T> ValidateAndDeserialize<T>(byte[]? xml, string schemaName) where T : class;
}
