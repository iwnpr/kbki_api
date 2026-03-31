using QBCH_lib.qcb_xml.v2_0.CommonTypes;
using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v2_0.qcb_answer
{
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class Сведения
    {

        /// <remarks/>
        public ТипСубъектТитул ТитульнаяЧасть { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("КБКИ")]
        public List<КБКИ> КБКИ { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public int ПорядковыйНомер { get; set; }
    }
}
