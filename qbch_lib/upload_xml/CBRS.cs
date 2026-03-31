using QBCH_lib.qcb_xml.v1_3.Enums;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace QBCH_lib.upload_xml
{
    [Serializable]
    public class tOtherComment
    {
        /// <summary>
        /// Тип записи
        /// </summary>
        public int? record_type { get; set; }

        /// <summary>
        /// Данные записи
        /// </summary>
        public string? record_id { get; set; }

        /// <summary>
        /// id заявки
        /// </summary>
        public string? application_id { get; set; }

        /// <summary>
        /// Комментарий в свободной форме
        /// </summary>
        public string? record_comment { get; set; }
    }


    /// <summary>
    /// Выгрузка в формате КБРС
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true)]
    [XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Document
    {

        /// <summary>
        /// Сведения об источнике формирования кредитной истории
        /// </summary>
        public tSource? source { get; set; }

        /// <summary>
        /// Количество субъектов, сведения о которых указаны в документе выгрузки
        /// </summary>
        public string? number_of_subjects { get; set; }

        /// <summary>
        /// Количество групп блоков показателей кредитной информации, содержащихся в документе выгрузки
        /// </summary>
        public string? number_of_blocks_group { get; set; }

        /// <summary>
        /// Сведения о субъекте кредитной истории
        /// </summary>
        [XmlElement("subject", IsNullable = false)]
        public List<Subject> subjects { get; set; } = new();

        /// <summary>
        /// Исходящая дата документа выгрузки
        /// </summary>
        [XmlAttributeAttribute()]
        public string? outgoing_date_time { get; set; }

        /// <summary>
        /// Версия схемы
        /// </summary>
        [XmlAttributeAttribute()]
        public string? schema_version { get; set; } = "05.07";

        /// <summary>
        /// Исходящий регистрационный номер документа выгрузки
        /// </summary>
        [XmlAttributeAttribute()]
        public string? outgoing_reg_num { get; set; }

        /// <summary>
        /// Исходящий регистрационный номер документа выгрузки, содержащего ранее не принятую кредитную информацию
        /// </summary>
        [XmlAttributeAttribute()]
        public string? prev_outgoing_reg_num { get; set; }

        /// <summary>
        /// Исходящая дата документа выгрузки, содержащего ранее не принятую кредитную информацию
        /// </summary>
        [XmlAttributeAttribute()]
        public string? prev_outgoing_date_time { get; set; }
    }

    /// <summary>
    /// Сведения об источнике формирования кредитной истории
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tSource
    {

        /// <summary>
        /// ИНН
        /// </summary>
        public string? itn { get; set; }

        /// <summary>
        /// ОГРН
        /// </summary>
        public string? psrn { get; set; }
    }

    /// <summary>
    /// Субъект
    /// </summary>
    public partial class Subject
    {
        /// <summary>
        /// Список групп блоков внутри тега субъект
        /// </summary>
        [XmlElement("blocks_group", IsNullable = false)]
        public List<tBlocksGroup> tBlocksGroups { get; set; } = [];
    }

    /// <summary>
    /// Сведения об отказе источника от предложения совершить сделку (ФЛ_57 = ЮЛ_47)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditRefusal
    {

        /// <summary>
        /// Дата отказа источника субъекту
        /// </summary>
        public string? refuse_date { get; set; }

        /// <summary>
        /// Код причины отказа источника субъекту
        /// </summary>
        [XmlElementAttribute("refuse_reason_code")]
        public List<string>? refuse_reason_code { get; set; }
    }

    /// <summary>
    /// Сведения об участии в обязательстве, по которому формируется кредитная история (ФЛ_56 = ЮЛ_46)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditParticipation
    {

        /// <summary>
        /// Код вида участия в сделке
        /// </summary>
        public string? credit_participation_code { get; set; }

        /// <summary>
        /// Код вида займа (кредита)
        /// Если код типа сделки "1", то параметр обязательный
        /// </summary>
        public string? credit_type { get; set; }

        /// <summary>
        /// УИд сделки
        /// Если договор действует на 29.10.2019 и не прекратил действие до 29.10.2020, 
        /// или договор заключен после 29.10.2019, то параметр обязательный, 
        /// кроме требований о взыскании по алиментам, платы за жилое помещение, коммунальные услуги и услуги связи
        /// </summary>
        public string? credit_uid { get; set; }

        /// <summary>
        /// Дата передачи финансирования субъекту кредитной истории или возникновения обеспечения исполнения обязательства
        /// </summary>
        public string? funding_date { get; set; }

        /// <summary>
        /// Признак просрочки должника более 90 дней
        /// </summary>
        public string? overdue_more_90_days { get; set; }

        /// <summary>
        /// Признак прекращения обязательства
        /// </summary>
        public string? obligation_terminated { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tParticipationCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tCreditTypeCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("11")]
        Item11,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("12")]
        Item12,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("13")]
        Item13,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("14")]
        Item14,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("15")]
        Item15,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("16")]
        Item16,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("17")]
        Item17,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// Сведения об обращении (ФЛ_55 = ЮЛ_45)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditApplication
    {

        /// <summary>
        /// Код вида участия в сделке
        /// </summary>
        public string? credit_participation_code { get; set; }

        /// <summary>
        /// Сумма запрошенного займа (кредита), лизинга или обеспечения
        /// </summary>
        public string? ca_sum { get; set; }

        /// <summary>
        /// Запрошенная валюта обязательства
        /// Если сумма запрошенного займа(кредита), лизинга или обеспечения указана, то параметр обязательный
        /// </summary>
        public string? ca_currency { get; set; }

        /// <summary>
        /// УИд обращения субъекта к источнику
        /// </summary>
        public string? ca_uid { get; set; }

        /// <summary>
        /// Дата обращения субъекта к источнику
        /// </summary>
        public string? ca_date { get; set; }

        /// <summary>
        /// Код источника
        /// </summary>
        public string? src_code { get; set; }

        /// <summary>
        /// Код способа обращения субъекта к источнику
        /// </summary>
        public string? ca_way_code { get; set; }

        /// <summary>
        /// Дата окончания действия инвестиционного предложения, одобрения обращения (оферты кредитора)
        /// </summary>
        public string? ca_expiration_date { get; set; }

        /// <summary>
        /// Дата окончания срока рассмотрения обращения
        /// </summary>
        public string? ca_consideration_expiration_date { get; set; }

        /// <summary>
        /// Код цели запрошенного займа (кредита)
        /// </summary>
        public string? credit_purpose_code { get; set; }

        /// <summary>
        /// Код стадии рассмотрения обращения
        /// </summary>
        public string? ca_consideration_status { get; set; }

        /// <summary>
        /// Дата перехода обращения в текущую стадию рассмотрения
        /// </summary>
        public string? ca_consideration_status_date { get; set; }

        /// <summary>
        /// Код вида обращения
        /// </summary>
        public string? ca_type { get; set; }

        /// <summary>
        /// Номер обращения
        /// </summary>
        public string? ca_num { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tSourceCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("11")]
        Item11,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("12")]
        Item12,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("13")]
        Item13,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("14")]
        Item14,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("15")]
        Item15,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("16")]
        Item16,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("17")]
        Item17,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tWayCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tCreditPurposeCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.1")]
        Item21,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.2")]
        Item22,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.3")]
        Item23,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.4")]
        Item24,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.5")]
        Item25,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.6")]
        Item26,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.7")]
        Item27,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.1")]
        Item41,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.2")]
        Item42,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.3")]
        Item43,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.4")]
        Item44,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.5")]
        Item45,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.6")]
        Item46,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.7")]
        Item47,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.8")]
        Item48,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.9")]
        Item49,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("11")]
        Item11,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("12")]
        Item12,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("13")]
        Item13,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("14")]
        Item14,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("15")]
        Item15,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("16.1")]
        Item161,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("16.2")]
        Item162,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("16.3")]
        Item163,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("17")]
        Item17,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("18")]
        Item18,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("19")]
        Item19,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("20")]
        Item20,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tConsiderationStatusCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tApplicationCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// Информационная часть кредитной истории
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddInfo
    {
        /// <summary>
        /// Сведения об обращении (ФЛ_55 = ЮЛ_45)
        /// </summary>
        public tCreditApplication? credit_application { get; set; }

        /// <summary>
        /// Сведения об участии в обязательстве, по которому формируется кредитная история (ФЛ_56 = ЮЛ_46)
        /// </summary>
        public tCreditParticipation? credit_participation { get; set; }

        /// <summary>
        /// Сведения об отказе источника от предложения совершить сделку (ФЛ_57 = ЮЛ_47)
        /// </summary>
        public tCreditRefusal? credit_refusal { get; set; }
    }

    /// <summary>
    /// Закрытая (дополнительная) часть кредитной истории, измененная в рамках коррекции или аннуляции отдельных показателей кредитной информации
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddPrivateChanged
    {
        /// <remarks/>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? event_id { get; set; }

        /// <summary>
        /// Сведения об источнике формирования кредитных историй - арбитражном управляющем (ФЛ_48 = ЮЛ_38)
        /// </summary>
        public tSRCBankruptcyComissioner? src_bankruptcy_comissioner { get; set; }

        /// <summary>
        /// Сведения об источнике формирования кредитных историй - физическом лице (ФЛ_47 = ЮЛ_37)
        /// </summary>
        public tSRCIndividual? src_individual { get; set; }

        /// <summary>
        /// Сведения об источнике формирования кредитных историй - юридическом лице (ФЛ_46 = ЮЛ_36)
        /// </summary>
        public tSRCLegalEntity? src_legal_entity { get; set; }

        /// <summary>
        /// Сведения о пользователе кредитной истории - индивидуальном педпринимателе (ФЛ_50 = ЮЛ_40)
        /// </summary>
        public tUserIndividualEntrpreneur? user_individual_entrpreneur { get; set; }

        /// <summary>
        /// Сведения о пользователе кредитной истории - юридическом лице (ФЛ_49 = ЮЛ_39)
        /// </summary>
        public tUserLegalEntity? user_legal_entity { get; set; }

        /// <summary>
        /// Сведения о приобретателе прав кредитора - физическом лице (ФЛ_52 = ЮЛ_42)
        /// </summary>
        public tAcquirerIndividual? acquirer_individual { get; set; }

        /// <summary>
        /// Сведения о приобретателе прав кредитора - юридическом лице (ФЛ_51 = ЮЛ_41)
        /// </summary>
        public tAcquirerLegalEntity? acquirer_legal_entity { get; set; }

        /// <summary>
        /// Сведения об обслуживающей организации (ФЛ_53 = ЮЛ_43)
        /// </summary>
        public tServiceCompany? service_company { get; set; }

        /// <summary>
        /// Сведения об учете обязательства, о льготном финансировании с государственной поддержкой и процентной ставке (ФЛ_54 = ЮЛ_44)
        /// </summary>
        public tObligationAccountingInfo? obligation_accounting_info { get; set; }
    }

    /// <summary>
    /// Сведения об источнике формирования кредитных историй - арбитражном управляющем (ФЛ_48 = ЮЛ_38)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tSRCBankruptcyComissioner
    {

        /// <summary>
        /// Сведения о наименовании источника формирования кредитной истории
        /// </summary>
        public tFullName? src_name { get; set; }

        /// <summary>
        /// Сведения о дате и месте рождения источника
        /// </summary>
        public tbDatePlace? src_birth_date_place { get; set; }

        /// <summary>
        /// Наименование саморегулируемой организации
        /// </summary>
        [XmlElementAttribute("self-regulatory_organization_name")]
        public string? selfregulatory_organization_name { get; set; }

        /// <summary>
        /// Адрес саморегулируемой организации
        /// </summary>
        [XmlElementAttribute("self-regulatory_organization_address")]
        public string? selfregulatory_organization_address { get; set; }

        /// <summary>
        /// Дата утверждения арбитражного управляющего
        /// </summary>
        public string? src_approval_date { get; set; }

        /// <summary>
        /// Дата прекращения полномочий арбитражного управляющего
        /// </summary>
        public string? date_powers_termination_date { get; set; }

        /// <summary>
        /// Сведения о номере налогоплательщика - источника формирования кредитной истории
        /// </summary>
        [XmlElementAttribute("src_itn")]
        public List<tInfoITN>? src_itn { get; set; }

        /// <summary>
        /// СНИЛС
        /// </summary>
        public string? src_snils { get; set; }

        /// <summary>
        /// Дата формирования кредитной информации
        /// </summary>
        public string? credit_info_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tFullName
    {

        /// <summary>
        /// Фамилия
        /// </summary>
        public string? surname { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string? name { get; set; }

        private string? _patronymic;

        /// <summary>
        /// Отчество
        /// </summary>
        public string? patronymic
        {
            get
            {
                return this._patronymic;
            }
            set
            {
                this._patronymic = value == "-" ? null : value;
            }
        }
    }

    /// <summary>
    /// Сведения о дате и месте рождения источника
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tbDatePlace
    {

        /// <summary>
        /// Дата рождения
        /// </summary>
        public string? birth_date { get; set; }

        /// <summary>
        /// Место рождения
        /// </summary>
        public string? birth_place { get; set; }
    }

    /// <summary>
    /// Сведения о номере налогоплательщика - источника формирования кредитной истории
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tInfoITN
    {

        /// <summary>
        /// Код номера налогоплательщика
        /// </summary>
        public string? itn_code { get; set; }

        /// <summary>
        /// Номер налогоплательщика (ИНН)
        /// </summary>
        public string? itn { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tITNCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,
    }

    /// <summary>
    /// Сведения об источнике формирования кредитных историй - физическом лице (ФЛ_47 = ЮЛ_37)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tSRCIndividual
    {

        /// <summary>
        /// Сведения о наименовании источника формирования кредитной истории
        /// </summary>
        public tFullName? src_name { get; set; }

        /// <summary>
        /// Сведения о дате и месте рождения источника
        /// </summary>
        public tbDatePlace? src_birth_date_place { get; set; }

        /// <summary>
        /// Реквизиты документа, удостоверяющего личность источника
        /// </summary>
        public tIdentityDocData? src_identity_doc_data { get; set; }

        /// <summary>
        /// Сведения о регистрационном номере источника формирования кредитной истории
        /// </summary>
        public tInfoSRN? src_psrn { get; set; }

        /// <summary>
        /// Дата формирования кредитной информации
        /// </summary>
        public string? credit_info_date { get; set; }
    }

    /// <summary>
    /// Реквизиты документа, удостоверяющего личность источника
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tIdentityDocData
    {

        /// <summary>
        /// Код документа, удостоверяющего личность
        /// </summary>
        public string? d_code { get; set; }

        /// <summary>
        /// Наименование иного документа, удостоверяющего личность
        /// </summary>
        public string? d_other_name { get; set; }

        /// <summary>
        /// Серия документа, удостоверяющего личность
        /// </summary>
        public string? d_series { get; set; }

        /// <summary>
        /// Номер документа, удостоверяющего личность
        /// </summary>
        public string? d_number { get; set; }

        /// <summary>
        /// Дата выдачи документа, удостоверяющего личность
        /// </summary>
        public string? d_issue_date { get; set; }

        /// <summary>
        /// Наименование государственного учреждения, выдавшего документ, удостоверяющего личность
        /// </summary>
        public string? d_authority { get; set; }

        /// <summary>
        /// Код подразделения, выдавшего документ, удостоверяющий личность
        /// </summary>
        public string? d_department_code { get; set; }

        public string? d_expired_date { get; set; }
    }

    /// <summary>
    /// Сведения о регистрационном номере источника формирования кредитной истории
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tInfoSRN
    {

        /// <summary>
        /// Код регистрационного номера
        /// </summary>
        public string? psrn_code { get; set; }

        /// <summary>
        /// Регистрационный номер
        /// </summary>
        public string? psrn { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tPSRNCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,
    }

    /// <summary>
    /// Сведения об источнике формирования кредитных историй - юридическом лице (ФЛ_46 = ЮЛ_36)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tSRCLegalEntity
    {

        /// <summary>
        /// Код источника
        /// </summary>
        public string? src_code { get; set; }

        /// <summary>
        /// Признак регистрации юридического лица в РФ
        /// </summary>
        public string? registered_in_rf { get; set; }

        /// <summary>
        /// Сведения о наименовании источника формирования кредитной истории
        /// </summary>
        public tName? src_name { get; set; }

        /// <summary>
        /// Дата создания источника формирования кредитной истории
        /// </summary>
        public string? src_creation_date { get; set; }

        /// <summary>
        /// Сведения о регистрационном номере и идентификаторе источника формирования кредитной истории
        /// </summary>
        public tleRegNum? src_reg_num { get; set; }

        /// <summary>
        /// Сведения о номере налогоплательщика - источника формирования кредитной истории
        /// </summary>
        [XmlElementAttribute("src_itn")]
        public List<tInfoITN>? src_itn { get; set; }

        /// <summary>
        /// Дата признания источника банкротом
        /// </summary>
        public string? src_bankruptcy_date { get; set; }

        /// <summary>
        /// Дата окончания конкурсного производства
        /// </summary>
        public string? src_bp_termination_date { get; set; }

        /// <summary>
        /// Дата начала ликвидации источника формирования кредитной истории
        /// </summary>
        public string? src_liquidation_start_date { get; set; }

        /// <summary>
        /// Дата окончания ликвидации источника формирования кредитной истории
        /// </summary>
        public string? src_liquidation_termination_date { get; set; }

        /// <summary>
        /// Дата формирования кредитной информации
        /// </summary>
        public string? credit_info_date { get; set; }
    }

    /// <summary>
    /// Сведения о наименовании источника формирования кредитной истории
    /// </summary>
    [XmlIncludeAttribute(typeof(tLeName))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tName
    {

        /// <summary>
        /// Полное наименование
        /// </summary>
        public string? full_name { get; set; }

        /// <summary>
        /// Краткое наименование
        /// </summary>
        public string? short_name { get; set; }

        /// <summary>
        /// Иное наименование
        /// </summary>
        public string? other_name { get; set; }
    }

    /// <summary>
    /// Сведения о наименовании источника формирования кредитной истории
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tLeName : tName
    {
        /// <summary>
        /// Признак смены наименования
        /// </summary>
        public string? name_changed { get; set; }

        /// <summary>
        /// Полное наименование до его смены
        /// </summary>
        public string? old_full_name { get; set; }
    }

    /// <summary>
    /// Сведения о регистрационном номере и идентификаторе юридического лица (ЮЛ_3)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tleRegNum
    {

        /// <summary>
        /// Сведения о регистрационном номере юридического лица
        /// </summary>
        public tInfoSRN? le_psrn { get; set; }

        /// <summary>
        /// Идентификатор LEI юридического лица
        /// </summary>
        public string? lei { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tUserIndividualEntrpreneur
    {

        /// <summary>
        /// Сведения о наименовании пользователя кредитной истории
        /// </summary>
        public tFullName? user_name { get; set; }

        /// <summary>
        /// Сведения о дате и месте рождения пользователя кредитной истории
        /// </summary>
        public tbDatePlace? user_birth_date_place { get; set; }

        /// <summary>
        /// Сведения о номере налогоплательщика - пользователя кредитной истории
        /// </summary>
        [XmlElementAttribute("user_itn")]
        public List<tInfoITN>? user_itn { get; set; }

        /// <summary>
        /// Сведения о регистрационном номере пользователя кредитной истории
        /// </summary>
        public tInfoSRN? user_psrn { get; set; }

        /// <summary>
        /// СНИЛС пользователя кредитной истории
        /// </summary>
        public string? user_snils { get; set; }

        /// <summary>
        /// Реквизиты документа, удостоверяющего личность пользователя кредитной истории
        /// </summary>
        public tIdentityDocData? user_identity_doc_data { get; set; }

        /// <summary>
        /// Признак мониторинга изменения кредитной истории
        /// </summary>
        public string? change_monitoring { get; set; }

        /// <summary>
        /// Дата начала мониторинга изменения кредитной истории
        /// </summary>
        public string? change_monitoring_start_date { get; set; }

        /// <summary>
        /// Сведения о запросе информации пользователем кредитной истории (ФЛ_44 = ЮЛ_34)
        /// </summary>
        public tObligationRequest? obligation_info_request { get; set; }
    }

    /// <summary>
    /// Сведения о запросе информации пользователем кредитной истории (ФЛ_44 = ЮЛ_34)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tObligationRequest
    {
        /// <summary>
        /// Код запрошенных пользователем сведений
        /// </summary>
        public tRequestedCode? code_requested_info { get; set; }

        /// <summary>
        /// Дата предоставления сведений пользователю
        /// </summary>
        public string? info_provision_date { get; set; }

        /// <summary>
        /// Дата запроса информации пользователем
        /// </summary>
        public string? request_date { get; set; }

        /// <summary>
        /// Код цели запроса информации пользователем
        /// </summary>
        public tRequestPurposeCode? request_purpose_code { get; set; }

        /// <summary>
        /// Иная цель запроса информации пользователем
        /// </summary>
        public string? other_request_purpose { get; set; }

        /// <summary>
        /// Сумма обязательства, в связи с которым пользователем сделан запрос
        /// </summary>
        public string? obligation_sum { get; set; }

        /// <summary>
        /// Валюта обязательства, в связи с которым пользователем сделан запрос
        /// </summary>
        public string? obligation_currency { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tRequestedCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tRequestPurposeCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.1")]
        Item11,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("11")]
        Item111,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("12")]
        Item12,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("13")]
        Item13,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("13.1")]
        Item131,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("14")]
        Item14,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("14.1")]
        Item141,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("15")]
        Item15,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("16")]
        Item16,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("17")]
        Item17,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("18")]
        Item18,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("19")]
        Item19,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("20")]
        Item20,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("21")]
        Item21,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("22")]
        Item22,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("23")]
        Item23,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("24")]
        Item24,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("25")]
        Item25,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("26")]
        Item26,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("27")]
        Item27,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// Сведения о пользователе кредитной истории - юридическом лице (ФЛ_49 = ЮЛ_39)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tUserLegalEntity
    {

        /// <summary>
        /// Код пользователя кредитной истории
        /// </summary>
        public tUserCode? user_code { get; set; }

        /// <summary>
        /// Признак регистрации юридического лица в РФ
        /// </summary>
        public string? registered_in_rf { get; set; }

        /// <summary>
        /// Сведения о наименовании пользователя кредитной истории
        /// </summary>
        public tName? user_name { get; set; }

        /// <summary>
        /// Сведения о регистрационном номере и идентификаторе пользователя кредитной истории
        /// </summary>
        public tleRegNum? user_reg_num { get; set; }

        /// <summary>
        /// Сведения о номере налогоплательщика - пользователя кредитной истории
        /// </summary>
        [XmlElementAttribute("user_itn")]
        public List<tInfoITN>? user_itn { get; set; }

        /// <summary>
        /// Признак мониторинга изменения кредитной истории
        /// </summary>
        public string? change_monitoring { get; set; }

        /// <summary>
        /// Дата начала мониторинга изменения кредитной истории
        /// </summary>
        public string? change_monitoring_start_date { get; set; }

        /// <summary>
        /// Сведения о запросе информации пользователем кредитной истории (ФЛ_44 = ЮЛ_34)
        /// </summary>
        public tObligationRequest? obligation_info_request { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tUserCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// Сведения о приобретателе прав кредитора - физическом лице (ФЛ_52 = ЮЛ_42)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAcquirerIndividual
    {

        /// <summary>
        /// Сведения о наименовании приобретателя прав кредитора
        /// </summary>
        public tFullName? acquirer_name { get; set; }

        /// <summary>
        /// Сведения о дате и месте рождения приобретателя прав кредитора
        /// </summary>
        public tbDatePlace? acquirer_birth_date_place { get; set; }

        /// <summary>
        /// Сведения о номере налогоплательщика - приобретателя прав кредитора
        /// </summary>
        [XmlElementAttribute("acquirer_itn")]
        public List<tInfoITN>? acquirer_itn { get; set; }

        /// <summary>
        /// СНИЛС приобретателя прав кредитора
        /// </summary>
        public string? acquirer_snils { get; set; }

        /// <summary>
        /// Реквизиты документа, удостоверяющего личность приобретателя прав кредитора
        /// </summary>
        public tIdentityDocData? acquirer_identity_doc_data { get; set; }

        /// <summary>
        /// Дата приобретения прав кредитора
        /// </summary>
        public string? acquirer_date { get; set; }
    }

    /// <summary>
    /// Сведения о приобретателе прав кредитора - юридическом лице (ФЛ_51 = ЮЛ_41)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAcquirerLegalEntity
    {

        /// <summary>
        /// Код приобретателя прав кредитора
        /// </summary>
        public string? acquirer_code { get; set; }

        /// <summary>
        /// Признак регистрации юридического лица в РФ
        /// </summary>
        public string? registered_in_rf { get; set; }

        /// <summary>
        /// Сведения о наименовании приобретателя прав кредитора
        /// </summary>
        public tName? acquirer_name { get; set; }

        /// <summary>
        /// Сведения о регистрационном номере и идентификаторе приобретателя прав кредитора
        /// </summary>
        public tleRegNum? acquirer_reg_num { get; set; }

        /// <summary>
        /// Сведения о номере налогоплательщика - приобретателя прав кредитора
        /// </summary>
        [XmlElementAttribute("acquirer_itn")]
        public List<tInfoITN>? acquirer_itn { get; set; }

        /// <summary>
        /// Дата приобретения прав кредитора
        /// </summary>
        public string? acquirer_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tAcquirerCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// Сведения об обслуживающей организации (ФЛ_53 = ЮЛ_43)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tServiceCompany
    {

        /// <summary>
        /// Признак регистрации обслуживающей организации в РФ
        /// </summary>
        public string? registered_in_rf { get; set; }

        /// <summary>
        /// Сведения о наименовании обслуживающей организации
        /// </summary>
        public tName? company_name { get; set; }

        /// <summary>
        /// Сведения о регистрационном номере обслуживающей организации
        /// </summary>
        public tInfoSRN? company_psrn { get; set; }

        /// <summary>
        /// Сведения о номере налогоплательщика обслуживающей организации
        /// </summary>
        public tInfoITN? company_itn { get; set; }

        /// <summary>
        /// Дата начала действия договора обслуживания
        /// </summary>
        public string? contract_start_date { get; set; }

        /// <summary>
        /// Дата окончания действия договора обслуживания
        /// </summary>
        public string? contract_expiration_date { get; set; }

        /// <summary>
        /// Наименование эмитента
        /// </summary>
        public string? issuer_name { get; set; }

        /// <summary>
        /// Сведения о регистрационном номере эмитента
        /// </summary>
        public tInfoSRN? issuer_reg_number { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tObligationAccountingInfo
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? obligation_accounting { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? interest_rate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("obligation_sum_off-balance")]
        public string? obligation_sum_offbalance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? state_support { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? state_support_program { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddPrivate
    {
        /// <summary>
        /// 
        /// </summary>
        public tSRCBankruptcyComissioner? src_bankruptcy_comissioner { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tSRCIndividual? src_individual { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tSRCLegalEntity? src_legal_entity { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tUserIndividualEntrpreneur? user_individual_entrpreneur { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tUserLegalEntity? user_legal_entity { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAcquirerIndividual? acquirer_individual { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAcquirerLegalEntity? acquirer_legal_entity { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tServiceCompany? service_company { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tObligationAccountingInfo? obligation_accounting_info { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tTransferTermination
    {

        /// <summary>
        /// 
        /// </summary>
        public string? tt_reason_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? tt_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tDebtCollection
    {

        /// <summary>
        /// 
        /// </summary>
        public string? debt_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tJudgementInfo? debt_collection_judgement { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? judgement_operative_part { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? executive_document_num { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? dc_termination_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("one-time_collection_sum")]
        public string? onetime_collection_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? collected_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? calculation_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? periodic_collection_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? payment_frequency_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? debt_collection_currency { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tDebtCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tJudgementInfo
    {

        /// <summary>
        /// 
        /// </summary>
        public string? judgement_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? judgement_num { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? court_name { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tPaymentFrequencyCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tSourceLiquidation
    {

        /// <summary>
        /// 
        /// </summary>
        public string? debt_sum_at_start_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? start_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? debt_sum_at_termination_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? termination_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? debt_sum_at_last_payment_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? last_payment_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? creditor_rights_transfered { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? obligation_terminated { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_termination_reason_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? debt_currency { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAMPQCB
    {

        /// <summary>
        /// 
        /// </summary>
        public string? amp_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? amp_calculation_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? amp_currency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? uid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? qcb_reg_num { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? info_received_terminated { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tLitigation
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute()]
        public string? litigation_exists { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute()]
        public string? judgement { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? judgement_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? judgement_number { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? judgement_operative_part { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? judgement_into_force { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tObligationTerm
    {

        /// <summary>
        /// 
        /// </summary>
        public string? ot_reason_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ot_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tPaidSumReimbursement
    {

        /// <summary>
        /// 
        /// </summary>
        public string? reimbursement_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? sum_paid_by_principal { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? observance_reimbursement_rule { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? calculation_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? need_paid_sum_reimbursement { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tObligationRepayment
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlAttribute()]
        public string? obligation_repayment { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? code_of_used_pledge { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_repayment_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_repayment_sum { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tUsedPledgeCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tInsurancePS
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlAttribute()]
        public string? ins_exists { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ins_payments_limit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ins_payments_currency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? franchise { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ins_start_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ins_expiration_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ins_termination_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ins_termination_reason_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_subject_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tTerminationReasonCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tIndependentGuarantee
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlAttribute()]
        public string? ig_exists { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ig_uid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ig_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ig_currency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ig_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ig_expiration_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ig_termination_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ig_termination_reason_code { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tSurety
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlAttribute]
        public string? s_exists { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? s_contract_uid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? s_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? s_currency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? s_contract_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? s_expiration_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? s_termination_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? s_termination_reason_code { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tPledge
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlAttribute]
        public string? p_exists { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_subject { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_subject_id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_contract_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_subject_cost { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_subject_currency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_subject_valuation_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_expiration_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_termination_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_termination_reason_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_subject_cost_type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_all_obligations_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? p_contracts_number { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tObligationSub
    {

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_subject { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_perfomance_order { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? improper_obligation_perfomance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? received_property_code { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tObligationSrc
    {

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_subject { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? provided_property_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? property_transfer_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? provided_property_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAMP
    {

        /// <summary>
        /// 
        /// </summary>
        public string? amp_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? amp_calculation_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tMakingPayment
    {

        /// <summary>
        /// 
        /// </summary>
        public string? last_payment_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? last_payment_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? last_principal_payment { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? last_interest_payment { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? last_other_payment { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? all_payments_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? all_principal_payments_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? all_interest_payments_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? all_other_payments_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? payments_sum_observance_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? deadline_payments_observance_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? duration_of_overdue { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditOverdueDebt
    {

        /// <summary>
        /// 
        /// </summary>
        public string? overdue_debt_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? calculation_last_payment { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? od_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? od_sum_principal { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? interest_od_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? other_od_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? calculation_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? last_missed_principal_payment_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? last_missed_interest_payment_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditUrgentDebt
    {

        /// <summary>
        /// 
        /// </summary>
        public string? urgent_debt_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? calculation_last_payment { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ud_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ud_sum_principal { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? interest_ud_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? other_ud_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? calculation_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditDebt
    {

        /// <summary>
        /// 
        /// </summary>
        public string? debt_sum_on_funding_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? calculation_last_payment { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? debt_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? debt_sum_principal { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? interest_debt_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? other_debt_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? calculation_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? unconfirmed_grace_period { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? debt { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tFundingTransferInfo
    {

        /// <summary>
        /// 
        /// </summary>
        public string? funding_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "positiveInteger")]
        public string? tranche_number { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditContractChange
    {

        /// <summary>
        /// 
        /// </summary>
        public string? cc_change_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? cc_change_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? cc_special_change_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? cc_other_change { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? cc_change_date_into_force { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? cc_change_expiration_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? cc_change_termination_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? cc_change_termination_reason_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? currency_conversation_rate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? contract_changed { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tChangeCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tSpecialChangeCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("11")]
        Item11,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("12")]
        Item12,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("13")]
        Item13,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("14")]
        Item14,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("15")]
        Item15,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("16")]
        Item16,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("17")]
        Item17,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("18")]
        Item18,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("19")]
        Item19,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("20")]
        Item20,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("21")]
        Item21,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("22")]
        Item22,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tChangeTerminationCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditTotalSum
    {

        /// <summary>
        /// 
        /// </summary>
        public string? total_sum_percent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? total_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? total_sum_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tPaymentConditions
    {

        /// <summary>
        /// 
        /// </summary>
        public string? next_principal_payment_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? next_principal_payment_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? next_interest_payment_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? next_interest_payment_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? payment_frequency_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? card_payment_sum_min { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? free_period_begin_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? free_period_end_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? interest_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tJointDebtor
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "positiveInteger")]
        public string? joint_debtor_quantity { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? joint_debtor_exist { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditSum
    {

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_currency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? secured_obligation_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? secured_obligation_currency { get; set; }

        private string? _secured_obligation_type;

        /// <summary>
        /// 
        /// </summary>
        public string? secured_obligation_type
        {
            get
            {
                return _secured_obligation_type;
            }
            set
            {
                _secured_obligation_type = value == "-" ? "99" : value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string? calculation_date { get; set; }

        private string? _secured_obligation_uid;

        /// <summary>
        /// 
        /// </summary>
        public string? secured_obligation_uid
        {
            get
            {
                return _secured_obligation_uid;
            }
            set
            {
                _secured_obligation_uid = value == "-" ? null : value;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditDeal
    {

        /// <summary>
        /// 
        /// </summary>
        public string? deal_participation_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? deal_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? deal_type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? credit_type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("credit_purpose_code")]
        public List<string>? credit_purpose_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? consumer_credit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? payment_card { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_after_novation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? money_obligation_of_src { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? money_obligation_of_sub { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_expiration_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private string? _creditor_type;

        /// <summary>
        /// 
        /// </summary>
        public string? creditor_type
        {
            get
            {
                return _creditor_type;
            }
            set
            {
                _creditor_type = value == "-" ? "6" : value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_as_getting_partial_rights { get; set; }

        private string? _credit_line;

        /// <summary>
        /// 
        /// </summary>
        public string? credit_line
        {
            get
            {
                return _credit_line;
            }
            set
            {
                _credit_line = value == "-" ? "0" : value;
            }
        }

        private string? _credit_line_type;

        /// <summary>
        /// 
        /// </summary>
        public string? credit_line_type
        {
            get
            {
                return _credit_line_type;
            }
            set
            {
                _credit_line_type = value == "-" ? null : value;
            }
        }

        private string? _floating_interest_rate;

        /// <summary>
        /// 
        /// </summary>
        public string? floating_interest_rate
        {
            get
            {
                return _floating_interest_rate;
            }
            set
            {
                _floating_interest_rate = value == "-" ? "0" : value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string? partial_rights_transfered { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? partial_rights_transfered_uid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tDealId
    {

        /// <summary>
        /// 
        /// </summary>
        public string? uid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? deal_num { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditScore
    {

        /// <summary>
        /// 
        /// </summary>
        public string? credit_score_value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? credit_score_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tRatingFactor
    {

        /// <summary>
        /// 
        /// </summary>
        public tFactorCode? factor_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? factor_share { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tFactorCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("11")]
        Item11,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("12")]
        Item12,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("13")]
        Item13,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("14")]
        Item14,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("15")]
        Item15,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("16")]
        Item16,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("17")]
        Item17,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("18")]
        Item18,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("19")]
        Item19,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("20")]
        Item20,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("21")]
        Item21,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("22")]
        Item22,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tCreditRating
    {

        /// <summary>
        /// 
        /// </summary>
        public string? rating_value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? calculation_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("rating_factor")]
        public List<tRatingFactor>? rating_factor { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? no_calculation_reason_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? no_calculation_other_reason { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tNoCalculationCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("99")]
        Item99,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tObligationExcemption
    {

        /// <summary>
        /// 
        /// </summary>
        public string? settlements_complete_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? obligation_excemption { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_excemption_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? obligation_restoration_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? settlements_complete { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tBankruptcy
    {

        /// <summary>
        /// 
        /// </summary>
        public string? bankruptcy_stage_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? bankruptcy_case_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? publication_link { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? illegal_actions { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? illegal_actions_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? fictitious_bankruptcy { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? fictitious_bankruptcy_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? additional_info { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? bankruptcy_case { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tBankruptcyStageCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("6")]
        Item6,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("7")]
        Item7,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("8")]
        Item8,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("9")]
        Item9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("11")]
        Item11,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("12")]
        Item12,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("13")]
        Item13,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("14")]
        Item14,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tLegalCapacity
    {

        /// <summary>
        /// 
        /// </summary>
        public string? legal_capacity_code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public tJudgementInfo? incapacity_judgement { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public tJudgementInfo? legal_capacity_judgement { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tIndividualEntrpreneurReg
    {

        /// <summary>
        /// 
        /// </summary>
        public tInfoSRN? ie_psrn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ie_reg_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute()]
        public string? individual_entrpreneur { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tActualAddress
    {

        /// <summary>
        /// 
        /// </summary>
        public tAddress? address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? actual_address_dif { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddress
    {

        /// <summary>
        /// 
        /// </summary>
        public string? index { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? country_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? another_country_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? fias_num { get; set; }

        private string? _locality_code;

        /// <summary>
        /// 
        /// </summary>
        public string? locality_code
        {
            get
            {
                return _locality_code;
            }
            set
            {
                _locality_code = value == "-" ? "99999999999" : value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string? other_locality_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? street { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? house { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ownership { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? housing { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? building { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? apartment { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tRegAddress
    {

        /// <summary>
        /// 
        /// </summary>
        public string? reg_address_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddress? address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? reg_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? reg_authority_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? reg_authority_code { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tAddressCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddMain
    {
        /// <summary>
        /// 
        /// </summary>
        public tRegAddress? i_reg_address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tActualAddress? i_actual_address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tContacts? i_contacts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tIndividualEntrpreneurReg? i_individual_entrpreneur_reg { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tLegalCapacity? i_legal_capacity { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tBankruptcy? bankruptcy { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tObligationExcemption? obligation_excemption_because_of_bankruptcy { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tCreditRating? i_credit_rating { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tCreditScore? credit_score { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tDealId? deal_id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tCreditDeal? credit_deal { get; set; }

        /// <remarks/>
        [XmlElementAttribute("credit_sum")]
        public List<tCreditSum>? credit_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tJointDebtor? joint_debtor { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tPaymentConditions? payment_conditions { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tCreditTotalSum? i_credit_total_sum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("credit_contract_change")]
        public List<tCreditContractChange>? credit_contract_change { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("funding_transfer_info")]
        public List<tFundingTransferInfo>? funding_transfer_info { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tCreditDebt? credit_debt { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tCreditUrgentDebt? credit_urgent_debt { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tCreditOverdueDebt? credit_overdue_debt { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tMakingPayment? making_payment { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAMP? i_amp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("non-money_obligation_of_src")]
        public tObligationSrc? nonmoney_obligation_of_src { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("non-money_obligation_of_sub")]
        public tObligationSub? nonmoney_obligation_of_sub { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("pledge")]
        public List<tPledge>? pledge { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("surety")]
        public List<tSurety>? surety { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("independent_guarantee")]
        public List<tIndependentGuarantee>? independent_guarantee { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("insurance_of_pledge_subject")]
        public List<tInsurancePS>? insurance_of_pledge_subject { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tObligationRepayment? obligation_repayment_against_pledge { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tPaidSumReimbursement? paid_sum_reimbursement { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tObligationTerm? obligation_termination { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("litigation")]
        public List<tLitigation>? litigation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAMPQCB? i_amp_qcb { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tSourceLiquidation? bankruptcy_proceedings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tSourceLiquidation? source_liquidation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tDebtCollection? debt_collection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tTransferTermination? obligation_info_transfer_termination { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tContacts
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("phone")]
        public List<tPhone>? phone { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("email")]
        public List<string>? email { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tPhone
    {

        /// <summary>
        /// 
        /// </summary>
        public string? phone_num { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? phone_comment { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tPhoneCommentCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1")]
        Item1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2")]
        Item2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3")]
        Item3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4")]
        Item4,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("5")]
        Item5,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("10")]
        Item10,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddLegalEntityChanged
    {

        /// <summary>
        /// 
        /// </summary>
        public tLeName? le_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddressAndContacts? le_address_and_contacts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tleRegNum? le_reg_num { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("le_itn")]
        public List<tInfoITN>? le_itn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("le_reorganization")]
        public List<tReorganization>? le_reorganization { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddressAndContacts
    {

        /// <summary>
        /// 
        /// </summary>
        public tAddressAndContactsLe_address? le_address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tContacts? le_contacts { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true)]
    public partial class tAddressAndContactsLe_address
    {

        /// <summary>
        /// 
        /// </summary>
        public string? country_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? another_country_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? fias_num { get; set; }

        private string? _locality_code;

        /// <summary>
        /// 
        /// </summary>
        public string? locality_code
        {
            get
            {
                return _locality_code;
            }
            set
            {
                _locality_code = value == "-" ? "99999999999" : value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string? other_locality_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? street { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? house { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ownership { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? housing { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? building { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? apartment { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tReorganization
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute(DataType = "integer")]
        public string? reorganized { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? full_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? short_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tInfoSRN? le_psrn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? reorganization_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddIndividualChanged
    {

        /// <summary>
        /// 
        /// </summary>
        public tFullName? i_full_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("i_old_full_name")]
        public List<tOldFullName>? i_old_full_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tibDatePlace? i_birth_date_place { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("i_identity_doc")]
        public List<tIdentityDoc>? i_identity_doc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("i_old_identity_doc")]
        public List<tOldIdentityDoc>? i_old_identity_doc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tRegNum? i_reg_num { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? i_snils { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tOldFullName
    {

        /// <summary>
        /// 
        /// </summary>
        public tFullName? i_full_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? new_name_issue_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? old_name { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tibDatePlace
    {

        /// <summary>
        /// 
        /// </summary>
        public string? birth_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? country_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? birth_place { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tIdentityDoc
    {

        /// <summary>
        /// 
        /// </summary>
        public tSitizenship? citizenship { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tiIdentityDocData? identity_doc_data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tSitizenship
    {

        /// <summary>
        /// 
        /// </summary>
        public string? country_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? another_country_name { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tiIdentityDocData
    {

        /// <summary>
        /// 
        /// </summary>
        public СправочникДУЛ? d_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? d_other_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? d_series { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? d_number { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? d_issue_date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? d_authority { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? d_department_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? d_expired_date { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tOldIdentityDoc
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlAttribute()]
        public string? old_identity_doc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tSitizenship? citizenship { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tIdentityDocData? identity_doc_data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tRegNum
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("i_itn")]
        public List<tInfoITN>? i_itn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tInfoSRN? i_psrn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? special_tax_mode { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddLegalEntity
    {

        /// <summary>
        /// 
        /// </summary>
        public tLeName? le_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddressAndContacts? le_address_and_contacts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tleRegNum? le_reg_num { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("le_itn")]
        public List<tInfoITN>? le_itn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("le_reorganization")]
        public List<tReorganization>? le_reorganization { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddIndividual
    {

        /// <summary>
        /// 
        /// </summary>
        public tFullName? i_full_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("i_old_full_name")]
        public List<tOldFullName>? i_old_full_name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tibDatePlace? i_birth_date_place { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("i_identity_doc")]
        public List<tIdentityDoc>? i_identity_doc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("i_old_identity_doc")]
        public List<tOldIdentityDoc>? i_old_identity_doc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tRegNum? i_reg_num { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? i_snils { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tOperation
    {

        /// <summary>
        /// 
        /// </summary>
        public string? operation_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? comment { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tOperationCode
    {

        /// <summary>
        /// 
        /// </summary>
        A,

        /// <summary>
        /// 
        /// </summary>
        B,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("C.1")]
        C1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("C.2")]
        C2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("C.9")]
        C9,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("D.1")]
        D1,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("D.2")]
        D2,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("D.3")]
        D3,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("D.4")]
        D4,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tBlocksGroup
    {
        /// <summary>
        /// 
        /// </summary>
        public tBlocksGroupEvent? @event { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tOperation? operation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddIndividual? add_individual { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddLegalEntity? add_legal_entity { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddIndividualChanged? add_individual_changed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddLegalEntityChanged? add_legal_entity_changed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddMain? add_main { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("add_main_changed")]
        public List<tAddMainChanged>? add_main_changed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddPrivate? add_private { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks/>
        [XmlElementAttribute("add_private_changed")]
        public List<tAddPrivateChanged>? add_private_changed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public tAddInfo? add_info { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlElementAttribute("add_info_changed")]
        public List<tAddInfoChanged>? add_info_changed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [XmlAttributeAttribute()]
        public string? id { get; set; }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class tBlocksGroupEvent
    {

        /// <remarks/>
        [XmlAttribute()]
        public string? event_date { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string? start_date { get; set; }

        /// <remarks/>
        [XmlText()]
        public string? Value { get; set; }
    }


    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddMainChanged
    {

        /// <remarks/>
        public tRegAddress? i_reg_address { get; set; }

        /// <remarks/>
        public tActualAddress? i_actual_address { get; set; }

        /// <remarks/>
        public tContacts? i_contacts { get; set; }

        /// <remarks/>
        public tIndividualEntrpreneurReg? i_individual_entrpreneur_reg { get; set; }

        /// <remarks/>
        public tLegalCapacity? i_legal_capacity { get; set; }

        /// <remarks/>
        public tBankruptcy? bankruptcy { get; set; }

        /// <remarks/>
        public tObligationExcemption? obligation_excemption_because_of_bankruptcy { get; set; }

        /// <remarks/>
        public tCreditRating? i_credit_rating { get; set; }

        /// <remarks/>
        public tCreditScore? credit_score { get; set; }

        /// <remarks/>
        public tDealId? deal_id { get; set; }

        /// <remarks/>
        public tCreditDeal? credit_deal { get; set; }

        /// <remarks/>
        [XmlElementAttribute("credit_sum")]
        public List<tCreditSum>? credit_sum { get; set; }

        /// <remarks/>
        public tJointDebtor? joint_debtor { get; set; }

        /// <remarks/>
        public tPaymentConditions? payment_conditions { get; set; }

        /// <remarks/>
        public tCreditTotalSum? i_credit_total_sum { get; set; }

        /// <remarks/>
        [XmlElementAttribute("credit_contract_change")]
        public List<tCreditContractChange>? credit_contract_change { get; set; }

        /// <remarks/>
        [XmlElementAttribute("funding_transfer_info")]
        public List<tFundingTransferInfo>? funding_transfer_info { get; set; }

        /// <remarks/>
        public tCreditDebt? credit_debt { get; set; }

        /// <remarks/>
        public tCreditUrgentDebt? credit_urgent_debt { get; set; }

        /// <remarks/>
        public tCreditOverdueDebt? credit_overdue_debt { get; set; }

        /// <remarks/>
        public tMakingPayment? making_payment { get; set; }

        /// <remarks/>
        public tAMP? i_amp { get; set; }

        /// <remarks/>
        [XmlElementAttribute("non-money_obligation_of_src")]
        public tObligationSrc? nonmoney_obligation_of_src { get; set; }

        /// <remarks/>
        [XmlElementAttribute("non-money_obligation_of_sub")]
        public tObligationSub? nonmoney_obligation_of_sub { get; set; }

        /// <remarks/>
        [XmlElementAttribute("pledge")]
        public List<tPledge>? pledge { get; set; }

        /// <remarks/>
        [XmlElementAttribute("surety")]
        public List<tSurety>? surety { get; set; }

        /// <remarks/>
        [XmlElementAttribute("independent_guarantee")]
        public List<tIndependentGuarantee>? independent_guarantee { get; set; }

        /// <remarks/>
        [XmlElementAttribute("insurance_of_pledge_subject")]
        public List<tInsurancePS>? insurance_of_pledge_subject { get; set; }

        /// <remarks/>
        public tObligationRepayment? obligation_repayment_against_pledge { get; set; }

        /// <remarks/>
        public tPaidSumReimbursement? paid_sum_reimbursement { get; set; }

        /// <remarks/>
        public tObligationTerm? obligation_termination { get; set; }

        /// <remarks/>
        [XmlElementAttribute("litigation")]
        public List<tLitigation>? litigation { get; set; }

        /// <remarks/>
        public tSourceLiquidation? bankruptcy_proceedings { get; set; }

        /// <remarks/>
        public tSourceLiquidation? source_liquidation { get; set; }

        /// <remarks/>
        public tDebtCollection? debt_collection { get; set; }

        /// <remarks/>
        public tTransferTermination? obligation_info_transfer_termination { get; set; }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string? @event { get; set; }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string? event_date { get; set; }

        /// <remarks/>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? event_id { get; set; }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class tAddInfoChanged
    {
        /// <remarks/>
        public tCreditApplication? credit_application { get; set; }

        /// <remarks/>
        public tCreditParticipation? credit_participation { get; set; }

        /// <remarks/>
        public tCreditRefusal? credit_refusal { get; set; }

        /// <remarks/>
        [XmlAttributeAttribute(DataType = "integer")]
        public string? event_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.9037.0")]
    [System.SerializableAttribute()]
    public enum tEventCode
    {

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.1")]
        Item11,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.2")]
        Item12,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.3")]
        Item13,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.4.0.1")]
        Item1401,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.4.0.2")]
        Item1402,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.4.0.3")]
        Item1403,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.4.1")]
        Item141,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.5")]
        Item15,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.6")]
        Item16,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.7")]
        Item17,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.8")]
        Item18,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.9")]
        Item19,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.10")]
        Item110,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.11")]
        Item111,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.12")]
        Item112,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.13")]
        Item113,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.14")]
        Item114,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("1.15")]
        Item115,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.1.1")]
        Item211,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.1.2")]
        Item212,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.2.0.1")]
        Item2201,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.2.0.2")]
        Item2202,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.2.1.1")]
        Item2211,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.2.1.2")]
        Item2212,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.3.1")]
        Item231,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.3.2")]
        Item232,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.4")]
        Item24,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.5.1")]
        Item251,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.5.2")]
        Item252,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.6")]
        Item26,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.7")]
        Item27,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.8.1")]
        Item281,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.8.2")]
        Item282,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.8.3")]
        Item283,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.9.1")]
        Item291,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.9.2")]
        Item292,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.9.3")]
        Item293,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.10")]
        Item210,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.11.1.1")]
        Item21111,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.11.1.2")]
        Item21112,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.11.2.1")]
        Item21121,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.11.2.2")]
        Item21122,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("2.12")]
        Item2121,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3.1")]
        Item31,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3.2")]
        Item32,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3.3")]
        Item33,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3.4")]
        Item34,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3.5")]
        Item35,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("3.6")]
        Item36,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.1")]
        Item41,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.2")]
        Item42,

        /// <summary>
        /// 
        /// </summary>
        [XmlEnumAttribute("4.3")]
        Item43,
    }

}
