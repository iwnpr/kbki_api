using QBCH_lib.qcb_xml.v3_0.Enums;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class ОбращениеОбязательствоУдалить
    {
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
        public string? Причина { get; set; }
    }
}