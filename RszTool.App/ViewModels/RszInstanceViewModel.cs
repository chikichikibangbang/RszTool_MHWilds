﻿using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RszTool.App.ViewModels
{
    public interface IFieldValueViewModel
    {
        string Name { get; }
        object Value { get; set; }
        RszField Field {  get; }
    }


    /// <summary>
    /// 字段
    /// </summary>
    public class BaseRszFieldViewModel(RszInstance instance, int index) : INotifyPropertyChanged
    {
        protected readonly RszInstance instance = instance;

        public int Index { get; } = index;

        public RszField Field => instance.RszClass.fields[Index];
        public string Name => Field.name;
        public RszFieldType Type => Field.type;
        public string OriginalType => Field.original_type;

        public virtual object Value
        {
            get => instance.Values[Index];
            set
            {
                instance.Values[Index] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }


    /// <summary>
    /// 普通字段
    /// </summary>
    public class RszFieldNormalViewModel(RszInstance instance, int index) :
        BaseRszFieldViewModel(instance, index), IFieldValueViewModel
    {
    }


    /// <summary>
    /// object字段
    /// </summary>
    public class RszFieldInstanceViewModel(RszInstance instance, int index) :
        BaseRszFieldViewModel(instance, index)
    {
        public override object Value =>
            RszInstanceToFieldViewModels.InstanceToFieldViewModels((RszInstance)instance.Values[Index]);
    }


    /// <summary>
    /// array字段
    /// </summary>
    public class RszFieldArrayViewModel : BaseRszFieldViewModel
    {
        public RszFieldArrayViewModel(RszInstance instance, int index) : base(instance, index)
        {
        }

        public override object Value
        {
            get
            {
                if (itemViewModels == null)
                {
                    var values = (List<object>)instance.Values[Index];
                    bool isReference = Field.IsReference;
                    itemViewModels = new(values.Count);
                    for (int i = 0; i < values.Count; i++)
                    {
                        itemViewModels.Add(isReference ?
                            new RszFieldArrayInstanceItemViewModel(Field, values, i) :
                            new RszFieldArrayNormalItemViewModel(Field, values, i));
                    }
                }
                return itemViewModels;
            }
        }

        private List<BaseRszFieldArrayItemViewModel>? itemViewModels;
    }


    /// <summary>
    /// 数组的项
    /// </summary>
    public class BaseRszFieldArrayItemViewModel(
            RszField field, List<object> values, int arrayIndex) : INotifyPropertyChanged
    {
        public int Index { get; } = arrayIndex;

        public RszField Field { get; } = field;
        public string Name => $"{Index}: {OriginalType}";
        protected List<object> Values { get; } = values;

        public RszFieldType Type => Field.type;
        public string OriginalType => Field.original_type;

        public virtual object Value
        {
            get => Values[Index];
            set
            {
                Values[Index] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }


    /// <summary>
    /// 普通数组的项
    /// </summary>
    public class RszFieldArrayNormalItemViewModel(RszField field, List<object> values, int arrayIndex) :
        BaseRszFieldArrayItemViewModel(field, values, arrayIndex), IFieldValueViewModel
    {
    }


    /// <summary>
    /// object数组的项
    /// </summary>
    public class RszFieldArrayInstanceItemViewModel(RszField field, List<object> values, int arrayIndex) :
        BaseRszFieldArrayItemViewModel(field, values, arrayIndex)
    {
        public override object Value =>
            RszInstanceToFieldViewModels.InstanceToFieldViewModels((RszInstance)Values[Index]);
    }
}
