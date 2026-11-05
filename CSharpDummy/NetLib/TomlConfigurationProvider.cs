using Microsoft.Extensions.Configuration;
using System.Globalization;
using Tomlyn;
using Tomlyn.Model;

namespace CSharpDummy.NetLib;

public sealed class TomlConfigurationProvider : FileConfigurationProvider
{
    public TomlConfigurationProvider(FileConfigurationSource source) : base(source)
    {
    }

    public override void Load(Stream stream)
    {
        var toml = TomlSerializer.Deserialize<TomlTable>(stream)!;

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        
        Flatten(data, "", toml);
        
        Data = data;
    }
    
    void Flatten(Dictionary<string, string?> data, string key, object? value)
    {
        switch (value)
        {
            case TomlTable table:
            {
                if (table.Count == 0)
                    data.Add(key, null);

                foreach (var (k,v) in table)
                {
                    var prefix = key.Length == 0 ? k : $"{key}:{k}";
                    Flatten(data, prefix, v);
                }
                break;
            }
            
            case TomlTableArray lstTable: 
            {
                if (lstTable.Count == 0)
                    data.Add(key, null);

                foreach (var (index, table) in lstTable.Index())
                {
                    Flatten(data, $"{key}:{index}", table);
                }
                break;
            }

            case TomlArray tomlArray:
            {
                if (tomlArray.Count == 0)
                    data.Add(key, null);

                foreach (var (k, v) in tomlArray.Index())
                {
                    Flatten(data, $"{key}:{k}", v);
                }
                break;
            }

            default:
            {
                data.Add(key, Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            }
        }
    }
}

public sealed class TomlConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new TomlConfigurationProvider(this);
    }
}

public static class TomlConfigurationExtensions
{
    public static IConfigurationBuilder AddTomlFile(this IConfigurationBuilder builder, string path,
        bool optional = false, bool reloadOnChange = false)
    {
        return builder.Add(
            new TomlConfigurationSource { Path = path, Optional = optional, ReloadOnChange = reloadOnChange }
        );
    }
}

