using Design.ORiN3.Provider.V1;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Root;
using ORiN3.Provider.Core.Abstract;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller;

internal class BaseStorageController : ControllerBase, ConnectionString.IHasNameAndKey
{
    private BaseStorageControllerOption? _analyzedResult;

    private class BaseStorageControllerOption
    {
        [JsonElementName("Account Name")]
        public OptionValue<string> AccountName { get; set; } = new();

        [SecretValue]
        [JsonElementName("Account Key")]
        public OptionValue<string> AccountKey { get; set; } = new();

        [Optional]
        [JsonElementName("Use Https")]
        public OptionValue<bool> UseHttps { get; set; } = new();

        [Optional]
        [JsonElementName("Endpoint Suffix")]
        public OptionValue<string> EndpointSuffix { get; set; } = new();
    }

    public string AccountName { private set; get; } = string.Empty;
    public string AccountKey { private set; get; } = string.Empty;
    public bool UseHttps { private set; get; } = true;
    public string EndpointSuffix { private set; get; } = "core.windows.net";

    protected override async Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        await base.OnInitializingAsync(option, needVersionCheck, fromParent, token);

        var optionManager = new OptionManager<BaseStorageControllerOption>(option);
        _analyzedResult = optionManager.Analyze();

        AccountName = _analyzedResult.AccountName.Value;
        AccountKey = _analyzedResult.AccountKey.Value;
#pragma warning disable IDE0075
        UseHttps = _analyzedResult.UseHttps.IsDefined ? _analyzedResult.UseHttps.Value : true;
#pragma warning restore IDE0075
        EndpointSuffix = _analyzedResult.EndpointSuffix.IsDefined ? _analyzedResult.EndpointSuffix.Value : "core.windows.net";
    }

    protected override Task<IFile> OnCreatingFileAsync(string name, string typeName, Type type, string option, object? _, CancellationToken token)
    {
        return base.OnCreatingFileAsync(name, typeName, type, option, new object[] { this, AzureRootObject.WorkingDir }, token);
    }
}
