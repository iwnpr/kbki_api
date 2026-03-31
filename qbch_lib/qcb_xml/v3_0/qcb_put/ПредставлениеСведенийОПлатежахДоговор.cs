namespace QBCH_lib.qcb_xml.v3_0.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ПредставлениеСведенийОПлатежахДоговор
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Добавить", typeof(ДоговорДобавить))]
        [System.Xml.Serialization.XmlElement("Удалить", typeof(ДоговорУдалить))]
        public object Item { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string УИД { get; set; }
    }
}