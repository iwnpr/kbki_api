using QBCH_lib.qcb_xml.v1_3.CommonTypes;
using QBCH_lib.qcb_xml.v1_3.Enums;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v1_3.qcb_putanswer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class РезультатПредставленияСведенийДоговор
    {
        /// <remarks/>
        [XmlElement("Ошибка", typeof(Ошибка))]
        [XmlElement("Успешно", typeof(object))]
        public object? Item { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string? УИД { get; set; }

        /// <remarks/>
        [XmlAttribute]
        public string? ДатаРасчета { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public СправочникОперации Операция { get; set; }
    }
}