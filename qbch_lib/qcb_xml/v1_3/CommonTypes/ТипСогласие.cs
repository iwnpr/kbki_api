using QBCH_lib.qcb_xml.v1_3.Enums;
using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v1_3.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public partial class ТипСогласие
    {
        /// <remarks/>
        public ТипСогласиеВыдано? Выдано { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Цель")]
        public List<ТипЦель>? Цель { get; set; }

        /// <remarks/>
        public ТипСогласиеДоговор? Договор { get; set; }

        /// <remarks/>
        public string? ХэшКод { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public System.DateTime ДатаВыдачи { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public КодыСрокаСогласия СрокДействия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute("ОснованиеПередачи")]
        public КодыОснованийПередачиСогласия TransferBasement { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnore()]
        public bool TransferBasementSpecified { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОбОтветственностиПредупрежден { get; set; }
    }
}