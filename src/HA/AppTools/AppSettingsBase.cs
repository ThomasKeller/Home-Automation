using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;

namespace HA.AppTools;

public abstract class AppSettingsBase
{
    protected readonly ValueConverter valueConverter = new ValueConverter();

    /// <summary>
    /// Contructor
    /// </summary>
    /// <param name="logger">null: disabled logging | not nul: enable logging</param>
    protected AppSettingsBase()
    {
    }

    /// <summary>
    /// Returns all properties of the derived class decorated with the attribute 'EnvParameter'
    /// </summary>
    /// <returns>list of all properties with the attribute 'EnvParameter'</returns>
    public IEnumerable<(string Name, string TypeName)> GetEnvironmentVariables()
    {
        return PropertiesWith<EnvParameterAttribute>().Select(p =>
            (p.Attr.Name, Type.GetTypeCode(p.Prop.PropertyType).ToString()));
    }

    /// <summary>
    /// Returns all properties of the derived class decorated with the attribute 'ConfigParameter'
    /// </summary>
    /// <returns>list of all properties with the attribute 'ConfigParameter'</returns>
    public IEnumerable<(string Section, string Name, string TypeName)> GetConfigParamenters()
    {
        return PropertiesWith<ConfigParameterAttribute>().Select(p =>
            (p.Attr.Section, p.Attr.Name, Type.GetTypeCode(p.Prop.PropertyType).ToString()));
    }

    /// <summary>
    /// Check if a property value of the derived class is required and throw an exceptions 
    /// if no value was provided.
    /// </summary>
    /// <exception cref="ArgumentNullException">throw an exception if required parameter was not provided </exception>
    public void CheckSettings()
    {
        var sb = new StringBuilder();
        PropertiesWith<ConfigParameterAttribute>().ToList().ForEach(p => {
            if (p.Attr.Required && p.Prop.GetValue(this) == null)
            {
                var envAttrs = p.Prop.GetCustomAttributes(typeof(EnvParameterAttribute), true);
                var envVariableName = (envAttrs?.Length > 0)
                    ? ((EnvParameterAttribute)envAttrs.First()).Name
                    : string.Empty;
                sb.AppendLine($"Parameter was not provided: in file: '{p.Attr.Section}:{p.Attr.Name}' or  Enviroment: '{envVariableName}'");
            }
        });
        if (sb.Length > 0)
        {
            throw new ArgumentException($"Settings required\n: {sb.ToString()}");
        }
    }

    /// <summary>
    /// Checks for all properties of the derived class decorated with the attribute 'EnvParameter'
    /// if there is a environment variable available and assign the environment value to the property.
    /// </summary>
    protected void ReadEnvironmentVariables()
    {
        PropertiesWith<EnvParameterAttribute>().ToList().ForEach(p => {
            var name = p.Attr.Name;
            var typeCode = Type.GetTypeCode(p.Prop.PropertyType);
            var variable = GetEnvironmentVariable(name, typeCode);
            if (variable.available)
            {
                Console.WriteLine("Use environment variable: {0} | {1} | {2}",
                    name, variable.value, typeCode.ToString());
                p.Prop.SetValue(this, variable.value);
            }
        });
    }

    /// <summary>
    /// Checks for all properties of the derived class decorated with the attribute 'ConfigParameter'
    /// if there is a property in the appsettings.json file available and assign the value to the property.
    /// </summary>
    protected void ReadAppConfigFile(IConfiguration configuration)
    {
        if (configuration == null)
        {
            Console.WriteLine("No configuration file provides");
            return;
        }
        PropertiesWith<ConfigParameterAttribute>().ToList().ForEach(p => {
            var typeCode = Type.GetTypeCode(p.Prop.PropertyType);
            var sectionName = $"{p.Attr.Section}:{p.Attr.Name}";
            var section = configuration.GetSection(sectionName);
            if (section.Exists())
            {
                var value = section.Value;
                if (value != null)
                {
                    var propValue = valueConverter.ConvertTo(value, typeCode);
                    Console.WriteLine("Read variable from config file : {0}:{1} | {2} | {3}",
                         p.Attr.Section, p.Attr.Name, propValue, typeCode.ToString());
                    p.Prop.SetValue(this, propValue);
                }
                else
                    Console.WriteLine("Property value not found: {0}", sectionName);
            }
            else
                Console.WriteLine("Entry not found: {0}", sectionName, p.Attr.Section);
        });
    }

    private (bool available, object? value) GetEnvironmentVariable(string name, TypeCode typeCode)
    {
        var variable = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        if (variable != null)
        {
            var value = valueConverter.ConvertTo(variable, typeCode);
            if (value != null)
                return (true, value);
        }
        return (false, null);
    }

    private IEnumerable<(PropertyInfo Prop, T Attr)> PropertiesWith<T>() where T : Attribute
    {
        var properties = GetType().GetProperties();
        foreach (var property in properties)
        {
            var customAttributes = property.GetCustomAttributes(typeof(T), true);
            if (customAttributes?.Length > 0)
            {
                var parameter = customAttributes.First() as T;
                if (parameter != null)
                    yield return (property, parameter);
            }
        }
        yield break;
    }
}
