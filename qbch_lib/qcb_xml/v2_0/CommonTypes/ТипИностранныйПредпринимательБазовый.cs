using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v2_0.CommonTypes
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипИностранныйПредпринимательБазовый
    {

        /// <remarks/>
        [XmlElement("НомерНП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("РегНомер")]
        public string? ОГРН { get; set; }

        /// <remarks/>
        public ТипФИО ФИО { get; set; }
    }
}