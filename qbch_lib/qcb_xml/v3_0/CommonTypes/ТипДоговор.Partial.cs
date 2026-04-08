using System;
using System.Globalization;

namespace QBCH.Lib.qcb_xml.v3_0
{
	public partial class ТипДоговор
	{
		public ТипПСК ПолучитьПСК()
		{
			if (string.IsNullOrWhiteSpace(ПСК))
			{
				return default;
			}

			if (ПСК == "-")
			{
				return ТипПСК.Отсутствует();
			}

			return ТипПСК.ИзЗначения(decimal.Parse(ПСК, NumberStyles.Float, CultureInfo.InvariantCulture));
		}

		public void УстановитьПСК(decimal значение)
		{
			ПСК = значение.ToString(CultureInfo.InvariantCulture);
		}

		public void УстановитьПСККакОтсутствующее()
		{
			ПСК = "-";
		}

		public void ОчиститьДатуПрекращения()
		{
			ДатаПрекращения = default;
			ДатаПрекращенияSpecified = false;
		}

		public void УстановитьДатуПрекращения(DateTime дата)
		{
			ДатаПрекращения = дата.Date;
			ДатаПрекращенияSpecified = true;
		}
	}
}