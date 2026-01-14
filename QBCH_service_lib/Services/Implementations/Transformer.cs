using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v1_3.CommonTypes;
using QBCH_lib.upload_xml;
using QBCH_lib.UTF4;
using QBCHService_lib.Services.Interfaces;

namespace QBCHService_lib.Services.Implementations
{
    /// <inheritdoc/>>
    public class Transformer : ITransformer
    {
        /// <inheritdoc/>
        public List<QBCH_lib.upload_xml.Document> ConvertDlPutToUpload(QBCH_lib.qcb_xml.v1_3.qcb_put.ПредставлениеСведенийОПлатежах data, AbonentValidatationResult abonent)
        {
            List<QBCH_lib.upload_xml.Document> docs = [];

            Subject subject = new();

            foreach (var item in data.Договоры)
            {
                if (item.Item is QBCH_lib.qcb_xml.v1_3.qcb_put.ДоговорДобавить add)
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
                                                amp_calculation_date = TryConvertDate(add?.СреднемесячныйПлатеж?.ДатаРасчета),
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

                if (item.Item is QBCH_lib.qcb_xml.v1_3.qcb_put.ДоговорУдалить delete)
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

                QBCH_lib.upload_xml.Document doc = new()
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

        /// <summary>
        /// В цикле создаются пакеты выгрузки с данными ос ССП
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <returns>Пакеты выгрузки</returns>
        public List<QBCH_lib.UTF4.Document> ConvertDlPutToUpload(QBCHProcessingTransaction request)
        {
            var transaction = request.ClentRequest;
            var dlput = transaction.PutRequest;
            List<QBCH_lib.UTF4.Document> documents = [];

            // В цикле создаются пакеты выгрузки с данными ос ССП
            foreach (var договор in request.ClentRequest.PutRequest?.Договоры ?? [])
            {
                // Пакет выгрузки
                QBCH_lib.UTF4.Document document = new()
                {
                    dateDoc = DateTime.Now,
                    regNumberDoc = dlput!.ИдентификаторЗапроса,
                    Source = new()
                    {
                        Item = new FL_46_UL_36_OrgSource_Type()
                        {
                            regNum = transaction.RequestOGRN,
                            TaxNum_group_FL_46_UL_36_OrgSource =
                        [
                            new()
                            {
                                taxCode = 1,
                                taxNum = transaction.RequestINN
                            }
                        ]
                        }
                    }
                };

                // Добавление ССП событие 2.7
                if (договор.Item is QBCH_lib.qcb_xml.v2_0.qcb_put.ДоговорДобавить addContract)
                {
                    document.Data = new()
                    {
                        Subject_FL = new DocumentDataSubject_FL()
                        {
                            Title = GetSubjctTitleData(addContract.Субъект),
                            Events = new()
                            {
                                Items =
                                [
                                    new FL_Event_2_7_Type()
                                    {
                                        operationCode = FL_Event_2_7_TypeOperationCode.B,
                                        eventDate = DateTime.Now.ToString(),
                                        orderNum = 1,
                                        FL_40_AvgPayment= new()
                                        {
                                            bureauRegNum = dlput?.БКИ.ОГРН,
                                            calcDate = TryConvertDate(addContract.СреднемесячныйПлатеж.ДатаРасчета),
                                            currency = addContract.СреднемесячныйПлатеж.Валюта,
                                            sum = addContract.СреднемесячныйПлатеж.Value,
                                            uid = договор.УИД,
                                            ItemElementName = ItemChoiceType13.stopReceiveExist_0
                                        }
                                    }
                                ]
                            }
                        }

                    };

                }

                // Удаление договора - собыитие 3.3
                if (договор.Item is QBCH_lib.qcb_xml.v2_0.qcb_put.ДоговорУдалить delContract)
                {

                    document.Data = new()
                    {
                        Subject_FL = new DocumentDataSubject_FL()
                        {
                            Events = new()
                            {

                                Items =
                                        [
                                            new FL_Event_3_3_Type()
                                            {
                                                operationCode = FL_Event_3_3_TypeOperationCode.C2,
                                                eventDate = delContract.ДатаРасчета?.ToString(),
                                                orderNum = 1,
                                                changeReason = delContract.Причина,
                                                FL_17_DealUid_R = new()
                                                {
                                                    uid = договор.УИД
                                                }
                                            }
                                        ]
                            }
                        }

                    };
                }

                // Добавление готового пакета в массив пакетов
                documents.Add(document);
            }

            return documents;
        }

        private static string? TryConvertDate(string? date)
        {
            if (DateTime.TryParse(date, out var result))
                return result.ToString("dd.MM.yyyy");

            return null;
        }

        private static string? TryConvertDate(DateTime date)
        {
            return date.ToString("dd.MM.yyyy");
        }

        private static SubjectTitleDataFL GetSubjctTitleData(QBCH_lib.qcb_xml.v2_0.CommonTypes.ТипСубъектТитул Субъект)
        {
            return new()
            {
                FL_1_4_Group = new()
                {

                    FL_1_Name = new()
                    {
                        lastName = Субъект.ФИО.FirstOrDefault()?.Фамилия,
                        firstName = Субъект.ФИО.FirstOrDefault()?.Имя,
                        middleName = Субъект.ФИО.FirstOrDefault()?.Отчество
                    },

                    FL_4_Doc = [.. Субъект.ДокументЛичности.Select(x => new FL_1_4_Group_TypeFL_4_Doc()
                            {
                                docSeries = x.Серия,
                                docNum = x.Номер,
                                issueDate = TryConvertDate(x.ДатаВыдачи),
                                docCode = x.КодДУЛ,
                                countryCode = x.Гражданство,
                                foreignerCode = x.Гражданство != "643" ? "0" : "1"
                            })]
                },
                FL_2_5_Group =
                        [
                            new()
                            {
                                FL_2_PrevName = new()
                                {
                                    prevNameFlag_0 = "0"
                                },
                                FL_5_PrevDoc =
                                [
                                    new()
                                    {
                                        prevDocFact_0 = "0"
                                    }
                                ],
                            }
                        ],
                FL_3_Birth = new()
                {
                    birthDate = TryConvertDate(Субъект.ДатаРождения)
                }
            };
        }
    }
}
