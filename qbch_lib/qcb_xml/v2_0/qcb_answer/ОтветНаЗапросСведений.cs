using QBCH_lib.qcb_xml.v2_0.Enums;
using QBCH_lib.qcb_xml.v2_0.qcb_result;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QBCH_lib.qcb_xml.v2_0.qcb_answer
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.9037.0")]
    [Serializable()]
    [System.Diagnostics.DebuggerStepThrough()]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public class ОтветНаЗапросСведений
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Сведения")]
        public List<Сведения> Сведения { get; set; } = new();

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string Версия { get; set; } = "2.0";

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ДатаЗапроса { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string ИдентификаторОтвета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public string? ОГРН { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникСпособыЗапроса ТипОтвета { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute()]
        public СправочникРежимыЗапроса РежимЗапроса { get; set; }

        /// <summary>
        /// Сформировать сведения о платежах с ошибкой, сформированной не на стороне КБКИ
        /// </summary>
        /// <param name="psrn">ОГРН КБКИ</param>
        /// <param name="errorCode">Код ошибки</param>
        /// <param name="errorValue">Значение ошибки</param>
        /// <returns>Сведения о платежах с ошибкой</returns>
        public static ОтветНаЗапросСведений CreateError(string psrn, string errorCode, string errorValue, int[] ПорядковыеНомера) //TODO для формирования ошибки
        {
            return new()
            {
                ТипОтвета = СправочникСпособыЗапроса.OurBureau,
                РежимЗапроса = СправочникРежимыЗапроса.Single,
                Версия = "2.0",
                ОГРН = psrn,
                Сведения = ПорядковыеНомера.Select(x => new Сведения()
                {
                    ПорядковыйНомер = x,
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
                }).ToList()
            };
        }

        /// <summary>
        /// Сформировать сведения о платежах с ошибкой, которая вернулась из КБКИ
        /// </summary>
        /// <param name="psrn">ОГРН</param>
        /// <param name="error">Ошибка</param>
        /// <returns>Сведения о платежах с ошибкой КБКИ</returns>
        public static ОтветНаЗапросСведений CreateError(string psrn, Ошибка error)
        {
            return new()
            {
                ТипОтвета = СправочникСпособыЗапроса.OurBureau,
                РежимЗапроса = СправочникРежимыЗапроса.Single,
                Версия = "2.0",
                ОГРН = psrn,
                Сведения = new()
                {
                    new()
                    {
                        КБКИ = new()
                        {
                            new()
                            {
                                ПоСостояниюНа = DateTime.Now,
                                ОГРН = psrn,
                                Ошибка = error
                            }
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
        public static ОтветНаЗапросСведений CreateError(string psrn, РезультатОшибка error, int[] ПорядковыеНомера)
        {
            return new()
            {
                ТипОтвета = СправочникСпособыЗапроса.OurBureau,
                РежимЗапроса = СправочникРежимыЗапроса.Single,
                Версия = "2.0",
                ОГРН = psrn,
                Сведения = ПорядковыеНомера.Select(x => new Сведения()
                {
                    ПорядковыйНомер = x,
                    КБКИ = new()
                    {
                        new()
                        {
                            ПоСостояниюНа = DateTime.Now,
                            ОГРН = psrn,
                            Ошибка = new (){ Код = error.Код, Value = error.Value}
                        }
                    }
                }).ToList()
                //new()
                //{
                //    new()
                //    {
                //        КБКИ = new()
                //        {
                //            new()
                //            {
                //                ПоСостояниюНа = DateTime.Now,
                //                ОГРН = psrn,
                //                Ошибка = new (){ Код = error.Код, Value = error.Value}
                //            }
                //        }
                //    }
                //}
            };
        }
    }
}