# Code conventions
The [Microsoft coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) are applied for this project.
For other, unspecified scenario's, inspire yourself from the existing codebases like [ASP.NET Core](https://github.com/aspnet/Mvc).
A good example is private class fields, they begin with an underscore.

## Overloads
Lot of methods have overloads to increase testability and support custom actions and settings, but take care. Using custom parameters overrides the consensus rules and may result in block/transaction rejections.
We advise you to use the minimum amount of arguments if you plan to use one of the Logistichain libraries.

### Avoid default parameters
We explicitly use overloading instead of default parameters to prevent mismatches in case of Logistichain library updates. [Some compilers copy-paste the default value](https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1026-default-parameters-should-not-be-used).
*Logistichain library with default parameter example:*
```csharp
public class Test {
	void TestMethod(string input, string addition = "abc") {};
}
```

*Your project, using the Logistichain library:*
```csharp
public class Consumer {
	void Consume() {
	var t = new Logistichain.Test();
	t.TestMethod("Hi!");
	};
}
```

*Your compiler:*
```csharp
public class Consumer {
	void Consume() {
	var t = new Test();
	t.TestMethod("Hi!", "abc");
	};
}
```

Whenever the default value changes in an update, **you must recompile** your project in order to use the new default value. This may introduce bugs, so we decided to use overloading.