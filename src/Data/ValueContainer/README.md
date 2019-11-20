This libarary is a part of the [Dasync](https://github.com/tyrotoxin/Dasync) project.

Examples.

1. Create new type:
```csharp
using Dasync.ValueContainer;
using NewtonSoft.Json;

// ...

var properties = new []
{
  new KeyValuePair<string, Type>("AccountName", typeof(string)),
  new KeyValuePair<string, Type>("AccessCode", typeof(long))
};

var container = ValueContainerFactory.Create(properties);
container.SetValue(0, "test");
container.SetValue(1, 12345L);

var json = JsonConvert.SerializeObject(container);
// { "AccountName": "test", "AccessCode": 12345 }
```

2. Create proxy container type to access possibly hidden members:
```csharp
using Dasync.ValueContainer;
using NewtonSoft.Json;
using System.Reflection;

public class AccountAccessCode
{
  private string _name;
  private long _code { get; set; }
}

// ...

var nameFieldInfo = typeof(AccountAccessCode).GetTypeInfo().GetDeclaredField("_name");
var codePropertyInfo = typeof(AccountAccessCode).GetTypeInfo().GetDeclaredProperty("_code");

var properties = new []
{
  new KeyValuePair<MemberInfo, string>(nameFieldInfo, "AccountName"),
  new KeyValuePair<MemberInfo, string>(codePropertyInfo, "AccessCode")
};

var accountAccessCode = new AccountAccessCode();
var container = ValueContainerFactory.CreateProxy(accountAccessCode, properties);
container.SetValue(0, "test");
container.SetValue(1, 12345L);


var json = JsonConvert.SerializeObject(container);
// { "AccountName": "test", "AccessCode": 12345 }
```