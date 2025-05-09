using Microsoft.AspNetCore.DataProtection.KeyManagement;
using ORiN3.Provider.Core.OptionAnalyzer;

namespace GeoLibrary.ORiN3.Provider.BaseLib;

public class ArgumentHelper
{
    private class ArgumentHelperException : ApplicationException
    {
        public ArgumentHelperException(string message)
            : base(message) 
        {
        }
    }

    public static T GetArgument<T>(OptionValue<T> option, string name)
    {
        if (option.IsDefined)
        {
            return option.Value;
        }
        throw new ArgumentHelperException($"Invalid argument. \"{name}\" does not exist.");
    }

    public static T GetArgumentOrDefault<T>(OptionValue<T> option, string name, T defaultValue)
    {
        try
        {
            return GetArgument<T>(option, name);
        }
        catch (ArgumentHelperException)
        {
            return defaultValue;
        }
    }

    public static T GetArgument<T>(IDictionary<string, object?> argument, string key)
    {
        if (!argument.TryGetValue(key, out var value))
        {
            throw new ArgumentHelperException($"Invalid argument. \"{key}\" does not exist.");
        }
        else if (value is not T)
        {
            throw new ArgumentHelperException($"Invalid argument. \"{key}\" is not {typeof(T).Name}.");
        }
        return (T)value;
    }

    public static T GetArgumentOrDefault<T>(IDictionary<string, object?> argument, string key, T defaultValue)
    {
        try
        {
            return GetArgument<T>(argument, key);
        }
        catch (ArgumentHelperException)
        {
            return defaultValue;
        }
    }
}