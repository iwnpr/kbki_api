using Domain.QBCHModels.qcb_xml.v2_0.CommonTypes;

namespace Domain.QBCHModels.qcb_xml.v2_0.qcb_answer
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
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
        public DateTime Представлено { get; set; }
    }
}