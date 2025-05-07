# GeoLibrary ORiN3 Provider TestBaseLib

## Overview
`TestBaseLib` is a library that provides utilities and base classes for testing ORiN3 providers. It includes the `ProviderTestFixture` class, which simplifies the setup and teardown of test environments for ORiN3 provider implementations.

## Key Features
- Simplifies the initialization of ORiN3 provider test environments.
- Provides a consistent structure for writing unit and integration tests.
- Supports dependency injection for test classes.

---

## ProviderTestFixture Usage

The `ProviderTestFixture` class is a generic fixture class designed to manage the lifecycle of ORiN3 provider test environments. Below is an example of how to use it in a test class.

---

## Additional Notes on Testing

When testing ORiN3 providers, it is important to disable certain checks to avoid unnecessary dependencies during the test process.

Ensure that similar configurations are applied in your test `RootObject` implementations to streamline the testing process.

---

## How to Use `ProviderTestFixture`

1. **Add `ProviderTestFixture` to Your Test Class**:
  - Implement the `IClassFixture<ProviderTestFixture<T>>` interface in your test class, where `T` is the test class itself.

2. **Initialize the Test Environment**:
  - Use the `InitAsync<S>` method to initialize the test environment with a specific `RootObject` implementation. Replace `S` with the type of the `RootObject` you want to test.

3. **Access the Root Object**:
  - Use `_fixture.Root` to access the initialized `RootObject` and perform operations.

4. **Clean Up Resources**:
  - Implement the `IDisposable` interface in your test class to clean up resources, such as `CancellationTokenSource`.

---

## API Reference: `ProviderTestFixture`

### Methods

#### `InitAsync<S>(ITestOutputHelper outputHelper, CancellationToken token, uint timeoutIntervalMilliseconds = 1000000)`
- **Description**: Initializes the test environment with the specified `RootObject` implementation.
- **Parameters**:
  - `outputHelper`: The `ITestOutputHelper` instance for logging test output.
  - `token`: A `CancellationToken` to manage the initialization process.
  - `timeoutIntervalMilliseconds`: (Optional) Timeout interval for initialization.
- **Type Parameter**:
  - `S`: The type of the `RootObject` to initialize.

#### `Dispose()`
- **Description**: Cleans up resources used by the fixture.

---

## License
This project is licensed under the [MIT License](./../../LICENSE).

### Example: Using `ProviderTestFixture` in a Test Class

``` csharp
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Xunit.Abstractions;

internal class SomeProviderRootForTest : SomeProviderRoot
{
    static SomeProviderRootForTest()
    {
        AuthorityCheckEnabled = false;
    }
}

public class SomeProviderTest : IClassFixture<ProviderTestFixture<SomeProviderTest>>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderTestFixture<SomeProviderTest> _fixture;
    private readonly CancellationTokenSource _tokenSource = new(10000);

    public SomeProviderTest(ProviderTestFixture<SomeProviderTest> fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;

        // Initialize the test environment with a specific RootObject implementation
        _fixture.InitAsync<SomeProviderRootForTest>(_output, _tokenSource.Token).Wait();
    }

    public void Dispose()
    {
        // Dispose of resources
        _tokenSource.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExampleTest()
    {
        // Arrange
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "ExampleController",
            typeName: "ExampleNamespace.ExampleController, ExampleAssembly",
            option: "{\"@Version\":\"1.0.0\",\"ExampleOption\":\"value\"}",
            token: _tokenSource.Token);

        // Act
        var result = await controller.ExecuteAsync("ExampleCommand", new Dictionary<string, object?>(), _tokenSource.Token);

        // Assert
        Assert.NotNull(result);
    }
}
```


---
## 概要
TestBaseLib は ORiN3 プロバイダのテストに必要なユーティリティやベースクラスを提供するライブラリです。ProviderTestFixture クラスを利用することで、ORiN3 プロバイダ実装のテスト環境の構築と解体を簡単に行えます。

## 主な特徴
- ORiN3 プロバイダのテスト環境の初期化を簡略化。
- 単体テストおよび結合テストの一貫した構造を提供。
- テストクラスへの依存性注入に対応。

---

## ProviderTestFixture の使い方
ProviderTestFixture クラスは、ORiN3 プロバイダのテスト環境のライフサイクルを管理する汎用的なフィクスチャクラスです。以下に使用例を示します。

---

## テストに関する補足事項
ORiN3 プロバイダのテスト時には、不要な依存関係を避けるために一部のチェックを無効にすることが重要です。

テスト用の RootObject 実装にも同様の設定を適用することで、テストの効率化が図れます。

---

## ProviderTestFixture の使用手順

1. テストクラスに `ProviderTestFixture` を追加:
  - `IClassFixture<ProviderTestFixture<T>>` を実装してください。ここで T は自身のテストクラス型です。

2. テスト環境を初期化:
  - `InitAsync<S>` メソッドを使用し、指定の `RootObject` 実装でテスト環境を初期化します。`S` はテスト対象の `RootObject` 型です。

3. Root オブジェクトにアクセス:
  - `_fixture.Root` を使って初期化された `RootObject` にアクセスし、操作を行います。

4. リソースのクリーンアップ:
  - `IDisposable` を実装し、`CancellationTokenSource` などのリソースを明示的に解放します。

---

## API リファレンス: `ProviderTestFixture`

### メソッド

#### `InitAsync<S>(ITestOutputHelper outputHelper, CancellationToken token, uint timeoutIntervalMilliseconds = 1000000)`
- **説明**: 指定された `RootObject` 実装でテスト環境を初期化します。
- **引数**:
  - `outputHelper`: テスト出力用の `ITestOutputHelper` インスタンス。
  - `token`: 初期化を制御する `CancellationToken`。
  - `timeoutIntervalMilliseconds`: （任意）初期化のタイムアウト（ミリ秒）。
- **型パラメータ**:
  - `S`: 初期化する `RootObject` の型。

#### `Dispose()`
- **説明**: フィクスチャで使用したリソースを解放します。

---

## ライセンス
このプロジェクトは [MIT License](./../../LICENSE) のもとでライセンスされています。

使用例: ProviderTestFixture を用いたテストクラス
（※ C#コードは英語版を参照してください。）