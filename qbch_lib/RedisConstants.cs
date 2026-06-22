namespace qbch_lib;

public static class RedisConstants
{
    /// <summary>
    /// Redis-scope (префикс ключей) для запросов /dlrequest версии 3.
    /// </summary>
    public const string DlRequestV3Scope = "dlrequest:v3";
    /// <summary>
    /// Redis-scope (префикс ключей) для запросов /dlput версии 3.
    /// </summary>
    public const string DlPutV3Scope = "dlput:v3";
    /// <summary>
    /// Redis-scope (префикс ключей) для запросов /dlanswer версии 3.
    /// </summary>
    public const string DlAnswerV3Scope = "dlanswer:v3";
    /// <summary>
    /// Redis-scope (префикс ключей) для запросов /dlputanswer версии 3.
    /// </summary>
    public const string DlPutAnswerV3Scope = "dlputanswer:v3";
}