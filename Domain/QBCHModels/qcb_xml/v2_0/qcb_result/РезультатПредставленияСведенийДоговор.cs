using Domain.QBCHModels.qcb_xml.v2_0.Enums;

namespace Domain.QBCHModels.qcb_xml.v2_0.qcb_result
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class РезультатПредставленияСведенийДоговор
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Ошибка", typeof(РезультатПредставленияСведенийДоговорОшибка))]
        [System.Xml.Serialization.XmlElement("Успешно", typeof(object))]
        public object Item { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string УИД { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ДатаРасчета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnore()]
        public bool ДатаРасчетаSpecified { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникОперации Операция { get; set; }
    }
}