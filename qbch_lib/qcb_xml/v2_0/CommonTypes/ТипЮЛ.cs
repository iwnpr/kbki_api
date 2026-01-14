using QBCH_lib.qcb_xml.v2_0.Enums;

namespace QBCH_lib.qcb_xml.v2_0.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    public class ТипЮЛ
    {
        /// <remarks/>
        public string ИНН { get; set; }

        /// <remarks/>
        public string ОГРН { get; set; }

        /// <remarks/>
        public string ПолноеНаименование { get; set; }

        /// <remarks/>
        public string СокращенноеНаименование { get; set; }

        /// <remarks/>
        public object ИноеНаименование { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникВидыПользователя КодВидаПользователя { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ПризнакРегистрацииРФ { get; set; } = "1";
    }
}