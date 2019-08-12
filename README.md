[![Build Status](https://dev.azure.com/mtkorg/oss-projects/_apis/build/status/MaximTkachenko.legacy-db-dependency-builder?branchName=master)](https://dev.azure.com/mtkorg/oss-projects/_build/latest?definitionId=4&branchName=master)

# legacy-db-dependency-builder

```
dotnet DbDependencyBuilder.dll --help
dotnet DbDependencyBuilder.dll -c C:\code\repos\legacy-db-dependency-builder\sample\search-config.json -n Person -t Tbl -o C:\code\repos\legacy-db-dependency-builder\src\DbDependencyBuilder\bin\Debug\netcoreapp2.2
dotnet DbDependencyBuilder.dll -c C:\code\repos\legacy-db-dependency-builder\sample\search-config.json -n Person -t Tbl,Sp,V -o C:\code\repos\legacy-db-dependency-builder\src\DbDependencyBuilder\bin\Debug\netcoreapp2.2 -e 0
```

Build dependency map for particular SQL objects (tables etc.). It's useful especially in case of legacy databases with a lot of nested views, synonyms and compelx stored procedures.

1. [import](https://docs.microsoft.com/en-us/sql/ssdt/import-into-a-database-project?view=sql-server-2017) database schema to sql project
2. prepare json config in format:
```

```
	- db
	- csharp
	- etl
3. install depdbbuilder as a global and run
```
dep-db-builder --cconfig C:\pathto\search-config.json --names Person --types Tbl --output C:\output
```
