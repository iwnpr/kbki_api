namespace QBCH_api.Services;

/// <summary>
/// Общие вспомогательные методы для валидаторов API 3.0.
/// </summary>
internal static class ValidationHelperV3
{
    public static int ParseOrderNumberOrPosition(string? orderNumberRaw, int position)
    {
        return int.TryParse(orderNumberRaw, out var parsedOrderNumber) && parsedOrderNumber > 0
            ? parsedOrderNumber
            : position;
    }
}
