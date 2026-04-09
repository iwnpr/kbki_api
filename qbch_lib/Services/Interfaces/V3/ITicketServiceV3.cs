using QBCH.Lib.qcb_xml.v3_0;
using System;

namespace QBCH_lib.Services.Interfaces.V3;

public interface ITicketServiceV3
{
    Результат CreateResultV3Error(core.Error error);

    Результат CreateResultV3Success(string requestId, DateTime requestDate);

    Результат CreateResultV3Accepted(string requestId, string responseId, DateTime requestDate, long? readyTime = null);

    Результат CreateResultV3Common();
}
