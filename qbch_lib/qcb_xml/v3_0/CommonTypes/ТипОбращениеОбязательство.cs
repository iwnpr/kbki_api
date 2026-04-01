using QBCH_lib.qcb_xml.v3_0.Enums;

namespace QBCH_lib.qcb_xml.v3_0.CommonTypes
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable]
    [System.Diagnostics.DebuggerStepThrough]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ТипОбращениеОбязательство
    {
        public СправочникВидыИсточников КодИсточника { get; set; }

        public СправочникСтадииРассмотрения СтадияРассмотрения { get; set; }

        public System.DateTime ДатаСтадии { get; set; }

        public ТипСумма СуммаЗайма { get; set; }

        [System.Xml.Serialization.XmlElement("ПричинаОтказа")]
        public System.Collections.Generic.List<СправочникПричиныОтказа> ПричинаОтказа { get; set; } = new();

        [System.Xml.Serialization.XmlAttribute]
        public string УИД { get; set; }
    }
}