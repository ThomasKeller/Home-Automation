using System;
using HA.IOBroker.config;

namespace HA.IOBroker;

public class PropertyDetail
{
    private FieldType m_FieldType;
    private string m_DataType;

    public PropertyDetail(string propertyName, FieldType fieldType, bool historize = true)
    {
        PropertyName = propertyName;
        Historize = historize;
        DataType = fieldType.ToString();
        m_FieldType = fieldType;
    }

    public string PropertyName { get; set; }

    public bool Historize { get; set; }

    public string DataType
    {
        get { return m_DataType; }
        set { SetDataType(value); }
    }

    private void SetDataType(string value)
    {
        m_DataType = value;
        m_FieldType = Enum.TryParse<FieldType>(value, out var fieldType)
            ? fieldType
            : FieldType.Unknown;
    }

    public FieldType GetFieldType()
    {
        return m_FieldType;
    }
}