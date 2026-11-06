using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace EducationAPI.Extensions;

// HTTP form numbers use a decimal point regardless of the server's OS culture.
internal sealed class InvariantFormValueProviderFactory : IValueProviderFactory
{
    public async Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        var request = context.ActionContext.HttpContext.Request;

        if (!request.HasFormContentType) return;

        try
        {
            var form = await request.ReadFormAsync();

            context.ValueProviders.Add(new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture));
        }
        catch (InvalidDataException exception)
        {
            throw new ValueProviderException(exception.Message, exception);
        }
        catch (IOException exception)
        {
            throw new ValueProviderException(exception.Message, exception);
        }
    }
}

internal sealed class FormDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = context.Metadata.UnderlyingOrModelType;

        if (type != typeof(decimal)) 
            return null;

        // Reject comma/group separators instead of silently turning 125,50 into 12550.
        return new DecimalModelBinder(NumberStyles.Float,
            context.Services.GetRequiredService<ILoggerFactory>());
    }
}
