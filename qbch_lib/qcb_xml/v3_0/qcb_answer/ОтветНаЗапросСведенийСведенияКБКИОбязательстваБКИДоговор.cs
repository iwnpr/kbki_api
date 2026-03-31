using QBCH_lib.qcb_xml.v3_0.CommonTypes;

namespace QBCH_lib.qcb_xml.v3_0.qcb_answer
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ОтветНаЗапросСведенийСведенияКБКИОбязательстваБКИДоговор
    {

        /// <remarks/>
        public ТипСреднемесячныйПлатеж СреднемесячныйПлатеж { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string УИД { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute(DataType = "date")]
        public System.DateTime Представлено { get; set; }
    }
}