using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v1_3.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public partial class ПредставлениеСведенийОПлатежах
    {
        /// <remarks/>
        public БКИ? БКИ { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItem("Договор", IsNullable = false)]
        public List<ПредставлениеСведенийОПлатежахДоговор> Договоры { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? Версия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ИдентификаторЗапроса { get; set; }
    }
}