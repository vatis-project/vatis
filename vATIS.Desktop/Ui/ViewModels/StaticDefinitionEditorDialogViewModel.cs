// <copyright file="StaticDefinitionEditorDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using ReactiveUI;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Common;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the ViewModel for the Static Definition Editor Dialog.
/// Provides functionality for editing static definitions with data-binding support.
/// </summary>
public class StaticDefinitionEditorDialogViewModel : ReactiveViewModelBase, IDisposable, ISpokenTextSource
{
    private TextDocument? _textDocument = new();
    private List<ICompletionData> _contractionCompletionData = [];
    private DialogResult _dialogResult;
    private string? _title = "Definition Editor";
    private string? _dataValidation;
    private string? _separatorAfter;
    private string? _condition;
    private ActiveRunways? _runways;
    private string? _conditionError;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticDefinitionEditorDialogViewModel"/> class.
    /// </summary>
    public StaticDefinitionEditorDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(window => window.Close());
        SaveButtonCommand = ReactiveCommand.Create<ICloseable>(HandleSaveButton);
    }

    /// <summary>
    /// Occurs when the dialog result changes. This event is triggered to notify subscribers about updates
    /// to the <see cref="DialogResult"/> property.
    /// </summary>
    public event EventHandler<DialogResult>? DialogResultChanged;

    /// <summary>
    /// Gets the command executed when the cancel button is clicked. This command triggers the close operation
    /// for an implementing <see cref="ICloseable"/> target.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    /// <summary>
    /// Gets the command executed when the save button is clicked. This command initiates
    /// the saving operation and closes the associated <see cref="ICloseable"/> target.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> SaveButtonCommand { get; }

    /// <summary>
    /// Gets or sets a value indicating the result of a dialog operation.
    /// </summary>
    public DialogResult DialogResult
    {
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    /// <summary>
    /// Gets or sets the title of the editor dialog.
    /// </summary>
    public string? Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the definition text.
    /// </summary>
    public string? DefinitionText
    {
        get => _textDocument?.Text;
        set => TextDocument = new TextDocument(value);
    }

    /// <summary>
    /// Gets or sets the instance of the <see cref="AvaloniaEdit.Document.TextDocument"/> associated with this view model.
    /// This document represents the editable text content used in the dialog.
    /// </summary>
    public TextDocument? TextDocument
    {
        get => _textDocument;
        set => this.RaiseAndSetIfChanged(ref _textDocument, value);
    }

    /// <summary>
    /// Gets or sets the runways of the station's presets, suggested while a condition is typed.
    /// </summary>
    public ActiveRunways? Runways
    {
        get => _runways;
        set => this.RaiseAndSetIfChanged(ref _runways, value);
    }

    /// <summary>
    /// Gets or sets the list of completion data used for contraction suggestions in the editor.
    /// This allows the editor to provide relevant completion suggestions when editing definitions.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets the source that provides the built-in contractions and spoken text for the editor.
    /// </summary>
    public ISpokenTextSource? SpokenTextSource { get; set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> BuiltInContractions =>
        SpokenTextSource?.BuiltInContractions ?? new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets an optional separator placed after this definition. Blank uses the default separator.
    /// </summary>
    public string? SeparatorAfter
    {
        get => _separatorAfter;
        set => this.RaiseAndSetIfChanged(ref _separatorAfter, value);
    }

    /// <summary>
    /// Gets or sets the condition, as text, e.g. <c>EPKK_E_APP AND dep:07 AND ceiling &lt; 200ft</c>. It can test
    /// controllers online, time, weather and runways.
    /// </summary>
    public string? Condition
    {
        get => _condition;
        set
        {
            this.RaiseAndSetIfChanged(ref _condition, value);
            ConditionError = string.IsNullOrWhiteSpace(value) || ConditionExpression.TryParse(value, out _, out var error)
                ? null
                : error;
        }
    }

    /// <summary>
    /// Gets the problem with the condition as typed, or null if it is empty or valid.
    /// </summary>
    public string? ConditionError
    {
        get => _conditionError;
        private set
        {
            this.RaiseAndSetIfChanged(ref _conditionError, value);
            this.RaisePropertyChanged(nameof(HasConditionError));
        }
    }

    /// <summary>
    /// Gets a value indicating whether the condition has a problem.
    /// </summary>
    public bool HasConditionError => _conditionError != null;

    /// <summary>
    /// Gets or sets the validation message associated with data input. This property provides feedback
    /// or error messages related to the data validation process.
    /// </summary>
    public string? DataValidation
    {
        get => _dataValidation;
        set => this.RaiseAndSetIfChanged(ref _dataValidation, value);
    }

    /// <summary>
    /// Loads the condition of an existing definition.
    /// </summary>
    /// <param name="definition">The definition.</param>
    public void LoadConditionsFrom(StaticDefinition definition)
    {
        Condition = definition.Condition;
    }

    /// <summary>
    /// Copies the condition being edited onto a definition.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The same definition.</returns>
    public StaticDefinition ApplyConditionsTo(StaticDefinition definition)
    {
        definition.Condition = string.IsNullOrWhiteSpace(Condition) ? null : Condition.Trim();
        return definition;
    }

    /// <summary>
    /// Checks the condition.
    /// </summary>
    /// <returns>True if the condition is valid; otherwise, false.</returns>
    /// <remarks>
    /// A problem is not raised as an error here because it is already shown next to the field, so the caller must not
    /// save when this returns false.
    /// </remarks>
    public bool ValidateConditions()
    {
        return !HasErrors && !HasConditionError;
    }

    /// <inheritdoc/>
    public string? GetSpokenText(string token)
    {
        return SpokenTextSource?.GetSpokenText(token);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DialogResultChanged = null;
        CancelButtonCommand.Dispose();
        SaveButtonCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleSaveButton(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        DialogResult = DialogResult.Ok;
        if (!HasErrors && !HasConditionError)
        {
            window.Close();
        }
    }
}
