using QBCH_api.Services.Interfaces.V3;
using QBCH_lib.Configuration;
using QBCH_lib.core;
using QBCH_lib.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using XmlService_lib.Services.Interfaces.V3;
using ПредставлениеСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведений;
using ПредставлениеСведенийСведенияV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведения;
using ПредставлениеСведенийСведенияДоговорV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведенияДоговор;
using ПредставлениеСведенийСведенияДоговорУдалитьV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведенияДоговорУдалить;
using ПредставлениеСведенийСведенияОбращениеV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведенияОбращениеОбязательство;
using ПредставлениеСведенийСведенияОбращениеУдалитьV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведенияОбращениеОбязательствоУдалить;
using РезультатПредставленияСведенийБКИV3 = QBCH.Lib.qcb_xml.v3_0.РезультатПредставленияСведенийБКИ;
using РезультатПредставленияСведенийРезультатV3 = QBCH.Lib.qcb_xml.v3_0.РезультатПредставленияСведенийРезультат;
using РезультатПредставленияСведенийРезультатДоговорV3 = QBCH.Lib.qcb_xml.v3_0.РезультатПредставленияСведенийРезультатДоговор;
using РезультатПредставленияСведенийРезультатОбращениеV3 = QBCH.Lib.qcb_xml.v3_0.РезультатПредставленияСведенийРезультатОбращениеОбязательство;
using РезультатПредставленияСведенийV3 = QBCH.Lib.qcb_xml.v3_0.РезультатПредставленияСведений;
using СправочникОперацииV3 = QBCH.Lib.qcb_xml.v3_0.СправочникОперации;
using ТипДоговорV3 = QBCH.Lib.qcb_xml.v3_0.ТипДоговор;
using ТипОбращениеV3 = QBCH.Lib.qcb_xml.v3_0.ТипОбращениеОбязательство;

namespace QBCH_api.Services.Implementations.V3;

public class DlPutServiceV3(
    ApiV3ContractRules contractRules,
    ITicketServiceV3 ticketServiceV3,
    IRepositoryV3 repository,
    IXmlServiceV3 xmlService) : IDlPutServiceV3
{
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly ITicketServiceV3 _ticketServiceV3 = ticketServiceV3;
    private readonly IRepositoryV3 _repository = repository;
    private readonly IXmlServiceV3 _xmlService = xmlService;

    public async Task<DlPutServiceV3ProcessingResult> ProcessAsync(ПредставлениеСведенийV3 request, bool returnAcceptedTicket = false, string? responseId = null, long? readyTime = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entities = request.Сведения ?? [];
        if (!_contractRules.IsDlPutEntitiesCountValid(entities.Length))
        {
            throw new InvalidOperationException($"Количество элементов Сведения превышает допустимый лимит {_contractRules.MaxDlPutEntities}.");
        }

        if (returnAcceptedTicket)
        {
            var acceptedId = !string.IsNullOrWhiteSpace(responseId) ? responseId : Guid.NewGuid().ToString();
            var accepted = _ticketServiceV3.CreateResultV3Accepted(request.ИдентификаторЗапроса, acceptedId, request.ДатаЗапроса, readyTime);
            return new DlPutServiceV3ProcessingResult(true, null, accepted);
        }

        var resultBlocks = new РезультатПредставленияСведенийРезультатV3[entities.Length];
        for (var i = 0; i < entities.Length; i++)
        {
            resultBlocks[i] = await BuildResultBlock(entities[i]);
        }

        var result = new РезультатПредставленияСведенийV3
        {
            Версия = request.Версия,
            ИдентификаторЗапроса = request.ИдентификаторЗапроса,
            ДатаЗапроса = request.ДатаЗапроса.Date,
            ИдентификаторОтвета = Guid.NewGuid().ToString(),
            ОГРН = request.БКИ?.ОГРН,
            БКИ = request.БКИ is null
                ? null
                : new РезультатПредставленияСведенийБКИV3
                {
                    ОГРН = request.БКИ.ОГРН,
                    Value = request.БКИ.Value,
                },
            Результат = resultBlocks
        };

        return new DlPutServiceV3ProcessingResult(false, result, null);
    }

    private async Task<РезультатПредставленияСведенийРезультатV3> BuildResultBlock(ПредставлениеСведенийСведенияV3 source)
    {
        return source.Item switch
        {
            ПредставлениеСведенийСведенияДоговорV3 deal => await BuildDealResult(deal),
            ПредставлениеСведенийСведенияОбращениеV3 appeal => await BuildAppealResult(appeal),
            _ => BuildUnknownBlockResult()
        };
    }

    private async Task<РезультатПредставленияСведенийРезультатV3> BuildDealResult(ПредставлениеСведенийСведенияДоговорV3 source)
    {
        var result = new РезультатПредставленияСведенийРезультатV3();
        var deal = new РезультатПредставленияСведенийРезультатДоговорV3();

        switch (source.Item)
        {
            case ТипДоговорV3 add:
                deal.Операция = СправочникОперацииV3.Добавить;
                deal.УИД = add.УИД;

                if (string.IsNullOrWhiteSpace(add.УИД))
                {
                    SetError(deal, Error.Code15_InvalidRequestData("Атрибут \"УИД\" не может быть пустым"));
                    break;
                }

                if (add.СреднемесячныйПлатеж is null)
                {
                    SetError(deal, Error.Code15_InvalidRequestData("Блок \"СреднемесячныйПлатеж\" обязателен для операции \"Добавить\""));
                    break;
                }

                deal.ДатаРасчета = add.СреднемесячныйПлатеж.ДатаРасчета.Date;
                deal.ДатаРасчетаSpecified = true;
                deal.УстановитьУспех();
                break;

            case ПредставлениеСведенийСведенияДоговорУдалитьV3 delete:
                deal.Операция = СправочникОперацииV3.Удалить;
                deal.УИД = delete.УИД;

                if (string.IsNullOrWhiteSpace(delete.УИД))
                {
                    SetError(deal, Error.Code15_InvalidRequestData("Атрибут \"УИД\" не может быть пустым"));
                    break;
                }

                if (!delete.ДатаРасчетаSpecified)
                {
                    SetError(deal, Error.Code15_InvalidRequestData("Атрибут \"ДатаРасчета\" обязателен для операции \"Удалить\""));
                    break;
                }

                deal.ДатаРасчета = delete.ДатаРасчета.Date;
                deal.ДатаРасчетаSpecified = true;

                var contractSubjectXml = _xmlService.SerializeAsStringV3(source.Субъект);
                var contractSubjectIds = await _repository.SearchContractSubjectsForDlPutV3(contractSubjectXml);
                if (contractSubjectIds is not null && contractSubjectIds.Count == 0)
                {
                    SetError(deal, Error.Code29_SubjectNotFound());
                    break;
                }

                if (contractSubjectIds is { Count: > 0 })
                {
                    var contractUidExists = await _repository.ContractUidExistsForSubjectsV3(contractSubjectIds, delete.УИД);
                    if (contractUidExists is false)
                    {
                        SetError(deal, Error.Code20_ContractNotFound());
                        break;
                    }

                    if (contractUidExists is true)
                    {
                        var calcDateExists = await _repository.ContractCalculationDateExistsForSubjectsV3(contractSubjectIds, delete.УИД, delete.ДатаРасчета);
                        if (calcDateExists is false)
                        {
                            SetError(deal, Error.Code21_CalculationDateNotFound());
                            break;
                        }
                    }
                }

                deal.УстановитьУспех();
                break;

            default:
                deal.Операция = СправочникОперацииV3.Добавить;
                deal.УстановитьОшибку(99, "Неизвестный тип операции для блока Договор.");
                break;
        }

        result.УстановитьРезультатДоговора(deal);
        return result;
    }

    private async Task<РезультатПредставленияСведенийРезультатV3> BuildAppealResult(ПредставлениеСведенийСведенияОбращениеV3 source)
    {
        var result = new РезультатПредставленияСведенийРезультатV3();
        var appeal = new РезультатПредставленияСведенийРезультатОбращениеV3();

        switch (source.Item)
        {
            case ТипОбращениеV3 add:
                appeal.Операция = СправочникОперацииV3.Добавить;
                appeal.УИД = add.УИД;

                if (string.IsNullOrWhiteSpace(add.УИД))
                {
                    SetError(appeal, Error.Code15_InvalidRequestData("Атрибут \"УИД\" не может быть пустым"));
                    break;
                }

                appeal.СтадияРассмотрения = add.СтадияРассмотрения;
                appeal.СтадияРассмотренияSpecified = true;
                appeal.УстановитьУспех();
                break;

            case ПредставлениеСведенийСведенияОбращениеУдалитьV3 delete:
                appeal.Операция = СправочникОперацииV3.Удалить;
                appeal.УИД = delete.УИД;

                if (string.IsNullOrWhiteSpace(delete.УИД))
                {
                    SetError(appeal, Error.Code15_InvalidRequestData("Атрибут \"УИД\" не может быть пустым"));
                    break;
                }

                if (!delete.СтадияРассмотренияSpecified)
                {
                    SetError(appeal, Error.Code15_InvalidRequestData("Атрибут \"СтадияРассмотрения\" обязателен для операции \"Удалить\""));
                    break;
                }

                appeal.СтадияРассмотрения = delete.СтадияРассмотрения;
                appeal.СтадияРассмотренияSpecified = true;

                var inn = source.Субъект?.ИНН?.Value;
                var appealSubjectIds = string.IsNullOrWhiteSpace(inn)
                    ? null
                    : await _repository.SearchAppealSubjectsByInnForDlPutV3(inn);

                if (appealSubjectIds is not null && appealSubjectIds.Count == 0)
                {
                    SetError(appeal, Error.Code29_SubjectNotFound());
                    break;
                }

                if (appealSubjectIds is { Count: > 0 })
                {
                    var appealUidExists = await _repository.AppealUidExistsForSubjectsV3(appealSubjectIds, delete.УИД);
                    if (appealUidExists is false)
                    {
                        SetError(appeal, Error.Code30_AppealObligationNotFound());
                        break;
                    }

                    if (appealUidExists is true)
                    {
                        var stageExists = await _repository.AppealStageExistsForSubjectsV3(appealSubjectIds, delete.УИД, delete.СтадияРассмотрения);
                        if (stageExists is false)
                        {
                            SetError(appeal, Error.Code31_AntiFraudDataNotFound());
                            break;
                        }
                    }
                }

                appeal.УстановитьУспех();
                break;

            default:
                appeal.Операция = СправочникОперацииV3.Добавить;
                appeal.УстановитьОшибку(99, "Неизвестный тип операции для блока ОбращениеОбязательство.");
                break;
        }

        result.УстановитьРезультатОбращения(appeal);
        return result;
    }

    private static РезультатПредставленияСведенийРезультатV3 BuildUnknownBlockResult()
    {
        var result = new РезультатПредставленияСведенийРезультатV3();
        var deal = new РезультатПредставленияСведенийРезультатДоговорV3
        {
            Операция = СправочникОперацииV3.Добавить,
            УИД = string.Empty,
        };
        deal.УстановитьОшибку(99, "Неизвестный тип элемента Сведения.");
        result.УстановитьРезультатДоговора(deal);
        return result;
    }

    private static void SetError(РезультатПредставленияСведенийРезультатДоговорV3 target, Error error)
        => target.УстановитьОшибку(error.Code, error.Message);

    private static void SetError(РезультатПредставленияСведенийРезультатОбращениеV3 target, Error error)
        => target.УстановитьОшибку(error.Code, error.Message);
}
