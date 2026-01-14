using System.Collections.Generic;

namespace Domain.QBCHModels.qcb_xml.v2_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипСубъектТитул
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ФИО")]
        public List<ТипФИО> ФИО { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlElement(DataType = "date")]
        public DateTime ДатаРождения { get; set; }

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