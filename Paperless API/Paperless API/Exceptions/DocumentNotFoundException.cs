namespace Paperless_API.Exceptions
{
    public class DocumentNotFoundException : Exception
    {
        public DocumentNotFoundException(Guid id)
        : base($"Document with id {id} not found.") { }
    }
}
