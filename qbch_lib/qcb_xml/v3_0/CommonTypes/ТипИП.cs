using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипИП
    {

        /// <remarks/>
        [XmlElement("ИННИП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("ОГРНИП")]
        public string? ОГРН { get; set; }

        /// <remarks/>
        public string СНИЛС { get; set; }

        /// <remarks/>
        public ТипФИО ФИО { get; set; }

        /// <remarks/>
        public ТипДУЛПредпринимателя ДокументЛичности { get; set; }

        /// <remarks/>
        [XmlElement(DataType = "date")]
        public System.DateTime ДатаРождения { get; set; }

        /// <remarks/>
        public string МестоРождения { get; set; }
    }
}