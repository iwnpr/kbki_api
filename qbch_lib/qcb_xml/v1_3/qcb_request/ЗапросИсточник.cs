using QBCH_lib.qcb_xml.v1_3.CommonTypes;

namespace QBCH_lib.qcb_xml.v1_3.qcb_request
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ЗапросИсточник
    {
        /// <summary>
        /// 
        /// </summary>
        public ТипИП? ИндивидуальныйПредприниматель { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ЗапросИсточникИностранноеЮЛ? ИностранноеЮЛ { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ТипИностранныйПредприниматель? ИностранныйПредприниматель { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ЗапросИсточникЮридическоеЛицо? ЮридическоеЛицо { get; set; }
    }
}