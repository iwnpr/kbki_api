using System.Xml.Serialization;

namespace Domain.QBCHModels.qcb_xml.v2_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипИностранныйПредприниматель
    {

        /// <remarks/>
        [XmlElement("НомерНП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("РегНомер")]
        public string? ОГРН { get; set; }

        /// <remarks/>
        public ТипФИО ФИО { get; set; }

        /// <remarks/>
        public ТипДУЛПредпринимателя ДокументЛичности { get; set; }

        /// <remarks/>
        [XmlElement(DataType = "date")]
        public DateTime ДатаРождения { get; set; }

        /// <remarks/>
        public string МестоРождения { get; set; }
    }
}