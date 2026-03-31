using QBCH_lib.CommonTypes.Api;
using System.Xml.Serialization;

namespace QBCH_lib.qcb_xml.v1_3.qcb_request
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class ЗапросСведенийОПлатежахАбонент
    {
        private АбонентИндивидуальныйПредприниматель? ИндивидуальныйПредпринимательfield;
        private АбонентИностранноеЛицо? ИностранноеЛицоfield;
        private АбонентЮридическоеЛицо? ЮридическоеЛицоfield;

        /// <summary>
        /// 
        /// </summary>
        public АбонентИндивидуальныйПредприниматель? ИндивидуальныйПредприниматель
        {
            get
            {
                return ИндивидуальныйПредпринимательfield;
            }
            set
            {
                FillRequsits(value?.ИНН, value?.ОГРН);
                ИндивидуальныйПредпринимательfield = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public АбонентИностранноеЛицо? ИностранноеЛицо
        {
            get
            {
                return ИностранноеЛицоfield;
            }
            set
            {
                FillRequsits(value?.ИНН, value?.ОГРН);
                ИностранноеЛицоfield = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public АбонентЮридическоеЛицо? ЮридическоеЛицо
        {
            get
            {
                return ЮридическоеЛицоfield;
            }
            set
            {
                FillRequsits(value?.ИНН, value?.ОГРН);
                ЮридическоеЛицоfield = value;
            }
        }

        /// <summary>
        /// Ревизиты абонента
        /// </summary>
        [XmlIgnore]
        public Requisites? Requisites { get; set; }

        /// <summary>
        /// Заполнить реквизиты абонента, чтоб каждый раз за ними не лазить в обход
        /// </summary>
        /// <param name="inn">ИНН</param>
        /// <param name="ogrn">ОГРН</param>
        private void FillRequsits(string? inn, string? ogrn)
        {
            Requisites = new()
            {
                inn = inn,
                ogrn = ogrn
            };
        }
    }
}