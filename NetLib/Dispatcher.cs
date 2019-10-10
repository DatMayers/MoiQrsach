using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NetLib
{
    public class Dispatcher<T>
    {
        Dictionary<Type, Delegate> _methods = new Dictionary<Type, Delegate>();

        public Dispatcher() : this(m => m.Name == "Handle" ? m.GetParameters()[0].ParameterType : null) { }

        public Dispatcher(Func<MethodInfo, Type> selector)
        {
            RecursiveRegisterMethods(typeof(T), selector);
        }

        private void RecursiveRegisterMethods(Type t, Func<MethodInfo, Type> selector)
        {
            var baseType = t.BaseType;
            if (baseType != null)
            {
                RecursiveRegisterMethods(baseType, selector);
            }

            foreach (var item in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (item.GetParameters().Length == 1 && item.ReturnType == typeof(void))
                {
                    var argType = selector(item);
                    if (argType != null)
                    {
                        _methods[argType] = Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(T), argType), item);
                    }
                }
            }
        }

        public bool Dispatch(T handler, object obj)
        {
            Delegate method;
            if (_methods.TryGetValue(obj.GetType(), out method))
                method.DynamicInvoke(handler, obj);

            return method != null;
        }
    }
}
