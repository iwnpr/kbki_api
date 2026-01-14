namespace QBCH_lib.qcb_xml.v2_0.qcb_result
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public class Результат
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ИдентификаторОтвета", typeof(РезультатИдентификаторОтвета))]
        [System.Xml.Serialization.XmlElement("Ошибка", typeof(РезультатОшибка))]
        [System.Xml.Serialization.XmlElement("Успешно", typeof(РезультатУспешно))]
        public object РезультатДанные { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Версия { get; set; } = "2.0";

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ОГРН { get; set; }
    }
}