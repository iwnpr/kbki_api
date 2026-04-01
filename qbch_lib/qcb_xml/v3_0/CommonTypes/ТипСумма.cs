using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class ТипСумма
    {
        /// <remarks/>
        [XmlAttribute()]
        public string Валюта { get; set; }

        /// <remarks/>
        [XmlText()]
        public double Value { get; set; }
    }
}
