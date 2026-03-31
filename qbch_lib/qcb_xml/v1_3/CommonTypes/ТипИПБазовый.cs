using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v1_3.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипИПБазовый
    {

        /// <remarks/>
        [XmlElement("ИННИП")]
        public string? ИНН { get; set; }

        /// <remarks/>
        [XmlElement("ОГРНИП")]
        public string? ОГРН { get; set; }

        /// <remarks/>
        public ТипФИО? ФИО { get; set; }
    }
}