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
## �T�v
TestBaseLib �� ORiN3 �v���o�C�_�̃e�X�g�ɕK�v�ȃ��[�e�B���e�B��x�[�X�N���X��񋟂��郉�C�u�����ł��BProviderTestFixture �N���X�𗘗p���邱�ƂŁAORiN3 �v���o�C�_�����̃e�X�g���̍\�z�Ɖ�̂��ȒP�ɍs���܂��B

## ��ȓ���
- ORiN3 �v���o�C�_�̃e�X�g���̏��������ȗ����B
- �P�̃e�X�g����ь����e�X�g�̈�т����\����񋟁B
- �e�X�g�N���X�ւ̈ˑ��������ɑΉ��B

---

## ProviderTestFixture �̎g����
ProviderTestFixture �N���X�́AORiN3 �v���o�C�_�̃e�X�g���̃��C�t�T�C�N�����Ǘ�����ėp�I�ȃt�B�N�X�`���N���X�ł��B�ȉ��Ɏg�p��������܂��B

---

## �e�X�g�Ɋւ���⑫����
ORiN3 �v���o�C�_�̃e�X�g���ɂ́A�s�v�Ȉˑ��֌W������邽�߂Ɉꕔ�̃`�F�b�N�𖳌��ɂ��邱�Ƃ��d�v�ł��B

�e�X�g�p�� RootObject �����ɂ����l�̐ݒ��K�p���邱�ƂŁA�e�X�g�̌��������}��܂��B

---

## ProviderTestFixture �̎g�p�菇

1. �e�X�g�N���X�� `ProviderTestFixture` ��ǉ�:
  - `IClassFixture<ProviderTestFixture<T>>` ���������Ă��������B������ T �͎��g�̃e�X�g�N���X�^�ł��B

2. �e�X�g����������:
  - `InitAsync<S>` ���\�b�h���g�p���A�w��� `RootObject` �����Ńe�X�g�������������܂��B`S` �̓e�X�g�Ώۂ� `RootObject` �^�ł��B

3. Root �I�u�W�F�N�g�ɃA�N�Z�X:
  - `_fixture.Root` ���g���ď��������ꂽ `RootObject` �ɃA�N�Z�X���A������s���܂��B

4. ���\�[�X�̃N���[���A�b�v:
  - `IDisposable` ���������A`CancellationTokenSource` �Ȃǂ̃��\�[�X�𖾎��I�ɉ�����܂��B

---

## API ���t�@�����X: `ProviderTestFixture`

### ���\�b�h

#### `InitAsync<S>(ITestOutputHelper outputHelper, CancellationToken token, uint timeoutIntervalMilliseconds = 1000000)`
- **����**: �w�肳�ꂽ `RootObject` �����Ńe�X�g�������������܂��B
- **����**:
  - `outputHelper`: �e�X�g�o�͗p�� `ITestOutputHelper` �C���X�^���X�B
  - `token`: �������𐧌䂷�� `CancellationToken`�B
  - `timeoutIntervalMilliseconds`: �i�C�Ӂj�������̃^�C���A�E�g�i�~���b�j�B
- **�^�p�����[�^**:
  - `S`: ���������� `RootObject` �̌^�B

#### `Dispose()`
- **����**: �t�B�N�X�`���Ŏg�p�������\�[�X��������܂��B

---

## ���C�Z���X
���̃v���W�F�N�g�� [MIT License](./../../LICENSE) �̂��ƂŃ��C�Z���X����Ă��܂��B

�g�p��: ProviderTestFixture ��p�����e�X�g�N���X
�i�� C#�R�[�h�͉p��ł��Q�Ƃ��Ă��������B�j