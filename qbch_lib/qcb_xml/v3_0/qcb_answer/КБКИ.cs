using QBCH_lib.qcb_xml.v3_0.CommonTypes;
using QBCH_lib.qcb_xml.v3_0.qcb_result;

namespace QBCH_lib.qcb_xml.v3_0.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public class КБКИ
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
        public СведенийОЗапретеНет? СведенийОЗапретеНет { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public СведенияОЗапретеНеПредоставляются? СведенияОЗапретеНеПредоставляются { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public СубъектНеНайден? СубъектНеНайден { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public УсловияЗапрета? УсловияЗапрета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public System.DateTime ПоСостояниюНа { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторОтвета { get; set; }
    }
}