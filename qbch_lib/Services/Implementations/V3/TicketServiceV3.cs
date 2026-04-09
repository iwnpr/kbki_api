using Microsoft.Extensions.Configuration;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_lib.Services.Interfaces.V3;
using System;

namespace QBCH_lib.Services.Implementations.V3;

public class TicketServiceV3(IConfiguration config) : ITicketServiceV3
{
    private readonly IConfiguration _config = config;
    private readonly string _BureauPSRN = "Bureau:PSRN";
    private readonly string _versionNumber = "3.0";

    public Результат CreateResultV3Error(core.Error error)
    {
        var result = CreateResultV3Common();
        result.УстановитьОшибку(error.Code, error.Message);
        return result;
    }

    public Результат CreateResultV3Success(string requestId, DateTime requestDate)
    {
        var result = CreateResultV3Common();
        result.УстановитьУспех(requestId, requestDate);
        return result;
    }

    public Результат CreateResultV3Accepted(string requestId, string responseId, DateTime requestDate, long? readyTime = null)
    {
        var result = CreateResultV3Common();
        result.УстановитьИдентификаторОтвета(responseId, requestId, requestDate, readyTime);
        return result;
    }

    public Результат CreateResultV3Common()
    {
        return new Результат
        {
            Версия = _versionNumber,
            ОГРН = _config.GetValue<string>(_BureauPSRN),
        };
    }
}
