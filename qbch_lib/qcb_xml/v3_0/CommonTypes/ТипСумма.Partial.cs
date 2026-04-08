using System;
using System.Globalization;

namespace QBCH.Lib.qcb_xml.v3_0
{
    public partial class ТипСумма
    {
        public decimal ЗначениеДесятичное
        {
            get => Convert.ToDecimal(Value, CultureInfo.InvariantCulture);
            set => Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        public static ТипСумма Создать(decimal значение, string валюта)
        {
            if (string.IsNullOrWhiteSpace(валюта))
            {
                throw new ArgumentException("Код валюты не должен быть пустым.", nameof(валюта));
            }

            return new ТипСумма
            {
                Валюта = валюта,
                ЗначениеДесятичное = значение,
            };
        }
    }
}

