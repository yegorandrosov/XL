namespace XL.API.Models
{
    public record NotFound;

    public record CommandHelperClasses<T>(T Value);

    public record Unprocessable;

    public static class CommandResponseExtensions
    {
        public static CommandHelperClasses<T> Success<T>(this T item)
        {
            return new CommandHelperClasses<T>(item);
        }
    }
}
