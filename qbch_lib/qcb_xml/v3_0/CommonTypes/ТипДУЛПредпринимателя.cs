using QBCH_lib.qcb_xml.v3_0.Enums;

namespace QBCH_lib.qcb_xml.v3_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипДУЛПредпринимателя
    {

        /// <remarks/>
        public string Серия { get; set; }

        /// <remarks/>
        public string Номер { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement(DataType = "date")]
        public System.DateTime ДатаВыдачи { get; set; }

        /// <remarks/>
        public string НаименованиеОргана { get; set; }

        /// <remarks/>
        public string КодПодразделения { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникДУЛ КодДУЛ { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string НаименованиеДУЛ { get; set; }
    }
}
