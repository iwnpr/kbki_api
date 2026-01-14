using System.Collections.Generic;

namespace Domain.QBCHModels.qcb_xml.v2_0.qcb_result
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public class РезультатПредставленияСведений
    {

        /// <remarks/>
        public РезультатПредставленияСведенийБКИ БКИ { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItem("Договор", IsNullable = false)]
        public List<РезультатПредставленияСведенийДоговор> Договоры { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Версия { get; set; } = "2.0";

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public DateTime ДатаЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторОтвета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ОГРН { get; set; }
    }
}