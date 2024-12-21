using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Vatsim.Vatis.Ui;

public abstract partial class ReactiveViewModelBase : ReactiveObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> mErrors = [];

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => mErrors.Count != 0;

    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
    {
        return (propertyName is null ? null : GetErrors(propertyName)) ?? Array.Empty<object>();
    }

    private IEnumerable? GetErrors(string propertyName)
    {
        return mErrors.TryGetValue(propertyName, out var v) ? v : null;
    }

    public void RaiseError(string propertyName, string error)
    {
        if (!mErrors.TryGetValue(propertyName, out List<string>? value))
        {
            value = [];
            mErrors[propertyName] = value;
        }

        value.Add(error);
        OnErrorsChanged(propertyName);
    }

    public void ClearAllErrors()
    {
        var propertyNames = new List<string>(mErrors.Keys);
        mErrors.Clear();

        foreach (var propertyName in propertyNames)
        {
            OnErrorsChanged(propertyName);
        }
    }

    public void ClearErrors(string propertyName)
    {
        mErrors.Remove(propertyName);
        OnErrorsChanged(propertyName);
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
