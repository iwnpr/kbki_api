using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v2_0.CommonTypes
{
    /// <summary>
    /// 
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипИПБазовый
    {
        /// <summary>
        /// 
        /// </summary>
        [XmlElement("ИННИП")]
        public string ИНН { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [XmlElement("ОГРНИП")]
        public string ОГРН { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ТипФИО ФИО { get; set; }
    }
}
