namespace FastEndpoints;

//NOTE: CommandHandlerExecutor<> class is singleton
//      (cached in CommandHandlerDefinition.HandlerExecutor property)
interface ICommandHandlerExecutor<TResult>
{
    Task<TResult> Execute(ICommand<TResult> command, Type handlerType, CancellationToken ct);
}

sealed class CommandHandlerExecutor<TCommand, TResult>(IEnumerable<ICommandMiddleware<TCommand, TResult>> m, ICommandReceiver<TCommand>? commandReceiver = null)
    : ICommandHandlerExecutor<TResult> where TCommand : ICommand<TResult>
{
    internal ICommandHandler<TCommand, TResult>? TestHandler { get; init; }

    readonly Type[] _tMiddlewares = m.Select(x => x.GetType()).ToArray();

    public Task<TResult> Execute(ICommand<TResult> command, Type tCommandHandler, CancellationToken ct)
    {
        commandReceiver?.AddCommand((TCommand)command);

        var cmdHandler = TestHandler ?? CreateHandler(tCommandHandler);

        return InvokeMiddleware(0);

        Task<TResult> InvokeMiddleware(int index)
        {
            return index == _tMiddlewares.Length
                       ? cmdHandler.ExecuteAsync((TCommand)command, ct)
                       : ((ICommandMiddleware<TCommand, TResult>)ServiceResolver.Instance.CreateInstance(_tMiddlewares[index])).ExecuteAsync(
                           (TCommand)command,
                           () => InvokeMiddleware(index + 1),
                           ct);
        }
    }

    ICommandHandler<TCommand, TResult> CreateHandler(Type tCommandHandler)
    {
        var tCommand = typeof(TCommand);

        // For generic command handlers, try AOT-safe factory first
        if (tCommandHandler.IsGenericType && !tCommandHandler.IsGenericTypeDefinition)
        {
            var tOpenHandler = tCommandHandler.GetGenericTypeDefinition();

            // Try to create the handler using the source-generated factory
            if (GenericTypeRegistryProvider.TryCreateClosedGenericCommandHandler(tOpenHandler, tCommand, out var handlerInstance) &&
                handlerInstance is not null)
            {
                return (ICommandHandler<TCommand, TResult>)handlerInstance;
            }
        }

        // Fall back to reflection-based creation (will fail in AOT if type not preserved)
        return (ICommandHandler<TCommand, TResult>)ServiceResolver.Instance.CreateInstance(tCommandHandler);
    }
}