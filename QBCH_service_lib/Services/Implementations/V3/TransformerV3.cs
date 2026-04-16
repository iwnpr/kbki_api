using QBCH_lib.CommonTypes.Api;
using QBCH_lib.upload_xml;
using QBCHService_lib.Services.Interfaces.V3;
using ТипДУЛV3 = QBCH.Lib.qcb_xml.v3_0.ТипДУЛ;
using ТипИННФЛV3 = QBCH.Lib.qcb_xml.v3_0.ТипИННФЛсПризнаком;
using ТипОбращениеV3 = QBCH.Lib.qcb_xml.v3_0.ТипОбращениеОбязательство;
using ТипСубъектТитулАФV3 = QBCH.Lib.qcb_xml.v3_0.ТипСубъектТитулАФ;
using ТипСубъектТитулV3 = QBCH.Lib.qcb_xml.v3_0.ТипСубъектТитул;
using ПредставлениеСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведений;
using ПредставлениеСведенийСведенияV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведения;
using ПредставлениеСведенийСведенияДоговорV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведенияДоговор;
using ПредставлениеСведенийСведенияДоговорУдалитьV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведенияДоговорУдалить;
using ПредставлениеСведенийСведенияОбращениеV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведенияОбращениеОбязательство;
using ПредставлениеСведенийСведенияОбращениеУдалитьV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведенийСведенияОбращениеОбязательствоУдалить;
using ТипДоговорV3 = QBCH.Lib.qcb_xml.v3_0.ТипДоговор;

namespace QBCHService_lib.Services.Implementations.V3;

public class TransformerV3 : ITransformerV3
{
    public List<Document> ConvertDlPutToUpload(ПредставлениеСведенийV3 data, AbonentValidatationResult abonent)
    {
        List<Document> docs = [];

        foreach (ПредставлениеСведенийСведенияV3 item in data.Сведения ?? [])
        {
            Subject? subject = item.Item switch
            {
                ПредставлениеСведенийСведенияДоговорV3 договор => MapDealSubject(договор, data, abonent),
                ПредставлениеСведенийСведенияОбращениеV3 обращение => MapAppealSubject(обращение, data, abonent),
                _ => null
            };

            if (subject is null)
            {
                continue;
            }

            docs.Add(new Document
            {
                outgoing_date_time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                outgoing_reg_num = data.ИдентификаторЗапроса,
                source = new tSource
                {
                    psrn = abonent.Ogrn,
                    itn = abonent.Inn
                },
                number_of_blocks_group = "1",
                number_of_subjects = "1",
                subjects = [subject]
            });
        }

        return docs;
    }

    private static Subject? MapDealSubject(ПредставлениеСведенийСведенияДоговорV3 договорContainer, ПредставлениеСведенийV3 data, AbonentValidatationResult abonent)
    {
        var block = договорContainer.Item switch
        {
            ТипДоговорV3 add => BuildDealAddBlock(договорContainer.Субъект, add, data, abonent),
            ПредставлениеСведенийСведенияДоговорУдалитьV3 delete => BuildDealDeleteBlock(delete, abonent),
            _ => null
        };

        return block is null ? null : new Subject { tBlocksGroups = [block] };
    }

    private static Subject? MapAppealSubject(ПредставлениеСведенийСведенияОбращениеV3 appealContainer, ПредставлениеСведенийV3 data, AbonentValidatationResult abonent)
    {
        var block = appealContainer.Item switch
        {
            ТипОбращениеV3 add => BuildAppealAddBlock(appealContainer.Субъект, add, abonent),
            ПредставлениеСведенийСведенияОбращениеУдалитьV3 delete => BuildAppealDeleteBlock(appealContainer.Субъект, delete, abonent),
            _ => null
        };

        return block is null ? null : new Subject { tBlocksGroups = [block] };
    }

    private static tBlocksGroup BuildDealAddBlock(ТипСубъектТитулV3? subject, ТипДоговорV3 add, ПредставлениеСведенийV3 data, AbonentValidatationResult abonent)
    {
        return new tBlocksGroup
        {
            @event = new tBlocksGroupEvent
            {
                event_date = DateTime.Now.ToString("dd.MM.yyyy"),
                Value = "2.7"
            },
            operation = new tOperation { operation_code = "B" },
            add_individual = BuildIndividual(subject?.ФИО, subject?.ДатаРождения, subject?.ДокументЛичности, subject?.ИНН, subject?.ИнНомер, subject?.СНИЛС),
            add_main = new tAddMain
            {
                deal_id = new tDealId { uid = add.УИД },
                i_amp_qcb = new tAMPQCB
                {
                    amp_sum = add.СреднемесячныйПлатеж?.Value,
                    amp_calculation_date = add.СреднемесячныйПлатеж?.ДатаРасчета.ToString("dd.MM.yyyy"),
                    amp_currency = add.СреднемесячныйПлатеж?.Валюта,
                    uid = add.УИД,
                    info_received_terminated = "0",
                    qcb_reg_num = data.БКИ?.ОГРН
                },
                credit_deal = new tCreditDeal
                {
                    floating_interest_rate = add.ПСК
                },
                obligation_termination = add.ДатаПрекращенияSpecified
                    ? new tObligationTerm { ot_date = add.ДатаПрекращения.ToString("dd.MM.yyyy") }
                    : null
            },
            add_private = BuildPrivate(abonent)
        };
    }

    private static tBlocksGroup BuildDealDeleteBlock(ПредставлениеСведенийСведенияДоговорУдалитьV3 delete, AbonentValidatationResult abonent)
    {
        return new tBlocksGroup
        {
            id = "1",
            @event = new tBlocksGroupEvent
            {
                event_date = DateTime.Now.ToString("dd.MM.yyyy"),
                Value = "3.3"
            },
            operation = new tOperation
            {
                operation_code = "C.2",
                comment = delete.Причина
            },
            add_main = new tAddMain
            {
                deal_id = new tDealId { uid = delete.УИД }
            },
            add_private = BuildPrivate(abonent)
        };
    }

    private static tBlocksGroup BuildAppealAddBlock(ТипСубъектТитулАФV3? subject, ТипОбращениеV3 add, AbonentValidatationResult abonent)
    {
        return new tBlocksGroup
        {
            @event = new tBlocksGroupEvent
            {
                event_date = DateTime.Now.ToString("dd.MM.yyyy"),
                Value = "2.7"
            },
            operation = new tOperation { operation_code = "B" },
            add_individual = BuildIndividual(subject?.ФИО, subject?.ДатаРождения, subject?.ДокументЛичности, subject?.ИНН, subject?.ИнНомер, subject?.СНИЛС),
            add_info = new tAddInfo
            {
                credit_application = new tCreditApplication
                {
                    ca_uid = add.УИД,
                    src_code = ((int)add.КодИсточника).ToString(),
                    ca_consideration_status = add.СтадияРассмотрения.ToString(),
                    ca_consideration_status_date = add.ДатаСтадии.ToString("dd.MM.yyyy"),
                    ca_sum = add.СуммаЗайма?.Value.ToString(),
                    ca_currency = add.СуммаЗайма?.Валюта
                },
                credit_refusal = add.ПричинаОтказа?.Length > 0
                    ? new tCreditRefusal { refuse_reason_code = add.ПричинаОтказа.Select(x => x.ToString()).ToList() }
                    : null
            },
            add_private = BuildPrivate(abonent)
        };
    }

    private static tBlocksGroup BuildAppealDeleteBlock(ТипСубъектТитулАФV3? subject, ПредставлениеСведенийСведенияОбращениеУдалитьV3 delete, AbonentValidatationResult abonent)
    {
        return new tBlocksGroup
        {
            id = "1",
            @event = new tBlocksGroupEvent
            {
                event_date = DateTime.Now.ToString("dd.MM.yyyy"),
                Value = "3.3"
            },
            operation = new tOperation
            {
                operation_code = "C.2",
                comment = delete.Причина
            },
            add_individual = BuildIndividual(subject?.ФИО, subject?.ДатаРождения, subject?.ДокументЛичности, subject?.ИНН, subject?.ИнНомер, subject?.СНИЛС),
            add_info = new tAddInfo
            {
                credit_application = new tCreditApplication
                {
                    ca_uid = delete.УИД,
                    ca_consideration_status = delete.СтадияРассмотренияSpecified
                        ? delete.СтадияРассмотрения.ToString()
                        : null
                }
            },
            add_private = BuildPrivate(abonent)
        };
    }

    private static tAddIndividual BuildIndividual(
        QBCH.Lib.qcb_xml.v3_0.ТипФИО[]? names,
        DateTime? birthDate,
        ТипДУЛV3[]? identityDocs,
        ТипИННФЛV3? inn,
        string? innNumber,
        string? snils)
    {
        var fullName = names?.FirstOrDefault();

        return new tAddIndividual
        {
            i_full_name = new tFullName
            {
                surname = fullName?.Фамилия?.ToUpper(),
                name = fullName?.Имя?.ToUpper(),
                patronymic = fullName?.Отчество?.ToUpper()
            },
            i_old_full_name = names is { Length: > 1 }
                ? names.Skip(1).Select(person => new tOldFullName
                {
                    old_name = "1",
                    i_full_name = new tFullName
                    {
                        surname = person.Фамилия?.ToUpper(),
                        name = person.Имя?.ToUpper(),
                        patronymic = person.Отчество?.ToUpper()
                    }
                }).ToList()
                : [new tOldFullName { old_name = "0" }],
            i_birth_date_place = new tibDatePlace
            {
                country_code = identityDocs?.FirstOrDefault()?.Гражданство,
                birth_date = birthDate?.ToString("dd.MM.yyyy")
            },
            i_snils = snils,
            i_reg_num = !string.IsNullOrWhiteSpace(inn?.Value) || !string.IsNullOrWhiteSpace(innNumber)
                ? new tRegNum
                {
                    i_itn =
                    [
                        new tInfoITN
                        {
                            itn = inn?.Value ?? innNumber,
                            itn_code = inn is not null ? ((int)inn.ПризнакПроверки).ToString() : "0"
                        }
                    ]
                }
                : null,
            i_identity_doc = identityDocs?.Select(doc => new tIdentityDoc
            {
                citizenship = new tSitizenship { country_code = doc.Гражданство },
                identity_doc_data = new tiIdentityDocData
                {
                    d_series = doc.Серия,
                    d_number = doc.Номер,
                    d_issue_date = doc.ДатаВыдачи.ToString("dd.MM.yyyy")
                }
            }).ToList(),
            i_old_identity_doc = [new tOldIdentityDoc { old_identity_doc = "0" }]
        };
    }


    private static tAddPrivate BuildPrivate(AbonentValidatationResult abonent)
    {
        return new tAddPrivate
        {
            src_legal_entity = new tSRCLegalEntity
            {
                src_code = "99",
                registered_in_rf = "1",
                credit_info_date = DateTime.Now.ToString("dd.MM.yyyy"),
                src_name = new tName
                {
                    full_name = abonent.FullName,
                    short_name = abonent.ShortName
                },
                src_creation_date = "-",
                src_itn = string.IsNullOrWhiteSpace(abonent.Inn)
                    ? null
                    : [new tInfoITN { itn = abonent.Inn, itn_code = "1" }],
                src_reg_num = new tleRegNum
                {
                    le_psrn = new tInfoSRN
                    {
                        psrn = abonent.Ogrn,
                        psrn_code = "1"
                    }
                }
            }
        };
    }
}
