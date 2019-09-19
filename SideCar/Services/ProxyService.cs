// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SideCar.Extensions;
using TypeKitchen;

namespace SideCar.Services
{
	public class ProxyService
	{
		private readonly PackageService _packages;

		public ProxyService(PackageService packages)
		{
			_packages = packages;
		}
		 
		public async Task<string> GenerateTypeScriptProxy(string packageName, CancellationToken cancellationToken = default)
		{
			var assembly = await _packages.FindAssemblyByNameAsync(packageName, cancellationToken);
			if (assembly == null)
				return null;

			return Pooling.StringBuilderPool.Scoped(sb =>
			{
				//sb.AppendLine($"declare module \"{packageName}\" {{");
				//sb.AppendLine();

				var map = BuildTypeMap(assembly);
				
				//
				// Interfaces:
				foreach (var type in map.Keys)
				{
					if (type.IsClass || type.IsInterface)
					{
						AppendInterface(sb, 0, type, map);
					}
					else if (type.IsEnum)
					{
						AppendEnum(sb, 0, type);
					}
				}

				//
				// Forwarding Classes:
				foreach (var type in map.Keys)
				{
					if (type.IsClass || type.IsInterface)
					{
						AppendClass(sb, 0, packageName, type, map);
					}
					else if (type.IsEnum)
					{
						AppendEnum(sb, 0, type);
					}
				}

				// sb.AppendLine("}"); // module
			});
		}

		private static IReadOnlyDictionary<Type, List<MemberInfo>> BuildTypeMap(Assembly assembly)
		{
			var map = new Dictionary<Type, List<MemberInfo>>();

			foreach (var type in assembly.GetTypes())
				TryMapType(type, map);

			return map;
		}

		private static void TryMapType(Type type, IDictionary<Type, List<MemberInfo>> map)
		{
			if (type == null)
				return;

			if (type.IsArray)
			{
				TryMapType(type.GetElementType(), map);
				return;
			}

			if (Filtered(type))
				return;

			if (!map.TryGetValue(type, out var list))
				map.Add(type, list = new List<MemberInfo>());

			foreach (var member in AccessorMembers.Create(type, scope: AccessorMemberScope.Public))
			{
				switch (member.MemberType)
				{
					case AccessorMemberType.Property:
					{
						if (!map.ContainsKey(member.Type))
							TryMapType(member.Type, map);
						list.Add(member.MemberInfo);
						break;
					}

					case AccessorMemberType.Field:
					{
						if (!map.ContainsKey(member.Type))
							TryMapType(member.Type, map);
						list.Add(member.MemberInfo);
						break;
					}
					case AccessorMemberType.Method:
					{
						list.Add(member.MemberInfo);
						if (member.MemberInfo is MethodInfo method)
						{
							foreach (var parameter in method.GetParameters())
							{
								if (!map.ContainsKey(parameter.ParameterType))
								{
									TryMapType(parameter.ParameterType, map);
								}
							}
						}
						break;
					}

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
		
		private static bool Filtered(Type type)
		{
			return
				type.IsPrimitive || type.IsAbstract ||
				type == typeof(object) || type == typeof(void) || type == typeof(string) || type == typeof(decimal) ||
				type == typeof(Type);
		}

		private static void AppendEnum(StringBuilder sb, int indent, Type type)
		{
			sb.AppendLine();
			AppendIndent(sb, indent);
			sb.AppendLine($"  enum {type.Name} {{");

			var names = type.GetEnumNames();
			var values = type.GetEnumValues();
			
			var i = 0;
			foreach (var value in values)
			{
				AppendIndent(sb, indent + 1);
				sb.Append($"{names[i]} = {(int) value}");
				if (i < values.Length - 1)
					sb.Append(",");
				sb.AppendLine();
				i++;
			}

			AppendIndent(sb, indent);
			sb.AppendLine("}");
		}

		private static void AppendInterface(StringBuilder sb, int indent, Type type, IReadOnlyDictionary<Type, List<MemberInfo>> map)
		{
			AppendIndent(sb, indent);

			sb.AppendLine($"export interface {type.Name} {{");

			if (map.TryGetValue(type, out var members))
			{
				foreach (var member in members)
				{
					switch (member)
					{
						case PropertyInfo p:
							AppendMember(sb, indent + 1, p.PropertyType, p.Name, p.CanRead, p.CanWrite);
							break;
						case FieldInfo f:
							AppendMember(sb, indent + 1, f.FieldType, f.Name, true, true);
							break;
					}
				}
			}

			AppendIndent(sb, indent);
			sb.AppendLine("}");
			sb.AppendLine();
		}

		private static void AppendClass(StringBuilder sb, int indent, string packageName, Type type, IReadOnlyDictionary<Type, List<MemberInfo>> map)
		{
			AppendIndent(sb, indent);
			sb.AppendLine($"export class {type.Name} {{");

			if (map.TryGetValue(type, out var members))
			{
				foreach (var member in members)
				{
					switch (member)
					{
						case PropertyInfo p:
							AppendMember(sb, indent + 1, p.PropertyType, p.Name, p.CanRead, p.CanWrite);
							break;
						case FieldInfo f:
							AppendMember(sb, indent + 1, f.FieldType, f.Name, true, true);
							break;
						case MethodInfo m:
							AppendMethodForwarder(sb, indent + 1, packageName, type, m);
							break;
					}
				}
			}

			AppendIndent(sb, indent);
			sb.AppendLine("}");
			sb.AppendLine();
		}

		private static void AppendMember(StringBuilder sb, int indent, Type type, string name, bool canRead, bool canWrite)
		{
			AppendIndent(sb, indent);

			if (!canRead)
				return;

			if (!canWrite)
				sb.Append("readonly ");

			sb.Append($"{name.ToCamelCase()}: ");

			if (type.IsArray)
			{
				type = type.GetElementType();
				sb.Append(type == null ? "any" : GetTypeName(type));
				sb.Append("[]");
			}
			else if (type.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(type))
			{
				type = type.GetGenericArguments()[0];
				sb.Append(GetTypeName(type));
				sb.Append("[]");
			}
			else if (type.IsGenericType && type.IsValueType && typeof(Nullable<>).MakeGenericType(type).IsAssignableFrom(type))
			{
				type = type.GenericTypeArguments[0];
				sb.Append(GetTypeName(type));
				sb.Append("?");
			}
			else
			{
				sb.Append(GetTypeName(type));
			}
			
			sb.AppendLine(";");
		}

		private static void AppendMethodForwarder(StringBuilder sb, int indent, string packageName, Type type, MethodInfo method)
		{
			if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
				return;
			if (method.DeclaringType == typeof(object))
				return;

			var methodName = method.Name.ToCamelCase();
			var parameters = method.GetParameters();

			AppendIndent(sb, indent);
			sb.Append($"static {methodName}(");
			for (var i = 0; i < parameters.Length; i++)
			{
				if (i != 0)
					sb.Append(", ");
				sb.Append(parameters[i].Name.ToCamelCase()).Append(": ").Append(GetTypeName(parameters[i].ParameterType));
			}
			sb.Append("): ").Append(GetTypeName(method.ReturnType)).AppendLine(" {");
			AppendIndent(sb, indent + 2);
			if (method.ReturnType != typeof(void))
				sb.Append("return ");

			var ns = packageName.Replace(".", "_");
			var className = GetTypeName(type);
			sb.Append($"SideCar.{ns}.{className}.{methodName}").Append("(");
			for (var i = 0; i < parameters.Length; i++)
			{
				if (i != 0)
					sb.Append(", ");
				sb.Append(parameters[i].Name.ToCamelCase());
			}
			sb.AppendLine(");");

			AppendIndent(sb, indent);
			sb.AppendLine("}");
		}

		private static string GetTypeName(Type type)
		{
			if (type == typeof(object))
				return "any";

			if (type == typeof(bool))
				return "boolean";

			if (type == typeof(string) || type == typeof(char))
				return "string";

			if(type == typeof(sbyte) || type == typeof(byte) ||
			   type == typeof(ushort) || type == typeof(short) ||
			   type == typeof(uint) || type == typeof(int) ||
			   type == typeof(ulong) || type == typeof(long) ||
			   type == typeof(float) || type == typeof(double) || type == typeof(decimal))
				return "number";

			if (type == typeof(byte[]))
				return "Uint8Array";
			if (type == typeof(sbyte[]))
				return "Int8Array";
			if (type == typeof(ushort[]))
				return "Uint16Array";
			if (type == typeof(short[]))
				return "Int16Array";
			if (type == typeof(uint[]))
				return "Uint32Array";
			if (type == typeof(int[]))
				return "Int32Array";
			if (type == typeof(ulong[]))
				return "Uint64Array";
			if (type == typeof(long[]))
				return "Int64Array";
			
			if (type == typeof(void))
				return "void";

			if (type != typeof(IDictionary<,>))
				return type.Name;

			var key = GetTypeName(type.GenericTypeArguments[0]);
			var value = GetTypeName(type.GenericTypeArguments[1]);
			return $"{{ [key: {key}]: {value} }}";
		}

		public async Task<string> GenerateJavaScriptProxy(string packageName, CancellationToken cancellationToken)
		{
			var assembly = await _packages.FindAssemblyByNameAsync(packageName, cancellationToken);
			if (assembly == null)
				return null;

			var ns = packageName.Replace(".", "_");

			return Pooling.StringBuilderPool.Scoped(sb =>
			{
				var map = BuildTypeMap(assembly);

				sb.AppendLine("var SideCar = {");
				
				//
				// Function Wrapper:
				AppendIndent(sb, 1);
				sb.AppendLine($"{ns}: {{");
				var count = 0;
				foreach (var type in map.Keys)
				{
					if (!type.IsClass)
						continue;
					if (!map.TryGetValue(type, out var members))
						continue;

					var emit = false;
					foreach (var member in members)
					{
						if (!(member is MethodInfo method))
							continue;
						if (!method.IsStatic)
							continue;
						emit = true;
						break;
					}
					if (!emit)
						continue;

					var className = type.Name;

					AppendIndent(sb, 2);
					sb.AppendLine($"{className}: {{");
					foreach (var member in members)
					{
						if (!(member is MethodInfo method))
							continue;
						if (!method.IsStatic)
							continue;

						AppendIndent(sb, 3);
						sb.Append("function (");
						var parameters = method.GetParameters();
						for (var i = 0; i < parameters.Length; i++)
						{
							if (i != 0)
								sb.Append(", ");
							var parameterName = parameters[i].Name.ToCamelCase();
							sb.Append(parameterName);
						}
						sb.AppendLine(") {");

						AppendIndent(sb, 4);
						if (method.ReturnType != typeof(void))
							sb.Append("return ");
						var methodName = method.Name.ToCamelCase();
						sb.Append($"{ns}.{className}.{methodName}(");
						for (var i = 0; i < parameters.Length; i++)
						{
							if (i != 0)
								sb.Append(", ");
							var parameterName = parameters[i].Name.ToCamelCase();
							sb.Append(parameterName);
						}
						sb.AppendLine(");"); // function call

						AppendIndent(sb, 3);
						sb.AppendLine("}"); // function
					}

					AppendIndent(sb, 2);
					sb.AppendLine("}"); // class
					count++;
					if(count < map.Count -1)
						sb.AppendLine();
				}

				AppendIndent(sb, 1);
				sb.AppendLine($"}},"); // ns

				//
				// Init:
				AppendIndent(sb, 1);
				sb.AppendLine("init: function() {");
				foreach (var type in map.Keys)
				{
					if (!type.IsClass)
						continue;
					if (!map.TryGetValue(type, out var members))
						continue;

					var className = GetTypeName(type);
					foreach (var member in members)
					{
						if (!(member is MethodInfo method))
							continue;
						if (!method.IsStatic)
							continue;
						var methodName = method.Name.ToCamelCase();

						AppendIndent(sb, 2);
						sb.AppendLine($"this.{ns}.{className}.{methodName} = Module.mono_bind_static_method(\"[{packageName}] {type.Name}:{method.Name}\");");
					}
				}

				AppendIndent(sb, 1);
				sb.AppendLine("}"); // init
				sb.AppendLine("}"); // SideCar
			});
		}

		private static void AppendIndent(StringBuilder sb, int indent)
		{
			sb.Append(string.Join(string.Empty, Enumerable.Repeat(" ", indent * 2)));
		}

	}
}
