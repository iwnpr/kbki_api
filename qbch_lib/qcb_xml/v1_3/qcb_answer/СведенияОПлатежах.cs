using QBCH_lib.qcb_xml.v1_3.CommonTypes;
using System;
using System.Collections.Generic;

namespace QBCH_lib.qcb_xml.v1_3.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public partial class СведенияОПлатежах
    {
        /// <remarks/>
        public ТипСубъектТитул? ТитульнаяЧасть { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("КБКИ")]
        public List<КБКИ>? КБКИ { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? Версия { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ИдентификаторЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ИдентификаторОтвета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ТипОтвета { get; set; }

        /// <summary>
        /// Сформировать сведения о платежах с ошибкой, сформированной не на стороне КБКИ
        /// </summary>
        /// <param name="psrn">ОГРН КБКИ</param>
        /// <param name="errorCode">Код ошибки</param>
        /// <param name="errorValue">Значение ошибки</param>
        /// <returns>Сведения о платежах с ошибкой</returns>
        public static СведенияОПлатежах CreateError(string psrn, string errorCode, string errorValue)
        {
            return new()
            {
                Версия = "1.2",
                ОГРН = psrn,
                КБКИ = new()
                {
                    new()
                    {
                        ПоСостояниюНа = DateTime.Now,
                        ОГРН = psrn,
                        Ошибка = new()
                        {
                            Код = errorCode,
                            Value = errorValue,
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Сформировать сведения о платежах с ошибкой, которая вернулась из КБКИ
        /// </summary>
        /// <param name="psrn">ОГРН</param>
        /// <param name="error">Ошибка</param>
        /// <returns>Сведения о платежах с ошибкой КБКИ</returns>
        public static СведенияОПлатежах CreateError(string psrn, Ошибка error)
        {
            return new()
            {
                Версия = "1.2",
                ОГРН = psrn,
                КБКИ = new()
                {
                    new()
                    {
                        ПоСостояниюНа = DateTime.Now,
                        ОГРН = psrn,
                        Ошибка = error
                    }
                }
            };
        }
    }
}