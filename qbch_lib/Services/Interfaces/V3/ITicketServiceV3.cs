using QBCH.Lib.qcb_xml.v3_0;
using qbch_lib.domain.errors;
using System;

namespace QBCH_lib.Services.Interfaces.V3;

public interface ITicketServiceV3
{
    Результат CreateResultV3Error(AnswerErrorCode error);

    Результат CreateResultV3Success(string requestId, DateTime requestDate);

    Результат CreateResultV3Accepted(string requestId, string responseId, DateTime requestDate);

    Результат CreateResultV3Common();
}
