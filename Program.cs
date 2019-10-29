using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace expression_example
{
  public class Program
  {
    private static readonly List<User> UserData = UserDataSeed();

    private static IDictionary<string, object> Cache = new Dictionary<string, object>();
    
    private static void Main(string[] args)
    {
      const string propertyName = "LastName";
      const string value = "a";

      var dn = GetDynamicQueryWithExpresionTrees0(propertyName, value);
      var q = UserData.Where(dn);

      var output = q.ToList();
      
      foreach (var item in output)
      {
        Console.WriteLine("Filtered result:");
        Console.WriteLine($"\t Id: {item.Id}");
        Console.WriteLine($"\t First Name: {item.FirstName}");
        Console.WriteLine($"\t Last Name: {item.LastName}");
      }
      
      // Now use the cached version
      const string propertyName0 = "LastName";
      const string value0 = "a";
      
      var dn0 = GetDynamicQueryWithExpresionTrees0(propertyName0, value0);
      var q0 = UserData.Where(dn);

      var output0 = q0.ToList();
      
      foreach (var item in output0)
      {
        Console.WriteLine("Filtered result:");
        Console.WriteLine($"\t Id: {item.Id}");
        Console.WriteLine($"\t First Name: {item.FirstName}");
        Console.WriteLine($"\t Last Name: {item.LastName}");
      }

    }

    private static List<User> UserDataSeed()
    {
      return new List<User>
      {
        new User {Id = 1, FirstName = "John", LastName = "Wayne"},
        new User {Id = 2, FirstName = "Gary", LastName = "Cooper"},
        new User {Id = 3, FirstName = "Elizabeth", LastName = "Taylor"}
      };
    }

    private static Func<User, bool> GetDynamicQueryWithExpresionTrees(string propertyName, string val)
    {
      var param = Expression.Parameter(typeof(User), "x");
      var member = Expression.Property(param, propertyName);
      var valExpression = GetValueExpression(propertyName, val, param);
      Expression body = Expression.Equal(member, valExpression);
      var final = Expression.Lambda<Func<User, bool>>(body, param);
      
      return final.Compile();
    }

    private static Func<User, bool> GetDynamicQueryWithExpresionTrees0(string propertyName, string value)
    {
      // cache with key of Expression + model + propertyName + value
      var key = $"{nameof(User)}-{propertyName}+{value}";

      var expression = FindCachedExpression(key);

      if (expression != null)
      {
        return (Func<User, bool>) expression;
      }
      
      // System.String ToLower()
      MethodInfo toLower = typeof(string).GetMethod(nameof(string.ToLower), new Type[] { });
      
      // Boolean Contains(System.String)
      MethodInfo containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
      
      // x
      ParameterExpression parameter = Expression.Parameter(typeof(User), "x");
      
      // x.LastName
      Expression parameterProperty = Expression.Property(parameter, propertyName);
      
      // x.LastName.ToLower()
      Expression parameterPropertyFunction = Expression.Call(parameterProperty, toLower);
      
      // "a"
      Expression argument = Expression.Constant(value, typeof(string));
      
      // "a".ToLower()
      Expression argumentFunction = Expression.Call(argument, toLower);

      // x.LastName.ToLower().Contains("a")
      Expression containsMethodBody = Expression.Call(parameterPropertyFunction, containsMethod, argumentFunction);
      
      // x => x.LastName.ToLower().Contains("a")
      Expression<Func<User, bool>> final = Expression.Lambda<Func<User, bool>>(containsMethodBody, parameter);

      var compiled = final.Compile();

      Cache.Add(key, compiled);
      
      return compiled;

    }

    private static object FindCachedExpression(string key)
    {
      var q =
        from x in Cache where x.Key == key select x.Value;

      return q.FirstOrDefault();

    }
    
    private static UnaryExpression GetValueExpression(string propertyName, string val, ParameterExpression param)
    {
      var member = Expression.Property(param, propertyName);
      var propertyType = ((PropertyInfo) member.Member).PropertyType;
      var converter = TypeDescriptor.GetConverter(propertyType);

      if (!converter.CanConvertFrom(typeof(string)))
        throw new NotSupportedException();

      var propertyValue = converter.ConvertFromInvariantString(val);
      var constant = Expression.Constant(propertyValue);
      return Expression.Convert(constant, propertyType);
      
    }
    
  }
  
}