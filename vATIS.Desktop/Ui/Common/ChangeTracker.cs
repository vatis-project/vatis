// <copyright file="ChangeTracker.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace Vatsim.Vatis.Ui.Common;

/// <summary>
/// Provides functionality to track changes to property values and determine if unsaved changes exist.
/// </summary>
public class ChangeTracker : ReactiveObject
{
    private readonly ConcurrentDictionary<string, (object? OriginalValue, object? CurrentValue)> _fieldHistory = [];
    private readonly BehaviorSubject<bool> _hasUnsavedChangesSubject = new(false);
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Gets an observable stream that emits the current status of unsaved changes.
    /// </summary>
    /// <remarks>
    /// Subscribers will receive updates whenever there is a change to the tracked properties
    /// and whether any changes have been made that are not yet saved.
    /// </remarks>
    public IObservable<bool> HasUnsavedChangesObservable => _hasUnsavedChangesSubject.AsObservable();

    private bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Applies any unsaved changes by marking current values as the new original values,
    /// and resets the <see cref="HasUnsavedChanges"/> flag.
    /// </summary>
    /// <returns><c>true</c> if any changes were applied; otherwise, <c>false</c>.</returns>
    public bool ApplyChangesIfNeeded()
    {
        if (!HasUnsavedChanges)
            return false;

        foreach (var key in _fieldHistory.Keys.ToList())
        {
            if (_fieldHistory.TryGetValue(key, out var entry))
            {
                _fieldHistory[key] = (entry.CurrentValue, entry.CurrentValue);
            }
        }

        CheckForUnsavedChanges();
        return true;
    }

    /// <summary>
    /// Tracks a property change by comparing the current value to the original.
    /// </summary>
    /// <param name="propertyName">The name of the property being tracked.</param>
    /// <param name="currentValue">The current value of the property.</param>
    public void TrackChange(string propertyName, object? currentValue)
    {
        _fieldHistory.AddOrUpdate(
            propertyName,
            (currentValue, currentValue), // Value if the key doesn't exist
            (_, existing) => (existing.OriginalValue, currentValue) // Value if the key already exists
        );

        CheckForUnsavedChanges();
    }

    /// <summary>
    /// Resets all tracked changes and clears the change history.
    /// </summary>
    public void ResetChanges()
    {
        _fieldHistory.Clear();
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Compares two values to determine if they are equal.
    /// </summary>
    /// <param name="originalValue">The original value of the property.</param>
    /// <param name="currentValue">The current value of the property.</param>
    /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
    private static bool AreValuesEqual(object? originalValue, object? currentValue)
    {
        if (originalValue == null && currentValue == null)
            return true;

        if (originalValue == null || currentValue == null)
            return false;

        if (originalValue is string s1 && currentValue is string s2)
            return s1 == s2;

        if (originalValue is int i1 && currentValue is int i2)
            return i1 == i2;

        if (originalValue is bool b1 && currentValue is bool b2)
            return b1 == b2;

        if (originalValue is IEnumerable<object> e1 && currentValue is IEnumerable<object> e2)
            return e1.SequenceEqual(e2);

        return originalValue.Equals(currentValue);
    }

    /// <summary>
    /// Checks if there are any changes between original and current values.
    /// </summary>
    private void CheckForUnsavedChanges()
    {
        HasUnsavedChanges = _fieldHistory.Any(entry =>
            !AreValuesEqual(entry.Value.OriginalValue, entry.Value.CurrentValue));

        _hasUnsavedChangesSubject.OnNext(HasUnsavedChanges);
    }
}
