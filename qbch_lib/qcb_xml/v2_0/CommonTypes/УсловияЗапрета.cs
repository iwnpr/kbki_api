using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v2_0.CommonTypes
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class УсловияЗапрета
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Условие")]
        public List<ТипУсловиеЗапрета> Условие { get; set; } = new();
    }
}