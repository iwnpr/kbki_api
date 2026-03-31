using QBCH_lib.qcb_xml.v1_3.CommonTypes;
using System;

namespace QBCH_lib.qcb_xml.v1_3.qcb_result
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Результат
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ИдентификаторОтвета", typeof(РезультатИдентификаторОтвета))]
        [System.Xml.Serialization.XmlElement("Ошибка", typeof(Ошибка))]
        [System.Xml.Serialization.XmlElement("Успешно", typeof(РезультатУспешно))]
        public object? Item { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? Версия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }
    }
}