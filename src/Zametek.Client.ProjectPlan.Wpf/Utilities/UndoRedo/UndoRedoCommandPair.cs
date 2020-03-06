using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Zametek.Client.ProjectPlan.Wpf
{
    public class UndoRedoCommandPair
    {
        public UndoRedoCommandPair(
            ICommand undoCommand,
            object undoParameter,
            ICommand redoCommand,
            object redoParameter)
        {
            UndoCommand = undoCommand ?? throw new ArgumentNullException(nameof(undoCommand));
            UndoParameter = undoParameter;
            RedoCommand = redoCommand ?? throw new ArgumentNullException(nameof(redoCommand));
            RedoParameter = redoParameter;
        }

        public ICommand UndoCommand { get; }

        public object UndoParameter { get; }

        public ICommand RedoCommand { get; }

        public object RedoParameter { get; }
    }
}
