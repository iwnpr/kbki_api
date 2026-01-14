using System.Collections.Generic;

namespace Domain.QBCHModels.qcb_xml.v2_0.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class Обязательства
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("БКИ")]
        public List<ОтветНаЗапросСведенийСведенияКБКИОбязательстваБКИ> БКИ { get; set; } = new();
    }
}