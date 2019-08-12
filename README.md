[![Build Status](https://dev.azure.com/mtkorg/oss-projects/_apis/build/status/MaximTkachenko.legacy-db-dependency-builder?branchName=master)](https://dev.azure.com/mtkorg/oss-projects/_build/latest?definitionId=4&branchName=master)

# legacy-db-dependency-builder
```
-c, --config    Required. Path to json configuration file.

-n, --names     Required.  Comma separated root sql objects. Provide fragment of name or full name.

-t, --types     Filter for sql object type of root. Possible values: Tbl (table), Syn (synonym), Sp (stored procedure), Fun (function), V (view). All types by default.

-o, --output    Required. Directory for output files.

-e, --exact     Define how to search for roots. 1 means 'equals', 0 means 'contains'

--help          Display this help screen.

--version       Display version information.
```

```
dotnet DbDependencyBuilder.dll -c C:\code\repos\legacy-db-dependency-builder\sample\search-config.json -n Person -t Tbl -o C:\code\repos\legacy-db-dependency-builder\src\DbDependencyBuilder\bin\Debug\netcoreapp2.2
dotnet DbDependencyBuilder.dll -c C:\code\repos\legacy-db-dependency-builder\sample\search-config.json -n Person -t Tbl,Sp,V -o C:\code\repos\legacy-db-dependency-builder\src\DbDependencyBuilder\bin\Debug\netcoreapp2.2 -e 0
```

Build dependency map for particular SQL objects (tables etc.). It's useful especially in case of legacy databases with a lot of nested views, synonyms and compelx stored procedures.

1. [import](https://docs.microsoft.com/en-us/sql/ssdt/import-into-a-database-project?view=sql-server-2017) database schema to sql project
2. prepare json config in format ([sample](https://github.com/MaximTkachenko/legacy-db-dependency-builder/blob/master/sample/search-config.json)):
```
{
	"db": {
		"sql-database-key": "path-to-sql-database-project"
	},
	"etl": "path-to-folder-with-dtsx-files",
	"csharp": "path-to-folder-with-csharp-code"
}
```
3. run
```
dotnet DbDependencyBuilder.dll --config C:\pathto\search-config.json --names Person --types Tbl --output C:\output
```
4. check generated files: tree ([source](https://github.com/MaximTkachenko/legacy-db-dependency-builder/blob/master/sample/sample-output/1565599401_tree_Person.html), [preview](https://rawcdn.githack.com/MaximTkachenko/legacy-db-dependency-builder/59965a0302e11889bf317ed0e481d3f632296d7e/sample/sample-output/1565599401_tree_Person.html)) and graph ([source](https://github.com/MaximTkachenko/legacy-db-dependency-builder/blob/master/sample/sample-output/1565599401_graph_Person.html), [preview](https://rawcdn.githack.com/MaximTkachenko/legacy-db-dependency-builder/7289811f4b9430db5dcca0f1825264c1ea809cbf/sample/sample-output/1565599401_graph_Person.html)).
