namespace HA.Service.Settings;

[AttributeUsage(AttributeTargets.Property)]
public class EnvParameterAttribute : Attribute
{
    private string _name;

    public EnvParameterAttribute(string? name)
    {
        _name = name ?? "";
    }

    public virtual string Name
    {
        get { return _name; }
    }
}