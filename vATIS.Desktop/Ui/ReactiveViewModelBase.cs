using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Vatsim.Vatis.Ui;

public abstract class ReactiveViewModelBase : ReactiveObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => _errors.Count != 0;

    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
    {
        return (propertyName is null ? null : GetErrors(propertyName)) ?? Array.Empty<object>();
    }

    private IEnumerable? GetErrors(string propertyName)
    {
        return _errors.GetValueOrDefault(propertyName);
    }

    public void RaiseError(string propertyName, string error)
    {
        if (!_errors.TryGetValue(propertyName, out List<string>? value))
        {
            value = [];
            _errors[propertyName] = value;
        }

        value.Add(error);
        OnErrorsChanged(propertyName);
    }

    public void ClearAllErrors()
    {
        var propertyNames = new List<string>(_errors.Keys);
        _errors.Clear();

        foreach (var propertyName in propertyNames)
        {
            OnErrorsChanged(propertyName);
        }
    }

    public void ClearErrors(string propertyName)
    {
        _errors.Remove(propertyName);
        OnErrorsChanged(propertyName);
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
