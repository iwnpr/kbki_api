using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ДоговорУдалить
    {
        /// <remarks/>
        [XmlAttribute()]
        public string УИД { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? Причина { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public string? ДатаРасчета { get; set; }
    }
}
