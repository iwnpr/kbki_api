using QBCH_lib.qcb_xml.v3_0.Enums;
using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v3_0.qcb_request
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public class ЗапросСведений
    {

        /// <remarks/>
        public ЗапросСведенийАбонент Абонент { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Запрос")]
        public List<ЗапросСведенийЗапрос> Запрос { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Версия { get; set; } = "3.0";

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public System.DateTime ДатаЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникСпособыЗапроса ТипЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникВидыСведений КодСведений { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникРежимыЗапроса РежимЗапроса { get; set; }
    }
}