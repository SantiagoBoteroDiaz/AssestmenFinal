namespace assestment.Response;

public class SystemResponse <T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}
