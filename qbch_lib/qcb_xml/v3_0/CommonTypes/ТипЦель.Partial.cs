using System;

namespace QBCH.Lib.qcb_xml.v3_0
{
    public partial class ТипЦель
    {
        public string ПолучитьКодЦели() => XmlEnumHelper.GetXmlEnumValue(КодЦели);

        public void УстановитьКодЦели(string xmlCode)
        {
            КодЦели = XmlEnumHelper.ParseXmlEnumValue<ТипЦельКодЦели>(xmlCode);
        }

        public static ТипЦель Создать(string xmlCode, string описание = null)
        {
            var цель = new ТипЦель();
            цель.УстановитьКодЦели(xmlCode);
            цель.Описание = описание;
            return цель;
        }
    }
}
