using System;

namespace QBCH.Lib.qcb_xml.v3_0
{
    public partial class РезультатПредставленияСведенийРезультатОбращениеОбязательство
    {
        public АнтифродСтадияРассмотрения СтадияКакПеречисление
        {
            get => (АнтифродСтадияРассмотрения)СтадияРассмотрения;
            set
            {
                СтадияРассмотрения = (ushort)value;
                СтадияРассмотренияSpecified = true;
            }
        }

        public bool УспешноВыполнено => !(Item is ТипОшибка);

        public void УстановитьУспех()
        {
            Item = new object();
        }

        public void УстановитьОшибку(int код, string описание)
        {
            Item = ТипОшибка.Создать(код, описание);
        }

        public ТипОшибка ПолучитьОшибку() => Item as ТипОшибка;
    }
}