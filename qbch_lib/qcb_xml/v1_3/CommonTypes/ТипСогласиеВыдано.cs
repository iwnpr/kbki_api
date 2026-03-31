namespace QBCH_lib.qcb_xml.v1_3.CommonTypes
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class ТипСогласиеВыдано
    {
        /// <summary>
        /// ИП
        /// </summary>
        public ТипИПБазовый? ИндивидуальныйПредприниматель { get; set; }

        /// <summary>
        /// Иностранное ЮЛ
        /// </summary>
        public ТипИЮБазовый? ИностранноеЮЛ { get; set; }

        /// <summary>
        /// Иностранный ИП
        /// </summary>
        public ТипИностранныйПредпринимательБазовый? ИностранныйПредприниматель { get; set; }

        /// <summary>
        /// ЮЛ
        /// </summary>
        public ТипЮЛБазовый? ЮридическоеЛицо { get; set; }
    }
}