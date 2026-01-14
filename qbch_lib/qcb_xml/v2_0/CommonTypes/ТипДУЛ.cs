using QBCH_lib.qcb_xml.v2_0.Enums;

namespace QBCH_lib.qcb_xml.v2_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипДУЛ
    {

        /// <remarks/>
        public string Серия { get; set; }

        /// <remarks/>
        public string Номер { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement(DataType = "date")]
        public System.DateTime ДатаВыдачи { get; set; }

        /// <remarks/>
        public string Гражданство { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникДУЛ КодДУЛ { get; set; }
    }
}
