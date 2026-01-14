namespace Domain.QBCHModels.qcb_xml.v2_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипУсловиеЗапрета
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ДатаЗаявления { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ВремяЗаявления { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? НачалоДействия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlText()]
        public string? Value { get; set; }
    }
}