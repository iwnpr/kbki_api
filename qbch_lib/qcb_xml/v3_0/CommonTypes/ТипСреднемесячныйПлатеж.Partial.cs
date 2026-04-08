using System;
using System.Globalization;

namespace QBCH.Lib.qcb_xml.v3_0
{
    public partial class ТипСреднемесячныйПлатеж
    {
        public long ЗначениеЦелое
        {
            get => long.Parse(Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            set => Value = value.ToString(CultureInfo.InvariantCulture);
        }

        public static ТипСреднемесячныйПлатеж Создать(long значение, string валюта, DateTime датаРасчета)
        {
            if (string.IsNullOrWhiteSpace(валюта))
            {
                throw new ArgumentException("Код валюты не должен быть пустым.", nameof(валюта));
            }

            return new ТипСреднемесячныйПлатеж
            {
                Валюта = валюта,
                ДатаРасчета = датаРасчета.Date,
                ЗначениеЦелое = значение,
            };
        }
    }
}