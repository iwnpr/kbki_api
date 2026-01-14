using QBCH_lib.qcb_xml.v2_0.CommonTypes;

namespace QBCH_lib.qcb_xml.v2_0.qcb_request
{
    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class ЗапросСведенийЗапросИсточник
    {
        /// <summary>
        /// 
        /// </summary>
        public ТипИП? ИндивидуальныйПредприниматель { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ЗапросСведенийЗапросИсточникИностранноеЮЛ? ИностранноеЮЛ { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ТипИностранныйПредприниматель? ИностранныйПредприниматель { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ЗапросСведенийЗапросИсточникЮридическоеЛицо? ЮридическоеЛицо { get; set; }
    }
}