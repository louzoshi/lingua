namespace Lingua.ViewModels;

/// <summary>
/// Envelope padrão das respostas da API.
/// <para>
/// Use <see cref="Success"/> e <see cref="Fail(string)"/> em vez dos construtores: quando
/// T é <c>string</c>, um <c>new ResultViewModel&lt;string&gt;("texto")</c> não deixa claro
/// se o texto é o dado ou o erro, e o compilador escolhe em silêncio.
/// </para>
/// </summary>
public class ResultViewModel<T>
{
    public ResultViewModel(T? data, List<string>? errors = null)
    {
        Data = data;
        Errors = errors ?? new List<string>();
    }

    public T? Data { get; private set; }

    public List<string> Errors { get; private set; } = new();

    public static ResultViewModel<T> Success(T? data)
        => new(data);

    public static ResultViewModel<T> Fail(string error)
        => new(default, new List<string> { error });

    public static ResultViewModel<T> Fail(List<string> errors)
        => new(default, errors);
}
