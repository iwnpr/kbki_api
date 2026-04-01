using QBCH_lib.qcb_xml.v3_0.CommonTypes;
using System;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ДоговорДобавить
    {
        /// <remarks/>
        public ТипСреднемесячныйПлатеж СреднемесячныйПлатеж { get; set; }

        /// <remarks/>
        public string ПСК { get; set; }

        /// <remarks/>
        [XmlElement(DataType = "date")]
        public string? ДатаПрекращения { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string УИД { get; set; }

        /// <remarks/>
        [XmlAttribute(DataType = "date")]
        public DateTime Представлено { get; set; }
    }
}
