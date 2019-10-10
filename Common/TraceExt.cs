using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Common
{
	public enum TraceDirection
	{
		Console,
		Debug
	}

	public static class TraceExt
	{
		public static string Repeat(this string str, int n)
		{
			return string.Join(str, new string[n + 1]);
		}

		public static T To<T>(this object obj)
		{
			return (T)obj;
		}

		public static IEnumerable<object> AsGenericEnumerable(this System.Collections.IEnumerable collection)
		{
			foreach (var item in collection)
			{
				yield return item;
			}
		}

		private static string GetTypeC(this Type t)
		{
			if (t.IsEnum)
				return "E";
			else if (t.IsValueType)
				return "V";
			else if (t.IsArray)
				return "A";
			else if (t.IsInterface)
				return "I";
			else if (t.IsClass)
				return "C";
			else
				return "U";
		}

		public static TraceDirection TraceDirection { get; set; }
		public static string Indent { get; set; }

		static TraceExt()
		{
			Indent = "  ";
		}

		public static void Trace<T>(this T obj, string varName = "", int depth = int.MaxValue, bool showTypes = false, string[] except = null)
		{
			var ret = obj.Collect(varName, depth, showTypes, except);

			switch (TraceDirection)
			{
				case TraceDirection.Console: Console.WriteLine(ret); break;
				case TraceDirection.Debug: System.Diagnostics.Debug.Print(ret); break;
				default: throw new InvalidOperationException();
			}
		}

		public static string Collect<T>(this T obj, string varName = "", int depth = int.MaxValue, bool showTypes = false, string[] except = null)
		{
			return Collect(typeof(T), () => obj, varName, depth, new Dictionary<object, string>(), showTypes, 0, new ExceptInfo(except));
		}

		private static string Collect(Type varType, Func<object> objf, string varName, int depth, Dictionary<object, string> traced, bool showTypes, int stdepth, ExceptInfo except)
		{
			if (stdepth > 10)
				return "!Не удается получить содержимое!";

			var ret = ((varType != null && showTypes) ? ("[" + varType.GetTypeC() + "#" + varType.Name + "] ") : ("")) +
					  ((varName != null) ? (varName + " : ") : (""));

			object obj = null;
			try { obj = objf(); }
			catch (Exception e)
			{
				while (e.InnerException != null)
					e = e.InnerException;

				return ret + "!Не удается получить содержимое : '" + e.Message + "'!";
			}

			if (obj == null)
				return ret + "null";

			var t = obj.GetType();
			ret += (varType != t && showTypes) ? ("[" + t.GetTypeC() + "#" + t.Name + "]") : ("");

			double v;
			bool b;
			string str = obj.ToString().Replace(Environment.NewLine, Environment.NewLine + " ".Repeat(ret.Length));

			if (t.IsEnum)
			{
				//ret += "\t\t #Enum";
				return ret + str + " = " + t.GetFields(System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)[0].GetValue(obj).ToString();
			}
			else if (double.TryParse(str, out v) || bool.TryParse(str, out b) || obj is DateTime || obj is TimeSpan)
			{
				return ret + str;
			}
			else
			{
				var dc = t.GetMethod("ToString", new Type[0]).DeclaringType;
				if (!dc.Equals(typeof(object)) && !dc.Equals(typeof(ValueType)))
					ret += " \"" + str + "\"";
			}

			if (depth == 0)
				return ret;

			if (obj is string)
				return ret + ", Длина = " + obj.To<string>().Length;

			//var props = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty);
			//var fields = t.GetFields(System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

			var props = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty);
            var fields = t.GetFields(System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | BindingFlags.NonPublic)
                          .Where(f => f.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true).Length == 0)
                          .ToArray();

			if (props.Length == 0 && fields.Length == 0)
				return ret;

			if (!t.IsValueType)
			{
				string msg;
				if (traced.TryGetValue(obj, out msg))
				{
					return ret + " " + msg;
				}
				else
				{
					traced.Add(obj, "!Уже прослеживается!");
				}

				if ((obj is System.Collections.IEnumerable) && !(obj is string))
				{
					var ifs = t.GetInterfaces()
							   .Select(it => it.FullName)
							   .Select(itn => (itn.Contains('[')) ? (itn.Substring(0, itn.IndexOf('['))) : (itn))
							   .ToArray();

					var enumerable = obj.To<System.Collections.IEnumerable>()
										.AsGenericEnumerable();

					if (t.IsArray && t.GetArrayRank() > 1)
					{ // multidimensional arrays
						throw new NotImplementedException("");
					}
					//else if (ifs.Contains("System.Collections.IDictionary") || ifs.Contains("System.Collections.Generic.IDictionary`2"))
					//{ // dictionary
					//    var cnt = (t.GetProperty("Length") ?? t.GetProperty("Count")).GetValue(obj, null);

					//    if (cnt is int && (int)cnt != 0)
					//        throw new NotImplementedException("");


					//    ret += " Count = " + cnt.ToString() + " { " + Environment.NewLine;
					//    ret += Environment.NewLine + "}";
					//}
					else if (ifs.Contains("System.Collections.IList") || ifs.Contains("System.Collections.Generic.IList`1"))
					{ // plain indexable enumerable
						ret += " Количество = " + (t.GetProperty("Length") ?? t.GetProperty("Count")).GetValue(obj, null) + " { " + Environment.NewLine;
						ret += string.Join("," + Environment.NewLine,
							enumerable.Select((el, n) => Indent + "(" + n + ") " + ((el == null) ? (":  null") : Collect(null, () => el, el.GetType().Name, depth - 1, traced, showTypes, (el.GetType().Equals(t)) ? (stdepth + 1) : (0), except)))
											  .Select(fd => fd.Replace(Environment.NewLine, Environment.NewLine + Indent))
										  );
						ret += Environment.NewLine + "}";
					}
					else if (ifs.Contains("System.Collections.ICollection") || ifs.Contains("System.Collections.Generic.ICollection`1"))
					{ // some collection
						ret += " { " + Environment.NewLine;
						ret += string.Join("," + Environment.NewLine,
									enumerable.Select(el => Indent + ((el == null) ? (":  null") : Collect(null, () => el, el.GetType().Name, depth - 1, traced, showTypes, (el.GetType().Equals(t)) ? (stdepth + 1) : (0), except)))
											  .Select(fd => fd.Replace(Environment.NewLine, Environment.NewLine + Indent))
										  );
						ret += Environment.NewLine + "}";
					}
					else
					{ // else enumerable
						throw new NotImplementedException("");
					}

					return ret;
				}
			}

			props = props.Where(prop => !except.IsExcluded(t, prop)).OrderBy(prop => prop.Name).ToArray(); ;
			//fields = fields.Where(fld => !except.IsExcluded(t, fld)).OrderBy(fld => fld.Name).ToArray();

			ret += " { " + Environment.NewLine;
			ret += string.Join(Environment.NewLine,
						fields.Select(fld => Indent + ((showTypes) ? ("F# ") : ("")) + Collect(fld.FieldType, () => fld.GetValue(obj), fld.Name, depth - 1, traced, showTypes, (fld.FieldType.Equals(t)) ? (stdepth + 1) : (0), except))
							  .Select(fd => fd.Replace(Environment.NewLine, Environment.NewLine + Indent))
							  );
			ret += string.Join(Environment.NewLine,
						props.Where(prop => (prop.GetIndexParameters() == null) ? (true) : (prop.GetIndexParameters().Length == 0))
							 .Select(prop => Indent + ((showTypes) ? ("P# ") : ("")) + Collect(prop.PropertyType, () => prop.GetValue(obj, null), prop.Name, depth - 1, traced, showTypes, (prop.PropertyType.Equals(t)) ? (stdepth + 1) : (0), except))
							 .Select(pd => pd.Replace(Environment.NewLine, Environment.NewLine + Indent))
							   );
			ret += Environment.NewLine + "}";

			return ret;
		}

		private class ExceptInfo
		{
			public bool NoOne { get; private set; }

			private LinkedList<string> _types = null;
			private LinkedList<KeyValuePair<string, string>> _props = null;

			public ExceptInfo(string[] except)
			{
				if (except == null || except.Length == 0)
				{
					NoOne = true;
					return;
				}

				foreach (var t in except)
				{
					if (t.Length <= 0)
						continue;

					var n = t.IndexOf('.');
					if (t[0] == '[' && t[t.Length - 1] == ']' && t.Length > 2)
					{
						if (_types == null)
							_types = new LinkedList<string>();

						_types.AddLast(t.Substring(1, t.Length - 2));
					}
					else if (n > 0 && n < t.Length - 1 && t.Length > 2)
					{
						if (_props == null)
							_props = new LinkedList<KeyValuePair<string, string>>();

						_props.AddLast(new KeyValuePair<string, string>(t.Substring(0, n), t.Substring(n + 1, t.Length - n - 1)));
					}
				}
			}

			private string[] ExpandType(Type t)
			{
				LinkedList<Type> _ret = new LinkedList<Type>();
				
				var ct = t;
				while (ct != null)
				{
					_ret.AddLast(ct);
					ct = ct.BaseType;
				}

				return _ret.Union(t.GetInterfaces()).Select(ctt => ctt.Name).ToArray();
			}

			public bool IsExcluded(Type t, MemberInfo m)
			{
				if (NoOne)
					return false;

				var expanded = ExpandType(t);

				if (_types != null)
				{
					if (expanded.Intersect(_types).Count() != 0)
						return true;
				}

				if (_props != null)
				{
					foreach (var p in _props)
					{
						if (p.Key == "*")
						{
							if (m.Name == p.Value)
								return true;
						}
						else if (p.Value == "*")
						{
							//if (t.Name == p.Key)
							if (expanded.Contains(p.Key))
								return true;
						}
						else
						{
							if (t.Name == p.Key && m.Name == p.Value)
								return true;
						}
					}
				}

				return false;
			}

		}
	}
}
