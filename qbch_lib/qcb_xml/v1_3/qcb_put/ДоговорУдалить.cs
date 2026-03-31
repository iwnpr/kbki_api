using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v1_3.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class ДоговорУдалить
    {

        /// <remarks/>
        [XmlAttribute()]
        public string? Причина { get; set; }

        /// <remarks/>
        [XmlAttribute]
        public string? ДатаРасчета { get; set; }
    }
}