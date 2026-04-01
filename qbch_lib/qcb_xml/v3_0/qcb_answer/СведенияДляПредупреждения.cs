using QBCH_lib.qcb_xml.v3_0.CommonTypes;

namespace QBCH_lib.qcb_xml.v3_0.qcb_answer
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable]
    [System.Diagnostics.DebuggerStepThrough]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class СведенияДляПредупреждения
    {
        [System.Xml.Serialization.XmlElement("БКИ")]
        public System.Collections.Generic.List<СведенияДляПредупрежденияБКИ> БКИ { get; set; } = new();
    }

    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable]
    [System.Diagnostics.DebuggerStepThrough]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class СведенияДляПредупрежденияБКИ
    {
        [System.Xml.Serialization.XmlElement("ОбращениеОбязательство")]
        public System.Collections.Generic.List<ТипОбращениеОбязательство> ОбращениеОбязательство { get; set; } = new();

        [System.Xml.Serialization.XmlAttribute]
        public string ОГРН { get; set; }
    }

    public class СведенияДляПредупрежденияНеПредоставляются { }
    public class СведенийДляПредупрежденияНет { }
}
