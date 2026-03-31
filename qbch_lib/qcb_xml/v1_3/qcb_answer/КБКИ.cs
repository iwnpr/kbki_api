using QBCH_lib.qcb_xml.v1_3.CommonTypes;

namespace QBCH_lib.qcb_xml.v1_3.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class КБКИ
    {
        /// <summary>
        /// 
        /// </summary>
        public ОбязательствНет? ОбязательствНет { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Обязательства? Обязательства { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Ошибка? Ошибка { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public СубъектНеНайден? СубъектНеНайден { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public System.DateTime ПоСостояниюНа { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ИдентификаторОтвета { get; set; }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class СубъектНеНайден
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ОбязательствНет
    {
    }
}