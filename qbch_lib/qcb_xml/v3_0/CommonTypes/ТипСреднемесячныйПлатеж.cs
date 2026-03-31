namespace QBCH_lib.qcb_xml.v3_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипСреднемесячныйПлатеж
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Валюта { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ДатаРасчета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlText()]
        public string? Value { get; set; }
    }
}