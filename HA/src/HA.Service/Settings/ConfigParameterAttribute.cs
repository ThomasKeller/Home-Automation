namespace HA.Service.Settings;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigParameterAttribute : Attribute
{
    private string _section;
    private string _name;
    private bool _required = false;

    /// <summary>
    /// You can decorate a property with this attribute to define
    /// from which section and which property a value should be read.
    /// </summary>
    /// <example>
    ///  [ConfigParameter("kostal", "url", Required = true)]
    ///  public string KostalUrl { get; set; } = "http://192.168.x.x";
    ///     
    ///  file: appsettings.json 
    ///  {
    ///    "kostal": {
    ///      "url": "http://192.168.111.4/",
    ///      ...
    ///     }
    ///   }
    /// 
    /// </example>
    /// <param name="section"></param>
    /// <param name="name"></param>
    public ConfigParameterAttribute(string? section, string? name)
    {
        _section = section ?? "";
        _name = name ?? "";
    }

    public virtual string Section
    {
        get { return _section; }
    }

    public virtual string Name
    {
        get { return _name; }
    }

    public virtual bool Required
    {
        get { return _required; }
        set { _required = value; }
    }

}