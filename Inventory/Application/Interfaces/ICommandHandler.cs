namespace Inventory.Application.Interfaces;

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command);
}
