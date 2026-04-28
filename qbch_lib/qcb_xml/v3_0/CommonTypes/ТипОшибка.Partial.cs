using System.Collections.Generic;

namespace QBCH.Lib.qcb_xml.v3_0;

public partial class ТипОшибка
{
    private static readonly HashSet<int> ДопустимыеКодыОшибок = CreateAllowedCodes();

    public static bool ЯвляетсяДопустимымКодом(int код) => ДопустимыеКодыОшибок.Contains(код);

    public static ТипОшибка Создать(int код, string описание)
    {
        var normalizedCode = ЯвляетсяДопустимымКодом(код) ? код : 99;

        return new ТипОшибка
        {
            Код = normalizedCode.ToString(),
            Value = описание ?? string.Empty,
        };
    }

    public static ТипОшибка Создать(string код, string описание)
    {
        return int.TryParse(код, out var codeNumber)
            ? Создать(codeNumber, описание)
            : Создать(99, описание);
    }

    private static HashSet<int> CreateAllowedCodes()
    {
        var result = new HashSet<int>();
        for (var code = 1; code <= 31; code++)
        {
            result.Add(code);
        }

        result.Add(99);
        return result;
    }
}
