using Colda.CommonUtilities.IO;
using Design.ORiN3.Provider.V1;
using FluentFTP;
using GeoLibrary.ORiN3.Provider.FTP.Client.O3Object.Root;
using ORiN3.Provider.Core;
using ORiN3.Provider.Core.Abstract;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.FTP.Client.O3Object.File
{
    internal class FtpClientFile : FileBase
    {
        private enum FtpClientFileMode
        {
            Auto,
            Ascii,
            Binary,
        }

        private class FtpClientFileOption
        {
            [JsonElementName("Path")]
            public OptionValue<string> Path { get; set; } = new();

            [Optional]
            [JsonElementName("Mode")]
            public OptionValue<FtpClientFileMode> Mode { get; set; } = new();
        }

        private string _tempFileName = Path.Combine(FtpClientRootObject.TempDir.FullName, Guid.NewGuid().ToString());
        private FileStream? _stream;
        private string _path = string.Empty;
        private FtpClientFileMode _mode = FtpClientFileMode.Auto;
        private Func<AsyncFtpClient>? _clientGetter;
        internal Func<AsyncFtpClient> FtpClientGetter
        {
            private get
            {
                return _clientGetter ?? throw new ArgumentNullException(nameof(_clientGetter));
            }
            set { _clientGetter = value; }
        }

        protected override async Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
        {
            await base.OnInitializingAsync(option, needVersionCheck, fromParent, token);

            var optionManager = new OptionManager<FtpClientFileOption>(option);
            var analyzedResult = optionManager.Analyze();

            if (analyzedResult.Path.IsDefined)
            {
                _path = analyzedResult.Path.Value;
            }

            if (analyzedResult.Mode.IsDefined)
            {
                _mode = analyzedResult.Mode.Value;
            }

            _clientGetter = (Func<AsyncFtpClient>?)fromParent;
        }

        protected override async Task OnOpeningAsync(JsonElement rootElement, IDictionary<string, object?> argument, CancellationToken token)
        {
            if (_stream != null)
            {
                throw new FtpClientProviderException(FtpClientProviderResultCode.AlreadyOpened, "FtpClientFile is already open.");
            }

            await base.OnOpeningAsync(rootElement, argument, token).ConfigureAwait(false);
            ORiN3ProviderLogger.LogTrace($"TempFile={_tempFileName}");
            await FtpClientGetter().DownloadFile(_tempFileName, _path, token: token).ConfigureAwait(false);
            var file = new FileInfo(_tempFileName);
            if (file.Exists)
            {
                _stream = new FileStream(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
        }

        protected override async Task OnClosingAsync(IDictionary<string, object?> argument, CancellationToken token)
        {
            if (_stream != null)
            {
                try
                {
                    _stream.Close();
                }
                catch (Exception e)
                {
                    ORiN3ProviderLogger.LogError(e, $"Failed to close stream.");
                }
                finally
                {
                    _stream = null;
                }

                var file = new FileInfo(_tempFileName);
                if (file.Exists)
                {
                    try
                    {
                        await file.SafeDeleteAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ORiN3ProviderLogger.LogError(e, $"Failed to delete file.");
                        // 再度Openされた際に読みだす必要がある
                        // パスがそのままだと書き込めない可能性があるのでパスを変更しておく
                        _tempFileName = Path.Combine(FtpClientRootObject.TempDir.FullName, Guid.NewGuid().ToString());
                    }
                }
            }
            await base.OnClosingAsync(argument, token).ConfigureAwait(false);
        }

        protected override Task<long> OnSeekingAsync(long offset, ORiN3FileSeekOrigin origin, CancellationToken token = default)
        {
            if (_stream == null)
            {
                throw new FtpClientProviderException(FtpClientProviderResultCode.FileNotOpened, $"File must be opened before calling {nameof(SeekAsync)}. [File name={Name}]");
            }

            var seekOrigin = origin switch
            {
                ORiN3FileSeekOrigin.Begin => SeekOrigin.Begin,
                ORiN3FileSeekOrigin.Current => SeekOrigin.Current,
                _ => SeekOrigin.End,
            };
            return Task.FromResult(_stream.Seek(offset, seekOrigin));
        }

        protected override async Task<int> OnReadingAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            if (_stream == null)
            {
                throw new FtpClientProviderException(FtpClientProviderResultCode.FileNotOpened, $"File must be opened before calling {nameof(ReadAsync)}. [File name={Name}]");
            }
            return await _stream.ReadAsync(buffer, token).ConfigureAwait(false);
        }

        protected override Task OnWritingAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
        {
            throw new FtpClientProviderException(FtpClientProviderResultCode.FileNotOpened, $"{nameof(FtpClientFile)} is read-only and cannot be written to. [File name={Name}]");
        }

        protected override Task<long> OnGettingLengthAsync(CancellationToken token = default)
        {
            if (_stream == null)
            {
                throw new FtpClientProviderException(FtpClientProviderResultCode.FileNotOpened, $"File must be opened before calling {nameof(GetLengthAsync)}. [File name={Name}]");
            }
            return Task.FromResult(_stream.Length);
        }

        protected override Task<bool> OnCanReadAsync(CancellationToken token = default)
        {
            return Task.FromResult(_stream != null);
        }

        protected override Task<bool> OnCanWriteAsync(CancellationToken token = default)
        {
            return Task.FromResult(false);
        }
    }
}
