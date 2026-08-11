using System;
using System.Collections.Generic;
using System.Linq;

namespace QBCH.Lib.qcb_xml.v3_0
{
    public partial class ТипОбращениеОбязательство
    {
        [System.Xml.Serialization.XmlIgnore]
        public АнтифродСтадияРассмотрения СтадияКакПеречисление
        {
            get => (АнтифродСтадияРассмотрения)СтадияРассмотрения;
            set => СтадияРассмотрения = (ushort)value;
        }

        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<АнтифродПричинаОтказа> ПричиныОтказаКакПеречисление =>
            (ПричинаОтказа ?? Array.Empty<ushort>())
            .Select(x => (АнтифродПричинаОтказа)x)
            .ToArray();

        public void УстановитьДатуСтадии(DateTime дата)
        {
            ДатаСтадии = DateTime.SpecifyKind(дата.Date, DateTimeKind.Unspecified);
        }

        public void УстановитьПричиныОтказа(IEnumerable<АнтифродПричинаОтказа> причины)
        {
            if (причины is null)
            {
                ПричинаОтказа = Array.Empty<ushort>();
                return;
            }

            ПричинаОтказа = причины.Select(x => (ushort)x).ToArray();
        }
    }
}
