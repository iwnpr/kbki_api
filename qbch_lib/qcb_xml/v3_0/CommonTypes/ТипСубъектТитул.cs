using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v3_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипСубъектТитул
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ФИО")]
        public List<ТипФИО> ФИО { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlElement(DataType = "date")]
        public System.DateTime ДатаРождения { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ДокументЛичности")]
        public List<ТипДУЛ> ДокументЛичности { get; set; } = new();

        /// <remarks/>
        public ТипИННФЛсПризнаком? ИНН { get; set; }

        /// <remarks/>
        public string ИнНомер { get; set; }

        /// <remarks/>
        public string СНИЛС { get; set; }
    }
}