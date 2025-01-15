// <copyright file="ReactiveViewModelBase.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ReactiveUI;

namespace Vatsim.Vatis.Ui;

/// <summary>
/// Serves as a base class for reactive view models that implement property change notifications
/// and error tracking. Inherits from <see cref="ReactiveObject"/> and implements <see cref="INotifyDataErrorInfo"/>.
/// </summary>
public abstract class ReactiveViewModelBase : ReactiveObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    /// <summary>
    /// Occurs when the errors associated with a specific property have changed.
    /// Implemented as part of the <see cref="INotifyDataErrorInfo"/> interface.
    /// </summary>
    /// <exception cref="DataErrorsChangedEventArgs">
    /// Raised with information about the property whose errors have changed.
    /// </exception>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Gets a value indicating whether the view model contains validation errors.
    /// Implemented as part of the <see cref="INotifyDataErrorInfo"/> interface.
    /// </summary>
    public bool HasErrors => _errors.Count != 0;

    /// <summary>
    /// Adds an error message to the collection of errors for a specified property and triggers the ErrorsChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property for which the error is being added.</param>
    /// <param name="error">The error message to associate with the specified property.</param>
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

    /// <summary>
    /// Clears all errors from the errors collection and triggers the ErrorsChanged event for all affected properties.
    /// </summary>
    public void ClearAllErrors()
    {
        var propertyNames = new List<string>(_errors.Keys);
        _errors.Clear();

        foreach (var propertyName in propertyNames)
        {
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    /// Clears all errors associated with the specified property and triggers the ErrorsChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property for which the errors are to be cleared.</param>
    public void ClearErrors(string propertyName)
    {
        _errors.Remove(propertyName);
        OnErrorsChanged(propertyName);
    }

    /// <summary>
    /// Gets the validation errors for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve validation errors for.</param>
    /// <returns>An <see cref="IEnumerable"/> containing the validation errors for the specified property. If the property name is null, returns an empty collection.</returns>
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
    {
        return (propertyName is null ? null : GetErrors(propertyName)) ?? Array.Empty<object>();
    }

    /// <summary>
    /// Gets the validation errors for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve validation errors for.</param>
    /// <returns>An <see cref="IEnumerable"/> containing the validation errors for the specified property, or null if no errors are associated with the property.</returns>
    private IEnumerable? GetErrors(string propertyName)
    {
        return _errors.GetValueOrDefault(propertyName);
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
