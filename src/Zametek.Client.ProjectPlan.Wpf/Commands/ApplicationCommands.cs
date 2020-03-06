using Prism.Commands;

namespace Zametek.Client.ProjectPlan.Wpf
{
    public class ApplicationCommands
        : IApplicationCommands
    {
        public CompositeCommand UndoCommand { get; } = new CompositeCommand(true);
    }
}
