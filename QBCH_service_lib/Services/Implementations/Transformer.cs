using QBCH_lib.CommonTypes.Api;
using QBCH_lib.qcb_xml.v1_3.CommonTypes;
using QBCH_lib.qcb_xml.v1_3.qcb_put;
using QBCH_lib.upload_xml;
using QBCHService_lib.Services.Interfaces;

namespace QBCHService_lib.Services.Implementations
{
    /// <inheritdoc/>>
    public class Transformer : ITransformer
    {
        /// <inheritdoc/>
        public List<Document> ConvertDlPutToUpload(ПредставлениеСведенийОПлатежах data, AbonentValidatationResult abonent)
        {
            List<Document> docs = [];

            Subject subject = new();

            foreach (var item in data.Договоры)
            {
                if (item.Item is ДоговорДобавить add)
                {
                    ТипФИО? fullName = add?.Субъект?.ФИО?.FirstOrDefault();

                    subject = new()
                    {
                        tBlocksGroups =
                                [
                                    new tBlocksGroup()
                                    {
                                        @event = new tBlocksGroupEvent()
                                        {
                                            event_date = DateTime.Now.ToString("dd.MM.yyyy"),
                                            Value = "2.7"
                                        },
                                        operation = new()
                                        {
                                            operation_code = "B"
                                        },
                                        add_individual = new()
                                        {
                                            i_full_name = new()
                                            {
                                                surname = fullName?.Фамилия?.ToUpper(),
                                                name = fullName?.Имя?.ToUpper(),
                                                patronymic = fullName?.Отчество?.ToUpper(),
                                            },
                                            i_old_full_name = add?.Субъект?.ФИО?.Count > 1
                                                ? add?.Субъект?.ФИО?.Skip(1).Select(person =>
                                                    new tOldFullName()
                                                    {
                                                        old_name = "1",
                                                        i_full_name = new()
                                                        {
                                                            surname = person.Фамилия?.ToUpper(),
                                                            name = person.Имя?.ToUpper(),
                                                            patronymic = person.Отчество?.ToUpper(),
                                                        }
                                                    }).ToList()
                                                :
                                                    [
                                                        new() { old_name = "0"}
                                                    ],
                                            i_birth_date_place = new()
                                            {
                                                country_code = add?.Субъект?.ДокументЛичности?.FirstOrDefault()?.Гражданство,
                                                birth_date = add?.Субъект?.ДатаРождения.ToString("dd.MM.yyyy"),
                                            },
                                            i_snils = add?.Субъект?.СНИЛС,
                                            i_reg_num = add?.Субъект?.ИНН is not null || add?.Субъект?.ИнНомер is not null ?
                                            new()
                                            {
                                                i_itn = new()
                                            {
                                                new()
                                                {
                                                    itn = add?.Субъект?.ИНН,
                                                    itn_code = add?.Субъект?.ИНН is not null ? "1" : "0"
                                                }
                                            }
                                            }: null,
                                            i_identity_doc = add?.Субъект?.ДокументЛичности?
                                                .Select(item => new tIdentityDoc()
                                                {
                                                    citizenship = new() { country_code = item.Гражданство },
                                                    identity_doc_data = new()
                                                    {
                                                        d_code = item.КодДУЛ,
                                                        d_series = item.Серия,
                                                        d_number = item.Номер,
                                                        d_issue_date = item.ДатаВыдачи.ToString("dd.MM.yyyy")
                                                    }

                                                })
                                                .ToList(),
                                            i_old_identity_doc = [new() { old_identity_doc = "0" }]
                                        },
                                        add_main = new()
                                        {
                                            deal_id = new()
                                            {
                                                uid = item.УИД
                                            },
                                            i_amp_qcb = new()
                                            {
                                                amp_sum = Math.Round(add?.СреднемесячныйПлатеж?.Value ?? 0).ToString(),
                                                amp_calculation_date = ConvertDate(add?.СреднемесячныйПлатеж?.ДатаРасчета),
                                                amp_currency = add?.СреднемесячныйПлатеж?.Валюта,
                                                uid = item.УИД,
                                                info_received_terminated = "0",
                                                qcb_reg_num = data.БКИ?.ОГРН
                                            }
                                        },
                                        add_private = new()
                                        {
                                            src_legal_entity = new()
                                            {
                                                src_code = "99",
                                                registered_in_rf = "1",
                                                credit_info_date = DateTime.Now.ToString("dd.MM.yyyy"),
                                                src_name = new()
                                                {
                                                    full_name = abonent.FullName,
                                                    short_name = abonent.ShortName
                                                },
                                                src_creation_date = "-",
                                                src_itn = abonent.Inn is null ? null: new()
                                                {
                                                    new()
                                                    {
                                                        itn = abonent.Inn,
                                                        itn_code = "1"
                                                    }
                                                },
                                                src_reg_num = new()
                                                {
                                                    le_psrn = new()
                                                    {
                                                        psrn = abonent.Ogrn,
                                                        psrn_code = "1"
                                                    }
                                                }
                                            }
                                        }
                                    }
                            ]

                    };
                }

                if (item.Item is ДоговорУдалить delete)
                {
                    subject = new()
                    {
                        tBlocksGroups =
                                [
                                    new tBlocksGroup()
                                    {
                                        id = "1",
                                        @event = new tBlocksGroupEvent()
                                        {
                                            event_date = DateTime.Now.ToString("dd.MM.yyyy"),
                                            Value = "3.3"
                                        },
                                        operation = new()
                                        {
                                            operation_code = "C.2",
                                            comment = delete?.Причина
                                        },
                                        add_main = new()
                                        {
                                            deal_id = new()
                                            {
                                                uid = item.УИД
                                            },
                                        },
                                        add_private = new()
                                        {
                                            src_legal_entity = new()
                                            {
                                                src_code = "99",
                                                registered_in_rf = "1",
                                                credit_info_date = DateTime.Now.ToString("dd.MM.yyyy"),
                                                src_name = new()
                                                {
                                                    full_name = abonent.FullName,
                                                    short_name = abonent.ShortName
                                                },
                                                src_creation_date = "-",
                                                src_itn = abonent.Inn is null ? null: new()
                                                {
                                                    new()
                                                    {
                                                        itn = abonent.Inn,
                                                        itn_code = "1"
                                                    }
                                                },
                                                src_reg_num = new()
                                                {
                                                    le_psrn = new()
                                                    {
                                                        psrn = abonent.Ogrn,
                                                        psrn_code = "1"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                ]
                    };
                }


                Document doc = new()
                {
                    outgoing_date_time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                    outgoing_reg_num = data.ИдентификаторЗапроса,
                    source = new()
                    {
                        psrn = abonent.Ogrn,
                        itn = abonent.Inn
                    },
                    number_of_blocks_group = "1",
                    number_of_subjects = "1",
                    subjects = [subject]
                };

                docs.Add(doc);
            }

            return docs;
        }

        private string? ConvertDate(string? date)
        {
            if (DateTime.TryParse(date, out var result))
                return result.ToString("dd.MM.yyyy");

            return null;
        }
    }
}
