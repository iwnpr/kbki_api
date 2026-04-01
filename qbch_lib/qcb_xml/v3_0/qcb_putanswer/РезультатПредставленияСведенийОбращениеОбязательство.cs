using QBCH_lib.qcb_xml.v3_0.Enums;
using QBCH_lib.qcb_xml.v3_0.qcb_result;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.qcb_putanswer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class РезультатПредставленияСведенийОбращениеОбязательство
    {
        /// <remarks/>
        [XmlElement("Ошибка", typeof(Ошибка))]
        [XmlElement("Успешно", typeof(object))]
        public object Item { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string УИД { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public СправочникСтадииРассмотрения СтадияРассмотрения { get; set; }

        /// <remarks/>
        [XmlIgnore()]
        public bool СтадияРассмотренияSpecified { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public СправочникОперации Операция { get; set; }
    }
}