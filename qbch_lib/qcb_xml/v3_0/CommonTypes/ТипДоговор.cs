namespace QBCH_lib.qcb_xml.v3_0.CommonTypes
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable]
    [System.Diagnostics.DebuggerStepThrough]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ТипДоговор
    {
        public ТипСреднемесячныйПлатеж СреднемесячныйПлатеж { get; set; }

        public string ПСК { get; set; }

        [System.Xml.Serialization.XmlElement(DataType = "date")]
        public System.DateTime? ДатаПрекращения { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public bool ДатаПрекращенияSpecified => ДатаПрекращения.HasValue;

        [System.Xml.Serialization.XmlAttribute]
        public string УИД { get; set; }

        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public System.DateTime Представлено { get; set; }
    }
}