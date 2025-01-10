using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ReactiveUI;

namespace Vatsim.Vatis.Ui;

public abstract class ReactiveViewModelBase : ReactiveObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => this._errors.Count != 0;

    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
    {
        return (propertyName is null ? null : this.GetErrors(propertyName)) ?? Array.Empty<object>();
    }

    private IEnumerable? GetErrors(string propertyName)
    {
        return this._errors.GetValueOrDefault(propertyName);
    }

    public void RaiseError(string propertyName, string error)
    {
        if (!this._errors.TryGetValue(propertyName, out List<string>? value))
        {
            value = [];
            this._errors[propertyName] = value;
        }

        value.Add(error);
        this.OnErrorsChanged(propertyName);
    }

    public void ClearAllErrors()
    {
        var propertyNames = new List<string>(this._errors.Keys);
        this._errors.Clear();

        foreach (var propertyName in propertyNames)
        {
            this.OnErrorsChanged(propertyName);
        }
    }

    public void ClearErrors(string propertyName)
    {
        this._errors.Remove(propertyName);
        this.OnErrorsChanged(propertyName);
    }

    private void OnErrorsChanged(string propertyName)
    {
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}