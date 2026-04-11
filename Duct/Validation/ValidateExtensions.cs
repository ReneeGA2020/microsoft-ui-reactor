using Duct.Core;
using Duct.Validation.Validators;

namespace Duct.Validation;

/// <summary>
/// Attached validation metadata for an element, stored in Element.Attached.
/// </summary>
public sealed record ValidationAttached(
    string FieldName,
    IValidator[] Validators,
    IAsyncValidator[] AsyncValidators)
{
    public static readonly ValidationAttached Empty = new("", [], []);
}

/// <summary>
/// Fluent extension methods for attaching validation to Duct elements.
/// </summary>
public static class ValidateExtensions
{
    /// <summary>
    /// Attaches validators to this element. The element's value will be validated
    /// against the provided validators, with results pushed to the nearest ValidationContext.
    /// </summary>
    /// <param name="el">The element to validate.</param>
    /// <param name="fieldName">Field name for validation messages.</param>
    /// <param name="validators">One or more validators to apply.</param>
    public static T Validate<T>(this T el, string fieldName, params IValidator[] validators) where T : Element
    {
        var existing = el.GetAttached<ValidationAttached>();
        var merged = existing is not null
            ? existing with
            {
                FieldName = fieldName,
                Validators = [.. existing.Validators, .. validators]
            }
            : new ValidationAttached(fieldName, validators, []);
        return (T)el.SetAttached(merged);
    }

    /// <summary>
    /// Attaches async validators to this element (in addition to any sync validators).
    /// </summary>
    public static T ValidateAsync<T>(this T el, string fieldName, params IAsyncValidator[] asyncValidators) where T : Element
    {
        var existing = el.GetAttached<ValidationAttached>();
        var merged = existing is not null
            ? existing with
            {
                FieldName = fieldName,
                AsyncValidators = [.. existing.AsyncValidators, .. asyncValidators]
            }
            : new ValidationAttached(fieldName, [], asyncValidators);
        return (T)el.SetAttached(merged);
    }

    /// <summary>
    /// Runs all synchronous validators attached to an element against a value.
    /// Returns the list of validation messages (empty if all pass).
    /// </summary>
    public static IReadOnlyList<ValidationMessage> RunValidators(
        this ValidationAttached attached, object? value)
    {
        var messages = new List<ValidationMessage>();
        foreach (var validator in attached.Validators)
        {
            var result = validator.Validate(value, attached.FieldName);
            if (result is not null)
                messages.Add(result);
        }
        return messages;
    }

    /// <summary>
    /// Runs all async validators attached to an element against a value.
    /// </summary>
    public static async Task<IReadOnlyList<ValidationMessage>> RunAsyncValidators(
        this ValidationAttached attached, object? value, CancellationToken cancellationToken = default)
    {
        var messages = new List<ValidationMessage>();
        foreach (var asyncValidator in attached.AsyncValidators)
        {
            var result = await asyncValidator.ValidateAsync(value, attached.FieldName, cancellationToken);
            if (result is not null)
                messages.Add(result);
        }
        return messages;
    }

    /// <summary>
    /// Gets the ValidationAttached metadata from an element, if any.
    /// </summary>
    public static ValidationAttached? GetValidation<T>(this T el) where T : Element =>
        el.GetAttached<ValidationAttached>();
}
