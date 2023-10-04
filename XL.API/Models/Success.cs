namespace XL.API.Models
{
    public record NotFound;

    public record AlreadyExists;

    public record AlreadyExists<T>(T Value);

    public record Success;

    public record Success<T>(T Value);

    public record Unprocessable;

    public static class CommandResponseExtensions
    {
        public static Success<T> Success<T>(this T item)
        {
            return new Success<T>(item);
        }

        
        public static AlreadyExists<T> AlreadyExists<T>(this T item)
        {
            return new AlreadyExists<T>(item);
        }
    }
}
