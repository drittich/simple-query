# simple-query
A library for generating a simple model layer for a SQLite database 

## Generating the model

To generate the model code, you will run the `SimpleQuery.CodeGen.exe` executable. You can find this in the `C:\Users\[YOUR_USERNAME]]\.nuget\packages\drittich.simplequery\x.x.x\tools` folder. Before doing this, create an `appsettings.json` file in the same folder as the executable. This file should contain the following settings:

```json
{
	"Settings": {
		"ConnectionString": "",
		"TargetFolder": "",
		"ExcludeTables": [ ]
	}
}
```


Populate the connection string for your SQLite database, as well as the target folder you want the files created in. If there are any tables you do not want modeled, you can add them to the `ExcludeTables` list.

Make sure you reference this package in whatever project you have generated the model in.

## Using the model

The `SimpleQueryService` provides a simple and efficient way to interact with a SQLite database in C#. Below are examples of how to use its various methods.

### Setting up SimpleQueryService

First, you need to initialize the `SimpleQueryService` with the connection string to your SQLite database:

```csharp
var connectionString = "Data Source=mydatabase.db;";
var queryService = new SimpleQueryService(connectionString);
```

This will allow you to begin querying your database using the methods provided by `SimpleQueryService`.

### Retrieving a Single Entity by ID

To retrieve the first entity of a specific type with a given ID, you can use the `GetFirstAsync` method:

```csharp
var user = await queryService.GetFirstAsync<User>(userId);
// Process the user object
```

### Retrieving an Entity or Null if Not Found

If you want to retrieve an entity but return `null` if it's not found (to avoid exceptions), use the `GetFirstOrDefaultAsync` method:

```csharp
var user = await queryService.GetFirstOrDefaultAsync<User>(userId);
if (user != null)
{
    // Process the user object
}
else
{
    Console.WriteLine("User not found.");
}
```

### Retrieving All Entities of a Type

To get all entities of a specific type, use the `GetAllAsync` method without providing any IDs:

```csharp
var users = await queryService.GetAllAsync<User>();
foreach (var user in users)
{
    // Process each user
}
```

### Filtering Entities by Column Value

To find entities based on a specific column's value, use the `GetAllByColumnValueAsync` method:

```csharp
var usersByName = await queryService.GetAllByColumnValueAsync<User>("Name", "John Doe");
foreach (var user in usersByName)
{
    // Process each matching user
}
```

### Complex Filtering with WHERE Clause

For more complex queries that require a custom WHERE clause, use the `GetAllByWhereClauseAsync` method:

```csharp
var whereClause = "Age > @Age AND Active = @Active";
var parameters = new { Age = 18, Active = true };
var activeAdultUsers = await queryService.GetAllByWhereClauseAsync<User>(whereClause, parameters);
foreach (var user in activeAdultUsers)
{
    // Process each matching user
}
```

### Retrieving the First Entity Matching Column Values

To retrieve the first entity that matches a set of column values, use `GetFirstByColumnValuesAsync`. This is useful when you need a single entity that matches a complex condition:

```csharp
var columnValues = new Dictionary<string, object>
{
    {"Name", "John Doe"},
    {"Age", 30}
};
var user = await queryService.GetFirstByColumnValuesAsync<User>(columnValues);
// Process the user object
```

### Retrieving the First Entity or Null

If you prefer to get `null` instead of throwing an exception when no entities match the specified criteria, use `GetFirstOrDefaultByColumnValuesAsync`:

```csharp
var columnValues = new Dictionary<string, object>
{
    {"Name", "Jane Doe"},
    {"Active", true}
};
var user = await queryService.GetFirstOrDefaultByColumnValuesAsync<User>(columnValues);
if (user != null)
{
    // Process the user object
}
else
{
    Console.WriteLine("No user matches the specified criteria.");
}
```

### Retrieving All Entities Matching Column Values

To retrieve a list of all entities that match a set of column values, you can use `GetAllByColumnValuesAsync`. This method is particularly useful for more complex queries that cannot be expressed with a single column filter:

```csharp
var columnValues = new Dictionary<string, object>
{
    {"Department", "Engineering"},
    {"YearsOfExperience", 5}
};
var employees = await queryService.GetAllByColumnValuesAsync<Employee>(columnValues);
foreach (var employee in employees)
{
    // Process each employee
}
```
