using QBCH_lib.qcb_xml.v1_3.CommonTypes;
using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v1_3.qcb_request
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ЗапросСведенийОПлатежахЗапрос
    {

        /// <remarks/>
        public ЗапросИсточник? Источник { get; set; }

        /// <remarks/>
        public ТипСубъектТитул? Субъект { get; set; }

        /// <remarks/>
        public ТипСогласие? Согласие { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Цель")]
        public List<ТипЦель>? Цель { get; set; }

        /// <remarks/>
        public ЗапросСуммаОбязательства? СуммаОбязательства { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public System.DateTime Дата { get; set; }
    }
}