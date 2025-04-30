# リリース方法

- Provider.nupkgの中のバージョンを更新する
- .orin3providerconfigの中のバージョンを更新する
- nuget.exeを使用してnupkgを作成する

> .\nuget.exe pack -NoDefaultExcludes .\GeoLibrary.ORiN3.Provider\src\Azure.Storage\Provider.nuspec -BasePath .\GeoLibrary.ORiN3.Provider\src\Azure.Storage\ -OutputDirectory .\nupkgs
