# simple-query
A library for generating a simple model layer for a SQLite database 

## Usage

To generate the model code, you will run the `SimpleQuery.CodeGen.exe` executable. You can find this in the `C:\Users\[YOUR_USERNAME]]\.nuget\packages\drittich.simplequery\1.0.1\tools` folder. Before doing this, create an `appsettings.json` file in the same folder as the executable. This file should contain the following settings:

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