using System.Xml.Serialization;
using Domain.QBCHModels.qcb_xml.v2_0.Enums;

namespace Domain.QBCHModels.qcb_xml.v2_0.CommonTypes
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипИЮ
    {

        /// <remarks/>
        [XmlElement("НомерНП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("РегНомер")]
        public string? ОГРН { get; set; }

        /// <remarks/>
        public string LEI { get; set; }

        /// <remarks/>
        public string ПолноеНаименование { get; set; }

        /// <remarks/>
        public string СокращенноеНаименование { get; set; }

        /// <remarks/>
        public string ИноеНаименование { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public СправочникВидыПользователя КодВидаПользователя { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string ПризнакРегистрацииРФ { get; set; } = "0";
    }
}