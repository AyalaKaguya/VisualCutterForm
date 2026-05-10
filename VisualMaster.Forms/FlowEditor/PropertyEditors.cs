using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VisualMaster.WorkFlow;

namespace VisualMaster.Forms.FlowEditor
{
    public interface IPropertyEditor
    {
        Control CreateEditor(NodePropertyDescriptor pd, FlowNode selectedNode);
    }

    public class BoolPropertyEditor : IPropertyEditor
    {
        public Control CreateEditor(NodePropertyDescriptor pd, FlowNode selectedNode)
        {
            var chk = new CheckBox
            {
                Size = new Size(140, 22),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Checked = pd.Getter() is bool b && b,
            };
            chk.CheckedChanged += (s, e) =>
            {
                pd.Setter(chk.Checked);
            };
            return chk;
        }
    }

    public class NumericPropertyEditor : IPropertyEditor
    {
        public Control CreateEditor(NodePropertyDescriptor pd, FlowNode selectedNode)
        {
            var num = new NumericUpDown
            {
                Size = new Size(120, 22),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                DecimalPlaces = (pd.PropertyType == typeof(float) || pd.PropertyType == typeof(double)) ? 3 : 0,
            };

            var val = pd.Getter();
            var dec = Convert.ToDecimal(val ?? 0);
            num.Minimum = 0;
            num.Maximum = 9999999;
            num.Value = Math.Max(num.Minimum, Math.Min(num.Maximum, dec));

            if (pd.PropertyType == typeof(int) || pd.PropertyType == typeof(long))
                num.DecimalPlaces = 0;

            num.ValueChanged += (s, e) =>
            {
                var converted = ConvertValue(num.Value, pd.PropertyType);
                pd.Setter(converted);
            };
            return num;
        }

        private static object ConvertValue(decimal val, Type targetType)
        {
            if (targetType == typeof(int)) return (int)val;
            if (targetType == typeof(long)) return (long)val;
            if (targetType == typeof(float)) return (float)val;
            if (targetType == typeof(double)) return (double)val;
            return val;
        }
    }

    public class EnumPropertyEditor : IPropertyEditor
    {
        public Control CreateEditor(NodePropertyDescriptor pd, FlowNode selectedNode)
        {
            var cmb = new ComboBox
            {
                Size = new Size(200, 22),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
            };

            var enumValues = Enum.GetValues(pd.PropertyType);
            var current = pd.Getter();
            int selIdx = 0;
            int idx = 0;
            foreach (var ev in enumValues)
            {
                cmb.Items.Add(ev.ToString());
                if (ev.Equals(current)) selIdx = idx;
                idx++;
            }
            if (cmb.Items.Count > 0)
                cmb.SelectedIndex = Math.Min(selIdx, cmb.Items.Count - 1);

            cmb.SelectedIndexChanged += (s, e) =>
            {
                if (cmb.SelectedIndex >= 0)
                {
                    var enumVal = Enum.GetValues(pd.PropertyType).GetValue(cmb.SelectedIndex);
                    pd.Setter(enumVal);
                }
            };
            return cmb;
        }
    }

    public class StringPropertyEditor : IPropertyEditor
    {
        public Control CreateEditor(NodePropertyDescriptor pd, FlowNode selectedNode)
        {
            var txt = new TextBox
            {
                Size = new Size(200, 22),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Text = pd.Getter()?.ToString() ?? "",
                BorderStyle = BorderStyle.FixedSingle,
            };
            txt.Leave += (s, e) => pd.Setter(txt.Text);
            return txt;
        }
    }

    public class PropertyEditorRegistry
    {
        private readonly Dictionary<Type, IPropertyEditor> _typeEditors = new Dictionary<Type, IPropertyEditor>();
        private readonly Dictionary<string, IPropertyEditor> _nameEditors = new Dictionary<string, IPropertyEditor>();

        public void Register(Type type, IPropertyEditor editor)
        {
            _typeEditors[type] = editor;
        }

        public void Register(string propertyName, IPropertyEditor editor)
        {
            _nameEditors[propertyName] = editor;
        }

        public IPropertyEditor Find(Type propertyType, string propertyName)
        {
            if (_nameEditors.TryGetValue(propertyName, out var nameEditor))
                return nameEditor;
            if (_typeEditors.TryGetValue(propertyType, out var typeEditor))
                return typeEditor;
            return null;
        }

        public static PropertyEditorRegistry CreateDefault()
        {
            var registry = new PropertyEditorRegistry();
            registry.Register(typeof(bool), new BoolPropertyEditor());
            registry.Register(typeof(int), new NumericPropertyEditor());
            registry.Register(typeof(long), new NumericPropertyEditor());
            registry.Register(typeof(float), new NumericPropertyEditor());
            registry.Register(typeof(double), new NumericPropertyEditor());
            registry.Register(typeof(string), new StringPropertyEditor());
            return registry;
        }
    }
}
