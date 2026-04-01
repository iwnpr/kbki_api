using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot("ПредставлениеСведений", Namespace = "", IsNullable = false)]
    public class ПредставлениеСведений
    {
        /// <remarks/>
        public ПредставлениеСведенийБКИ БКИ { get; set; }

        /// <remarks/>
        [XmlElement("Сведения")]
        public List<ПредставлениеСведенийСведения> Сведения { get; set; } = new();
        /// <remarks/>
        [XmlAttribute()]
        public string Версия { get; set; } = "3.0";

        /// <remarks/>
        [XmlAttribute()]
        public string ИдентификаторЗапроса { get; set; }

        /// <remarks/>
        [XmlAttribute(DataType = "date")]
        public DateTime ДатаЗапроса { get; set; }
    }
}