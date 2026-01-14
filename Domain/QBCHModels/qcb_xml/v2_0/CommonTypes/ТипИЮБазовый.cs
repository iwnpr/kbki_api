using System.Xml.Serialization;

namespace Domain.QBCHModels.qcb_xml.v2_0.CommonTypes
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипИЮБазовый
    {

        /// <remarks/>
        [XmlElement("НомерНП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("РегНомер")]
        public string? ОГРН { get; set; }

        /// <remarks/>
        public string ПолноеНаименование { get; set; }
    }
}