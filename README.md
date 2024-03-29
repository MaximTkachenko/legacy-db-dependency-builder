# legacy-db-dependency-builder

The tool builds dependency map for particular SQL objects (tables, stored procedures etc.). It's useful especially in case of legacy systems with a lot of nested views, synonyms and compelx stored procedures.

```
-c, --config    Required. Path to json configuration file.

-n, --names     Required.  Comma separated root sql objects. Provide fragment of name or full name.

-t, --types     Filter for sql object type of root. Possible values: Tbl (table), Syn (synonym), Sp (stored procedure), Fun (function), V (view). All types by default.

-o, --output    Required. Directory for output files.

-e, --exact     Define how to search for roots. 1 means 'equals', 0 means 'contains'. 'Equals' by default.

--help          Display this help screen.
```

1. [import](https://docs.microsoft.com/en-us/sql/ssdt/import-into-a-database-project?view=sql-server-2017) database schema to sql project
2. prepare json config in the following format ([sample](https://github.com/MaximTkachenko/legacy-db-dependency-builder/blob/master/sample/search-config.json)):
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
4. check generated files: [tree](https://rawcdn.githack.com/MaximTkachenko/legacy-db-dependency-builder/414d28de8637fecd895dd4f52df1f593b32c516f/sample/sample-output/1566227212_tree_Person.html) and [graph](https://rawcdn.githack.com/MaximTkachenko/legacy-db-dependency-builder/414d28de8637fecd895dd4f52df1f593b32c516f/sample/sample-output/1566227212_graph_Person.html) of dependencies for [sample csharp project and database](https://github.com/MaximTkachenko/legacy-db-dependency-builder/tree/master/sample).
