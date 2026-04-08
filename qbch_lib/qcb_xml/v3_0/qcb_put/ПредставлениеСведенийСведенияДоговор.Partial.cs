using System;

namespace QBCH.Lib.qcb_xml.v3_0
{
    public partial class ПредставлениеСведенийСведенияДоговор
    {
        public void УстановитьДобавление(ТипДоговор договор)
        {
            Item = договор ?? throw new ArgumentNullException(nameof(договор));
        }

        public void УстановитьУдаление(ПредставлениеСведенийСведенияДоговорУдалить удаление)
        {
            Item = удаление ?? throw new ArgumentNullException(nameof(удаление));
        }

        public ТипДоговор ПолучитьДобавление() => Item as ТипДоговор;

        public ПредставлениеСведенийСведенияДоговорУдалить ПолучитьУдаление() => Item as ПредставлениеСведенийСведенияДоговорУдалить;

        public bool ЭтоДобавление => Item is ТипДоговор;

        public bool ЭтоУдаление => Item is ПредставлениеСведенийСведенияДоговорУдалить;
    }
}
