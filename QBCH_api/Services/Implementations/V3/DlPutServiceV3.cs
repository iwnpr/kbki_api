using QBCH_api.Services.Interfaces.V3;
using QBCH_lib.Configuration;
using QBCH_lib.Services.Interfaces.V3;
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

public class DlPutServiceV3(ApiV3ContractRules contractRules, ITicketServiceV3 ticketServiceV3) : IDlPutServiceV3
{
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly ITicketServiceV3 _ticketServiceV3 = ticketServiceV3;

    public DlPutServiceV3ProcessingResult Process(ПредставлениеСведенийV3 request, bool returnAcceptedTicket = false, string? responseId = null, long? readyTime = null)
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
            Результат = entities.Select(BuildResultBlock).ToArray()
        };

        return new DlPutServiceV3ProcessingResult(false, result, null);
    }

    private static РезультатПредставленияСведенийРезультатV3 BuildResultBlock(ПредставлениеСведенийСведенияV3 source)
    {
        return source.Item switch
        {
            ПредставлениеСведенийСведенияДоговорV3 deal => BuildDealResult(deal),
            ПредставлениеСведенийСведенияОбращениеV3 appeal => BuildAppealResult(appeal),
            _ => BuildUnknownBlockResult()
        };
    }

    private static РезультатПредставленияСведенийРезультатV3 BuildDealResult(ПредставлениеСведенийСведенияДоговорV3 source)
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
                    deal.УстановитьОшибку(20, "Договор с указанным УИД не найден.");
                    break;
                }
                if (add.СреднемесячныйПлатеж is not null)
                {
                    deal.ДатаРасчета = add.СреднемесячныйПлатеж.ДатаРасчета.Date;
                    deal.ДатаРасчетаSpecified = true;
                }
                else
                {
                    deal.УстановитьОшибку(21, "Сведения о величине среднемесячного платежа по договору и дате его расчета не найдены.");
                    break;
                }
                deal.УстановитьУспех();
                break;

            case ПредставлениеСведенийСведенияДоговорУдалитьV3 delete:
                deal.Операция = СправочникОперацииV3.Удалить;
                deal.УИД = delete.УИД;

                if (string.IsNullOrWhiteSpace(delete.УИД))
                {
                    deal.УстановитьОшибку(20, "Договор с указанным УИД не найден.");
                    break;
                }

                if (!delete.ДатаРасчетаSpecified)
                {
                    deal.УстановитьОшибку(21, "Сведения о величине среднемесячного платежа по договору и дате его расчета не найдены.");
                    break;
                }
                if (delete.ДатаРасчетаSpecified)
                {
                    deal.ДатаРасчета = delete.ДатаРасчета.Date;
                    deal.ДатаРасчетаSpecified = true;
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

    private static РезультатПредставленияСведенийРезультатV3 BuildAppealResult(ПредставлениеСведенийСведенияОбращениеV3 source)
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
                    appeal.УстановитьОшибку(30, "Обращение/обязательство с указанным УИД не найдено.");
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
                    appeal.УстановитьОшибку(30, "Обращение/обязательство с указанным УИД не найдено.");
                    break;
                }

                if (!delete.СтадияРассмотренияSpecified)
                {
                    appeal.УстановитьОшибку(31, "Сведения для предупреждения мошенничества по стадии не найдены.");
                    break;
                }

                if (delete.СтадияРассмотренияSpecified)
                {
                    appeal.СтадияРассмотрения = delete.СтадияРассмотрения;
                    appeal.СтадияРассмотренияSpecified = true;
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
}
