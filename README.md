[![Build Status](https://dev.azure.com/mtkorg/oss-projects/_apis/build/status/MaximTkachenko.legacy-db-dependency-builder?branchName=master)](https://dev.azure.com/mtkorg/oss-projects/_build/latest?definitionId=4&branchName=master)

# legacy-db-dependency-builder

```
dotnet DbDependencyBuilder.dll -c C:\code\repos\legacy-db-dependency-builder\sample\search-config.json -n main.Person
```

Build dependency map for particular SQL objects (tables etc.). It's useful especially in case of legacy databases with a lot of nested views, synonyms and compelx stored procedures.
