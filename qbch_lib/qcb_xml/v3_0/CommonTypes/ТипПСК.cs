using System;

namespace QBCH.Lib.qcb_xml.v3_0
{
    /// <summary>
    /// Типобезопасное представление значения ПСК из XSD 3.0.
    /// По схеме поле допускает либо число, либо специальный маркер отсутствия значения '-'.
    /// </summary>
    public readonly struct ТипПСК
    {
        public ТипПСК(decimal? значение, bool значениеОтсутствует)
        {
            if (значение.HasValue && значениеОтсутствует)
            {
                throw new ArgumentException("Нельзя одновременно задать числовое значение ПСК и маркер отсутствия значения.");
            }

            Значение = значение;
            ЗначениеОтсутствует = значениеОтсутствует;
        }

        public decimal? Значение { get; }

        public bool ЗначениеОтсутствует { get; }

        public bool ЕстьЧисловоеЗначение => Значение.HasValue;

        public static ТипПСК Отсутствует() => new ТипПСК(null, true);

        public static ТипПСК ИзЗначения(decimal значение) => new ТипПСК(значение, false);
    }
}

