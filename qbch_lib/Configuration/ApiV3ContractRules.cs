using System;

namespace QBCH_lib.Configuration;

public class ApiV3ContractRules(ApiV3ContractOptions options)
{
    public int MaxDlRequestBatchSize => options.MaxDlRequestBatchSize > 0
        ? options.MaxDlRequestBatchSize
        : ApiV3ContractDefaults.MaxDlRequestBatchSize;

    public int MaxDlPutEntities => options.MaxDlPutEntities > 0
        ? options.MaxDlPutEntities
        : ApiV3ContractDefaults.MaxDlPutEntities;

    public int MinAnswerPollingIntervalSeconds => options.MinAnswerPollingIntervalSeconds > 0
        ? options.MinAnswerPollingIntervalSeconds
        : ApiV3ContractDefaults.MinAnswerPollingIntervalSeconds;

    public int ResponseRetentionHours => options.ResponseRetentionHours > 0
        ? options.ResponseRetentionHours
        : ApiV3ContractDefaults.ResponseRetentionHours;

    public int ResponseRetentionMinutes => ResponseRetentionHours * 60;

    public int ImmediateResponseDeadlineSeconds => options.ImmediateResponseDeadlineSeconds > 0
        ? options.ImmediateResponseDeadlineSeconds
        : ApiV3ContractDefaults.ImmediateResponseDeadlineSeconds;

    public int ImmediateResponseDeadlineMs => ImmediateResponseDeadlineSeconds * 1000;

    public int HttpClientTimeoutSeconds => options.HttpClientTimeoutSeconds > 0
        ? options.HttpClientTimeoutSeconds
        : ApiV3ContractDefaults.HttpClientTimeoutSeconds;

    public bool IsDlRequestBatchSizeValid(int count) => count <= MaxDlRequestBatchSize;

    public bool IsDlPutEntitiesCountValid(int count) => count <= MaxDlPutEntities;

    public bool IsAnswerRetryAllowed(DateTimeOffset previousRequestUtc, DateTimeOffset currentRequestUtc) =>
        currentRequestUtc - previousRequestUtc >= TimeSpan.FromSeconds(MinAnswerPollingIntervalSeconds);
}