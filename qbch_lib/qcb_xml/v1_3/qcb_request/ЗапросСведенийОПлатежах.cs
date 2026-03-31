using QBCH_lib.qcb_xml.v1_3.Enums;

namespace QBCH_lib.qcb_xml.v1_3.qcb_request
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public partial class ЗапросСведенийОПлатежах
    {
        /// <remarks/>
        public ЗапросСведенийОПлатежахАбонент? Абонент { get; set; }

        /// <remarks/>
        public ЗапросСведенийОПлатежахЗапрос? Запрос { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? Версия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ИдентификаторЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public ЗапросСведенийОПлатежахТипЗапроса ТипЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnore()]
        public bool ТипЗапросаSpecified { get; set; }
    }
}