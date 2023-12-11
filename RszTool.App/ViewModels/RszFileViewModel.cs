﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using RszTool.App.Common;
using RszTool.App.Resources;

namespace RszTool.App.ViewModels
{
    public abstract class BaseRszFileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public abstract BaseRszFile File { get; }
        public string? FilePath => File.FileHandler.FilePath;
        public string? FileName
        {
            get
            {
                string? path = FilePath;
                return path != null ? Path.GetFileName(path) : null;
            }
        }
        private bool changed;
        public bool Changed
        {
            get => changed;
            set
            {
                changed = value;
                HeaderChanged?.Invoke();
            }
        }
        public object? SelectedItem { get; set; }
        public InstanceSearchViewModel InstanceSearchViewModel { get; } = new();
        public ObservableCollection<RszInstance>? SearchInstanceList { get; set; }

        public static RelayCommand CopyInstance => new(OnCopyInstance);
        public RelayCommand PasteInstance => new(OnPasteInstance);
        public static RelayCommand CopyNormalField => new(OnCopyNormalField);
        public RelayCommand PasteNormalField => new(OnPasteNormalField);
        public static RelayCommand ArrayItemCopy => new(OnArrayItemCopy);
        public RelayCommand ArrayItemRemove => new(OnArrayItemRemove);
        public RelayCommand ArrayItemDuplicate => new(OnArrayItemDuplicate);
        public RelayCommand ArrayItemDuplicateMulti => new(OnArrayItemDuplicateMulti);
        public RelayCommand ArrayItemPasteAfter => new(OnArrayItemPasteAfter);
        public RelayCommand ArrayItemPasteToSelf => new(OnArrayItemPasteToSelf);
        public RelayCommand ArrayItemNew => new(OnArrayItemNew);
        public RelayCommand ArrayItemPaste => new(OnArrayItemPaste);
        public RelayCommand SearchInstances => new(OnSearchInstances);

        /// <summary>
        /// 标题改变(SaveAs或者Changed)
        /// </summary>
        public event Action? HeaderChanged;

        /// <summary>
        /// Item改变了，但Item本身不支持Notify，Items用了Convert的情况，通知TreeView更新
        /// </summary>
        // public event Action<object>? TreeViewItemChanged;

        public static RszInstance? CopiedInstance { get; private set; }
        public static RszField? CopiedNormalField { get; private set; }
        public static object? CopiedNormalValue { get; private set; }

        public virtual void PostRead() {}

        public bool Read()
        {
            if (File.Read())
            {
                PostRead();
                Changed = false;
                return true;
            }
            return false;
        }

        public bool Save()
        {
            bool result = File.Save();
            if (result)
            {
                Changed = false;
            }
            return result;
        }

        public bool SaveAs(string path)
        {
            bool result = File.SaveAs(path);
            if (result)
            {
                Changed = false;
                HeaderChanged?.Invoke();
            }
            return result;
        }

        public bool Reopen()
        {
            // TODO check Changed
            File.FileHandler.Reopen();
            bool result = Read();
            OnPropertyChanged(nameof(TreeViewItems));
            return result;
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void OnCopyInstance(object arg)
        {
            if (arg is RszInstance instance)
            {
                CopiedInstance = instance;
            }
            else if (arg is RszFieldInstanceViewModel fieldInstanceViewModel)
            {
                CopiedInstance = fieldInstanceViewModel.Instance;
            }
        }

        private void OnPasteInstance(object arg)
        {
            if (arg is RszInstance instance)
            {
                if (DoPasteInstance(instance))
                {
                    Changed = true;
                }
            }
            else if (arg is RszFieldInstanceViewModel fieldInstanceViewModel)
            {
                if (DoPasteInstance(fieldInstanceViewModel.Instance))
                {
                    fieldInstanceViewModel.NotifyItemsChanged();
                    Changed = true;
                }
            }
        }

        private bool DoPasteInstance(RszInstance instance)
        {
            if (CopiedInstance == null) return false;
            if (CopiedInstance.RszClass != instance.RszClass)
            {
                MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedInstance.RszClass.name, instance.RszClass.name));
                return false;
            }
            if (File.GetRSZ() is not RSZFile rsz) return false;
            return rsz.InstanceCopyValues(instance, CopiedInstance);
        }

        private static void OnCopyNormalField(object arg)
        {
            if (arg is RszFieldNormalViewModel item)
            {
                CopiedNormalField = item.Field;
                CopiedNormalValue = item.Value;
            }
        }

        private void OnPasteNormalField(object arg)
        {
            if (arg is RszFieldNormalViewModel item && CopiedNormalField != null && CopiedNormalValue != null)
            {
                if (CopiedNormalField.type != item.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedNormalField.type, item.Field.type));
                    return;
                }
                item.Value = CopiedNormalValue;
                Changed = true;
            }
        }

        private static void OnArrayItemCopy(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                CopiedInstance = item.Instance;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                CopiedNormalField = normalItem.Field;
                CopiedNormalValue = normalItem.Value;
            }
        }

        /// <summary>
        /// 移除数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemRemove(object arg)
        {
            if (arg is BaseRszFieldArrayItemViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                rsz.ArrayRemoveItem(item.Values, item.Value);
                item.Array.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 重复数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemDuplicate(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                rsz.ArrayInsertInstance(item.Values, item.Instance, item.Index + 1);
                item.Array.NotifyItemsChanged();
                Changed = true;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                rsz.ArrayInsertItem(normalItem.Values, normalItem.Value, normalItem.Index + 1);
                normalItem.Array.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 重复多次数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemDuplicateMulti(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            Views.InputDialog dialog = new()
            {
                Title = Texts.Duplicate,
                Message = Texts.InputDulicateCount,
                InputText = "1",
                Owner = Application.Current.MainWindow,
            };
            // 显示对话框，并等待用户输入
            if (dialog.ShowDialog() != true) return;
            string userInput = dialog.InputText;
            int count = int.Parse(userInput);
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                for (int i = 0; i < count; i++)
                {
                    rsz.ArrayInsertInstance(item.Values, item.Instance, item.Index + 1);
                }
                item.Array.NotifyItemsChanged();
                Changed = true;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                for (int i = 0; i < count; i++)
                {
                    rsz.ArrayInsertItem(normalItem.Values, normalItem.Value, normalItem.Index + 1);
                }
                normalItem.Array.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 在数组项后面粘贴
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemPasteAfter(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            if (arg is RszFieldArrayInstanceItemViewModel item && CopiedInstance != null)
            {
                if (CopiedInstance.RszClass != item.Instance.RszClass)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedInstance.RszClass.name, item.Instance.RszClass.name));
                    return;
                }
                rsz.ArrayInsertInstance(item.Values, CopiedInstance, item.Index + 1);
                item.Array.NotifyItemsChanged();
                Changed = true;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem
                && CopiedNormalField != null && CopiedNormalValue != null)
            {
                if (CopiedNormalField.type != normalItem.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedNormalField.type, normalItem.Field.type));
                    return;
                }
                rsz.ArrayInsertItem(normalItem.Values, CopiedNormalValue, normalItem.Index + 1);
                normalItem.Array.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴到自身
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemPasteToSelf(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                if (DoPasteInstance(item.Instance))
                {
                    item.NotifyItemsChanged();
                    Changed = true;
                }
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem
                && CopiedNormalField != null && CopiedNormalValue != null)
            {
                if (CopiedNormalField.type != normalItem.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedNormalField.type, normalItem.Field.type));
                    return;
                }
                normalItem.Value = CopiedNormalValue;
                Changed = true;
            }
        }

        /// <summary>
        /// 数组新建项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemNew(object arg)
        {
            if (arg is RszFieldArrayViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                string? className = null;
                if (item.Field.IsReference)
                {
                    className = RszInstance.GetElementType(item.Field.original_type);
                    Views.InputDialog dialog = new()
                    {
                        Title = Texts.NewItem,
                        Message = Texts.InputClassName,
                        InputText = className,
                        Owner = Application.Current.MainWindow,
                    };
                    if (dialog.ShowDialog() != true) return;
                    className = dialog.InputText;
                }
                var newItem = RszInstance.CreateArrayItem(rsz.RszParser, item.Field, className);
                if (newItem == null) return;
                if (newItem is RszInstance instance)
                {
                    rsz.ArrayInsertInstance(item.Values, instance, clone: false);
                }
                else
                {
                    rsz.ArrayInsertItem(item.Values, newItem);
                }
                item.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴到数组
        /// </summary>
        private void OnArrayItemPaste(object arg)
        {
            if (arg is RszFieldArrayViewModel item && CopiedInstance != null &&
                File.GetRSZ() is RSZFile rsz)
            {
                string className = RszInstance.GetElementType(item.Field.original_type);
                if (className != CopiedInstance.RszClass.name &&
                    !MessageBoxUtils.Confirm(string.Format(
                        Texts.RszClassMismatchConfirm, CopiedInstance.RszClass.name, className)))
                {
                    return;
                }
                rsz.ArrayInsertInstance(item.Values, CopiedInstance);
                item.NotifyItemsChanged();
                Changed = true;
            }
        }

        private void OnSearchInstances(object arg)
        {
            SearchInstanceList ??= new();
            SearchInstanceList.Clear();
            if (File.GetRSZ() is not RSZFile rsz) return;
            InstanceFilter filter = new(InstanceSearchViewModel);
            if (!filter.Enable) return;
            foreach (var instance in rsz.InstanceList)
            {
                if (filter.IsMatch(instance))
                {
                    SearchInstanceList.Add(instance);
                }
            }
        }

        public abstract IEnumerable<object> TreeViewItems { get; }
    }


    public class UserFileViewModel(UserFile file) : BaseRszFileViewModel
    {
        public override BaseRszFile File => UserFile;
        public UserFile UserFile { get; } = file;

        public RszViewModel RszViewModel => new(UserFile.RSZ!);

        public override IEnumerable<object> TreeViewItems
        {
            get
            {
                yield return new TreeItemViewModel("Instances", RszViewModel.Instances);
                yield return new TreeItemViewModel("Objects", RszViewModel.Objects);
            }
        }
    }


    public class PfbFileViewModel(PfbFile file) : BaseRszFileViewModel
    {
        public override BaseRszFile File => PfbFile;
        public PfbFile PfbFile { get; } = file;
        public RszViewModel RszViewModel => new(PfbFile.RSZ!);
        public IEnumerable<PfbFile.GameObjectData>? GameObjects => PfbFile.GameObjectDatas;
        public GameObjectSearchViewModel GameObjectSearchViewModel { get; } = new() { IncludeChildren = true };
        public ObservableCollection<PfbFile.GameObjectData>? SearchGameObjectList { get; set; }

        public RelayCommand SearchGameObjects => new(OnSearchGameObjects);
        public RelayCommand AddComponent => new(OnAddComponent);
        public RelayCommand PasteInstanceAsComponent => new(OnPasteInstanceAsComponent);

        public override void PostRead()
        {
            PfbFile.SetupGameObjects();
        }

        public override IEnumerable<object> TreeViewItems
        {
            get
            {
                yield return new TreeItemViewModel("GameObjects", GameObjects);
            }
        }

        private void OnSearchGameObjects(object arg)
        {
            SearchGameObjectList ??= new();
            SearchGameObjectList.Clear();
            GameObjectFilter filter = new(GameObjectSearchViewModel);
            if (!filter.Enable) return;
            if (PfbFile.GameObjectDatas == null) return;
            foreach (var gameObject in PfbFile.IterAllGameObjects(GameObjectSearchViewModel.IncludeChildren))
            {
                if (filter.IsMatch(gameObject))
                {
                    SearchGameObjectList.Add(gameObject);
                }
            }
        }

        private string lastInputClassName = "";
        private void OnAddComponent(object arg)
        {
            var gameObject = (PfbFile.GameObjectData)arg;
            Views.InputDialog dialog = new()
            {
                Title = Texts.NewItem,
                Message = Texts.InputClassName,
                InputText = lastInputClassName,
                Owner = Application.Current.MainWindow,
            };
            if (dialog.ShowDialog() != true) return;
            lastInputClassName = dialog.InputText;
            if (string.IsNullOrWhiteSpace(lastInputClassName))
            {
                MessageBoxUtils.Error("ClassName is empty");
                return;
            }
            AppUtils.TryActionSimple(() =>
            {
                PfbFile.AddComponent(gameObject, lastInputClassName);
                Changed = true;
            });
        }

        private void OnPasteInstanceAsComponent(object arg)
        {
            var gameObject = (PfbFile.GameObjectData)arg;
            if (CopiedInstance != null && File.GetRSZ() is RSZFile rsz)
            {
                RszInstance component = rsz.CloneInstance(CopiedInstance);
                PfbFile.AddComponent(gameObject, component);
                Changed = true;
            }
        }
    }


    public class ScnFileViewModel(ScnFile file) : BaseRszFileViewModel
    {
        public ScnFile ScnFile { get; } = file;
        public override BaseRszFile File => ScnFile;
        public RszViewModel RszViewModel => new(ScnFile.RSZ!);
        public ObservableCollection<ScnFile.FolderData>? Folders => ScnFile.FolderDatas;
        public ObservableCollection<ScnFile.GameObjectData>? GameObjects => ScnFile.GameObjectDatas;
        public GameObjectSearchViewModel GameObjectSearchViewModel { get; } = new();
        public ObservableCollection<ScnFile.GameObjectData>? SearchGameObjectList { get; set; }

        public static ScnFile.GameObjectData? CopiedGameObject { get; private set; }

        public override void PostRead()
        {
            ScnFile.SetupGameObjects();
        }

        public override IEnumerable<object> TreeViewItems
        {
            get
            {
                yield return new TreeItemViewModel("Folders", Folders);
                yield return new GameObjectsHeader("GameObjects", GameObjects);
            }
        }

        public RelayCommand CopyGameObject => new(OnCopyGameObject);
        public RelayCommand RemoveGameObject => new(OnRemoveGameObject);
        public RelayCommand DuplicateGameObject => new(OnDuplicateGameObject);
        public RelayCommand PasteGameObject => new(OnPasteGameObject);
        public RelayCommand PasteGameObjectToFolder => new(OnPasteGameObjectToFolder);
        public RelayCommand PasteGameobjectAsChild => new(OnPasteGameobjectAsChild);
        public RelayCommand SearchGameObjects => new(OnSearchGameObjects);
        public RelayCommand AddComponent => new(OnAddComponent);
        public RelayCommand PasteInstanceAsComponent => new(OnPasteInstanceAsComponent);

        /// <summary>
        /// 复制游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private static void OnCopyGameObject(object arg)
        {
            CopiedGameObject = (ScnFile.GameObjectData)arg;
        }

        /// <summary>
        /// 删除游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnRemoveGameObject(object arg)
        {
            ScnFile.RemoveGameObject((ScnFile.GameObjectData)arg);
            Changed = true;
        }

        /// <summary>
        /// 重复游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnDuplicateGameObject(object arg)
        {
            ScnFile.DuplicateGameObject((ScnFile.GameObjectData)arg);
            Changed = true;
        }

        /// <summary>
        /// 粘贴游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteGameObject(object arg)
        {
            if (CopiedGameObject != null)
            {
                ScnFile.ImportGameObject(CopiedGameObject);
                OnPropertyChanged(nameof(GameObjects));
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴游戏对象到文件夹
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteGameObjectToFolder(object arg)
        {
            if (CopiedGameObject != null)
            {
                var folder = (ScnFile.FolderData)arg;
                ScnFile.ImportGameObject(CopiedGameObject, folder);
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴游戏对象到父对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteGameobjectAsChild(object arg)
        {
            if (CopiedGameObject != null)
            {
                var parent = (ScnFile.GameObjectData)arg;
                ScnFile.ImportGameObject(CopiedGameObject, parent: parent);
                Changed = true;
            }
        }

        private void OnSearchGameObjects(object arg)
        {
            SearchGameObjectList ??= new();
            SearchGameObjectList.Clear();
            GameObjectFilter filter = new(GameObjectSearchViewModel);
            if (!filter.Enable) return;
            if (ScnFile.GameObjectDatas == null) return;
            foreach (var gameObject in ScnFile.IterAllGameObjects(GameObjectSearchViewModel.IncludeChildren))
            {
                if (filter.IsMatch(gameObject))
                {
                    SearchGameObjectList.Add(gameObject);
                }
            }
        }

        private string lastInputClassName = "";
        private void OnAddComponent(object arg)
        {
            var gameObject = (ScnFile.GameObjectData)arg;
            Views.InputDialog dialog = new()
            {
                Title = Texts.NewItem,
                Message = Texts.InputClassName,
                InputText = lastInputClassName,
                Owner = Application.Current.MainWindow,
            };
            if (dialog.ShowDialog() != true) return;
            lastInputClassName = dialog.InputText;
            if (string.IsNullOrWhiteSpace(lastInputClassName))
            {
                MessageBoxUtils.Error("ClassName is empty");
                return;
            }
            AppUtils.TryActionSimple(() =>
            {
                ScnFile.AddComponent(gameObject, lastInputClassName);
                Changed = true;
            });
        }

        private void OnPasteInstanceAsComponent(object arg)
        {
            var gameObject = (ScnFile.GameObjectData)arg;
            if (CopiedInstance != null && File.GetRSZ() is RSZFile rsz)
            {
                RszInstance component = rsz.CloneInstance(CopiedInstance);
                ScnFile.AddComponent(gameObject, component);
                Changed = true;
            }
        }
    }


    public class RszViewModel(RSZFile rsz)
    {
        public List<RszInstance> Instances => rsz.InstanceList;

        public IEnumerable<RszInstance> Objects => rsz.ObjectList;
    }
}
