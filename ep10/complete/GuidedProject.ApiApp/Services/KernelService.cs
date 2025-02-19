namespace GuidedProject.ApiApp.Services;

public interface IKernelService
{
    IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt);
}

public class KernelService : IKernelService
{
    private readonly static string loremipsum = """
        Lorem ipsum dolor sit amet, consectetur adipiscing elit.
        Vestibulum sed est nisi. Etiam scelerisque ligula nec nisl elementum,
        ac gravida tellus vestibulum. Vestibulum sed euismod elit.
        Phasellus condimentum mi vitae efficitur viverra.
        Proin ac fermentum nunc.
        Vestibulum in neque vulputate, tincidunt ex id, faucibus risus.
        Sed viverra, sem a ultricies bibendum, velit sapien cursus quam,
        ornare tristique tortor ante sed ipsum.
        Donec consectetur, felis quis scelerisque varius, tortor neque hendrerit lacus,
        eu vestibulum elit neque tempus lacus.
        Donec vitae risus id diam volutpat ornare sit amet sed est.
        Fusce cursus finibus leo quis commodo.
        """;

    public async IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt)
    {
        var result = loremipsum.Split([ " ", "\r", "\n" ], StringSplitOptions.RemoveEmptyEntries);

        foreach (var text in result)
        {
            yield return text;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
