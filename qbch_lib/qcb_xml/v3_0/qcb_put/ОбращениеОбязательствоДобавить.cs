using QBCH_lib.qcb_xml.v3_0.CommonTypes;
using QBCH_lib.qcb_xml.v3_0.Enums;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v3_0.qcb_put
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class ОбращениеОбязательствоДобавить
    {
        /// <remarks/>
        public СправочникВидыИсточников КодИсточника { get; set; }

        /// <remarks/>
        public СправочникСтадииРассмотрения СтадияРассмотрения { get; set; }

        /// <remarks/>
        public DateTime ДатаСтадии { get; set; }

        /// <remarks/>
        public ТипСумма СуммаЗайма { get; set; }

        /// <remarks/>
        [XmlElement("ПричинаОтказа")]
        public List<СправочникПричиныОтказа> ПричинаОтказа { get; set; } = new();

        /// <remarks/>
        [XmlAttribute()]
        public string УИД { get; set; }
    }
}