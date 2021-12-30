using System;
using System.Collections.Generic;
using System.Reflection;

namespace RHLDark {
    internal static class TypeUtil {
        public static object GetStaticPrivateField(Type type, string name) {
            var field = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            return field.GetValue(null);
        }

        public static void SetPrivateField(object obj, string name, object value) {
            var type = obj.GetType();
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(obj, value);
        }

        public static object GetPrivateField(object obj, string name) {
            var type = obj.GetType();
            var field = type.GetField(name, BindingFlags.NonPublic| BindingFlags.Instance);
            return field.GetValue(obj);
        }
    }
}
