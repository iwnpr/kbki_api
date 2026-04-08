using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace QBCH.Lib.qcb_xml.v3_0
{
    /// <summary>
    /// Технический адаптер между generated enum-моделями XSD и реальными строковыми значениями XML-контракта.
    /// Он нужен, чтобы не протаскивать в бизнес-код артефакты генератора вроде Item151,
    /// а работать с реальными кодами схемы, например 15.1.
    /// </summary>
    internal static class XmlEnumHelper
    {
        public static string GetXmlEnumValue<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            var member = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<XmlEnumAttribute>();
            return attr?.Name ?? value.ToString();
        }

        public static TEnum ParseXmlEnumValue<TEnum>(string xmlValue) where TEnum : struct, Enum
        {
            if (TryParseXmlEnumValue<TEnum>(xmlValue, out var value))
            {
                return value;
            }

            throw new ArgumentOutOfRangeException(
                nameof(xmlValue),
                xmlValue,
                $"Значение '{xmlValue}' не входит в {typeof(TEnum).Name}.");
        }

        public static bool TryParseXmlEnumValue<TEnum>(string xmlValue, out TEnum value) where TEnum : struct, Enum
        {
            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<XmlEnumAttribute>();

                if (string.Equals(attr?.Name, xmlValue, StringComparison.Ordinal) ||
                    string.Equals(field.Name, xmlValue, StringComparison.Ordinal))
                {
                    var rawValue = field.GetValue(null);
                    if (rawValue is TEnum typedValue)
                    {
                        value = typedValue;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }
    }
}