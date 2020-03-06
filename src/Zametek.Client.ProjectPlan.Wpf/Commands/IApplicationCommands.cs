using Prism.Commands;

namespace Zametek.Client.ProjectPlan.Wpf
{
    public interface IApplicationCommands
    {
        CompositeCommand UndoCommand { get; }
    }
}
