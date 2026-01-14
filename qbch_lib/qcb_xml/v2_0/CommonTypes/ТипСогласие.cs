using System.Collections.Generic;
using QBCH_lib.qcb_xml.v2_0.Enums;

namespace QBCH_lib.qcb_xml.v2_0.CommonTypes
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипСогласие
    {

        /// <remarks/>
        public ТипСогласиеВыдано Выдано { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Цель")]
        public List<ТипЦель> Цель { get; set; } = new();

        /// <remarks/>
        public ТипСогласиеДоговор Договор { get; set; }

        /// <remarks/>
        public string ХэшКод { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public System.DateTime ДатаВыдачи { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникСрокиСогласия СрокДействия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникОснованияПередачиСогласия ОснованиеПередачи { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnore()]
        public bool ОснованиеПередачиSpecified { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ОбОтветственностиПредупрежден { get; set; } = "1";
    }
}