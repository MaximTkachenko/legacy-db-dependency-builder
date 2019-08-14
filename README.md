[![Build Status](https://dev.azure.com/mtkorg/oss-projects/_apis/build/status/MaximTkachenko.legacy-db-dependency-builder?branchName=master)](https://dev.azure.com/mtkorg/oss-projects/_build/latest?definitionId=4&branchName=master)

# legacy-db-dependency-builder

Build dependency map for particular SQL objects (tables etc.). It's useful especially in case of legacy systems with a lot of nested views, synonyms and compelx stored procedures.

```
-c, --config    Required. Path to json configuration file.

-n, --names     Required.  Comma separated root sql objects. Provide fragment of name or full name.

-t, --types     Filter for sql object type of root. Possible values: Tbl (table), Syn (synonym), Sp (stored procedure), Fun (function), V (view). All types by default.

-o, --output    Required. Directory for output files.

-e, --exact     Define how to search for roots. 1 means 'equals', 0 means 'contains'. 'Equals' by default.

--help          Display this help screen.
```

1. [import](https://docs.microsoft.com/en-us/sql/ssdt/import-into-a-database-project?view=sql-server-2017) database schema to sql project
2. prepare json config in format ([sample](https://github.com/MaximTkachenko/legacy-db-dependency-builder/blob/master/sample/search-config.json)):
```
{
	"db": {
		"sql-database-key-1": "path-to-sql-database-project-1",
		...
		"sql-database-key-n": "path-to-sql-database-project-n"
	},
	"etl": "path-to-folder-with-dtsx-files",
	"csharp": "path-to-folder-with-csharp-code"
}
```
3. run
```
dotnet DbDependencyBuilder.dll --config C:\pathto\search-config.json --names Person --types Tbl --output C:\output
```
4. check generated files: [tree](https://rawcdn.githack.com/MaximTkachenko/legacy-db-dependency-builder/59965a0302e11889bf317ed0e481d3f632296d7e/sample/sample-output/1565599401_tree_Person.html) and [graph](https://rawcdn.githack.com/MaximTkachenko/legacy-db-dependency-builder/7289811f4b9430db5dcca0f1825264c1ea809cbf/sample/sample-output/1565599401_graph_Person.html) for [sample csharp project and database](https://github.com/MaximTkachenko/legacy-db-dependency-builder/tree/master/sample).
