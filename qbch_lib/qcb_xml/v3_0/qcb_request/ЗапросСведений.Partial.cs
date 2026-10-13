namespace QBCH.Lib.qcb_xml.v3_0
{
    public partial class ЗапросСведений
    {
        /// <summary>
        /// Код сведений в значениях XML-контракта: 6, 7 или 8.
        /// </summary>
        public string GetInfoCode() => XmlEnumHelper.GetXmlEnumValue(КодСведений);

        /// <summary>
        /// Тип запроса в значениях XML-контракта: 1 — только в наше КБКИ, 2 — «одно окно».
        /// </summary>
        public string GetRequestTypeCode() => XmlEnumHelper.GetXmlEnumValue(ТипЗапроса);

        /// <summary>
        /// Режим запроса в значениях XML-контракта: 1 — одиночный, 2 — пакетный.
        /// </summary>
        public string GetRequestModeCode() => XmlEnumHelper.GetXmlEnumValue(РежимЗапроса);
    }
}