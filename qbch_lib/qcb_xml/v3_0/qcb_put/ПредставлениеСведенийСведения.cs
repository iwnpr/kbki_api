using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class ПредставлениеСведенийСведения
    {
        /// <remarks/>
        [XmlElement("Договор", typeof(ПредставлениеСведенийДоговор))]
        [XmlElement("ОбращениеОбязательство", typeof(ПредставлениеСведенийОбращениеОбязательство))]
        public object Item { get; set; }
    }
}