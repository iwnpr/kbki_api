using System;
using System.Linq;

namespace QBCH.Lib.qcb_xml.v3_0
{
    public partial class ОтветНаЗапросСведенийСведенияКБКИ
    {
        private void SetSingle(object value, ItemsChoiceType choice)
        {
            Items = new[] { value };
            ItemsElementName = new[] { choice };
        }

        private void AddChoice(object value, ItemsChoiceType choice)
        {
            var items = (Items ?? Array.Empty<object>()).ToList();
            var names = (ItemsElementName ?? Array.Empty<ItemsChoiceType>()).ToList();
            items.Add(value);
            names.Add(choice);
            Items = items.ToArray();
            ItemsElementName = names.ToArray();
        }

        public void ОчиститьСекции()
        {
            Items = Array.Empty<object>();
            ItemsElementName = Array.Empty<ItemsChoiceType>();
        }

        public void УстановитьОшибку(int код, string описание)
        {
            SetSingle(new ТипОшибка { Код = код.ToString(), Value = описание ?? string.Empty }, ItemsChoiceType.Ошибка);
        }

        public void ПометитьКакСубъектНеНайден()
        {
            SetSingle(new object(), ItemsChoiceType.СубъектНеНайден);
        }

        public void ДобавитьОбязательства(ОтветНаЗапросСведенийСведенияКБКИОбязательства обязательства)
            => AddChoice(обязательства ?? throw new ArgumentNullException(nameof(обязательства)), ItemsChoiceType.Обязательства);

        public void ДобавитьПризнакОтсутствияОбязательств()
            => AddChoice(new object(), ItemsChoiceType.ОбязательствНет);

        public void ДобавитьУсловияЗапрета(ОтветНаЗапросСведенийСведенияКБКИУсловияЗапрета условия)
            => AddChoice(условия ?? throw new ArgumentNullException(nameof(условия)), ItemsChoiceType.УсловияЗапрета);

        public void ДобавитьПризнакОтсутствияСведенийОЗапрете()
            => AddChoice(new object(), ItemsChoiceType.СведенийОЗапретеНет);

        public void ДобавитьПризнакНепредоставленияСведенийОЗапрете()
            => AddChoice(new object(), ItemsChoiceType.СведенияОЗапретеНеПредоставляются);

        public void ДобавитьСведенияДляПредупреждения(ОтветНаЗапросСведенийСведенияКБКИСведенияДляПредупреждения сведения)
            => AddChoice(сведения ?? throw new ArgumentNullException(nameof(сведения)), ItemsChoiceType.СведенияДляПредупреждения);

        public void ДобавитьПризнакОтсутствияАнтифродСведений()
            => AddChoice(new object(), ItemsChoiceType.СведенийДляПредупрежденияНет);

        public void ДобавитьПризнакНепредоставленияАнтифродСведений()
            => AddChoice(new object(), ItemsChoiceType.СведенияДляПредупрежденияНеПредоставляются);

        public bool ЕстьОшибка => ItemsElementName?.Contains(ItemsChoiceType.Ошибка) == true;

        public ТипОшибка ПолучитьОшибку()
        {
            if (Items == null || ItemsElementName == null)
            {
                return null;
            }

            for (var i = 0; i < Math.Min(Items.Length, ItemsElementName.Length); i++)
            {
                if (ItemsElementName[i] == ItemsChoiceType.Ошибка)
                {
                    return Items[i] as ТипОшибка;
                }
            }

            return null;
        }
    }
}
