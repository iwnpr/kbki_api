using System.Collections.Generic;

namespace Domain.QBCHModels.qcb_xml.v2_0.qcb_answer
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ОтветНаЗапросСведенийСведенияКБКИОбязательстваБКИ
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Договор")]
        public List<ОтветНаЗапросСведенийСведенияКБКИОбязательстваБКИДоговор> Договор { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ОГРН { get; set; }
    }
}