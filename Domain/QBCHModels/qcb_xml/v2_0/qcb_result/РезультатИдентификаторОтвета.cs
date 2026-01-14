namespace Domain.QBCHModels.qcb_xml.v2_0.qcb_result
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class РезультатИдентификаторОтвета
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public DateTime ДатаЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public long ВремяГотовности { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnore()]
        public bool ВремяГотовностиSpecified { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlText()]
        public string Value { get; set; }
    }
}