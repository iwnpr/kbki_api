using QBCH_lib.qcb_xml.v3_0.qcb_putanswer;
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
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class ПредставлениеСведенийОПлатежахДоговор
    {
        /// <remarks/>
        public РезультатПредставленияСведенийБКИ БКИ { get; set; }

        /// <remarks/>
        [XmlElement("Результат")]
        public List<РезультатПредставленияСведенийРезультат> Результат { get; set; } = new();

        /// <remarks/>
        [XmlAttribute()]
        public string Версия { get; set; } = "3.0";

        /// <remarks/>
        [XmlAttribute(DataType = "date")]
        public DateTime ДатаЗапроса { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string ИдентификаторОтвета { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string ОГРН { get; set; }
    }
}