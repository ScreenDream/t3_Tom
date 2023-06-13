using System.Linq;
using System.Text.RegularExpressions;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Compilation;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Windows;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace T3.Editor.Gui.Graph.Modification
{
    internal static class GraphOperations
    {
        public static SymbolChildUi AddSymbolChild(Symbol symbol, Symbol parent, Vector2 positionOnCanvas)
        {
            var addCommand = new AddSymbolChildCommand(parent, symbol.Id) { PosOnCanvas = positionOnCanvas };
            UndoRedoStack.AddAndExecute(addCommand);
            var newSymbolChild = parent.Children.Single(entry => entry.Id == addCommand.AddedChildId);

            // Select new node
            var symbolUi = SymbolUiRegistry.Entries[parent.Id];
            var childUi = symbolUi.ChildUis.Find(s => s.Id == newSymbolChild.Id);

            return childUi;
        }

        /// <summary>
        /// Updates symbol definition, instances and symbolUi if modification to operator source code
        /// was detected by Resource file hook.
        /// </summary>
        public static void UpdateChangedOperators()
        {
            var modifiedSymbols = OperatorResource.UpdateChangedOperatorTypes();
            foreach (var symbol in modifiedSymbols)
            {
                UiModel.UpdateUiEntriesForSymbol(symbol);
                symbol.CreateAnimationUpdateActionsForSymbolInstances();
            }
        }

        public static void RenameNameSpaces(NamespaceTreeNode node, string nameSpace)
        {
            var orgNameSpace = node.GetAsString();
            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                if (!symbol.Namespace.StartsWith(orgNameSpace))
                    continue;

                //var newNameSpace = parent + "."
                var newNameSpace = Regex.Replace(symbol.Namespace, orgNameSpace, nameSpace);
                Log.Debug($" Changing namespace of {symbol.Name}: {symbol.Namespace} -> {newNameSpace}");
                symbol.Namespace = newNameSpace;
            }
        }

        public static bool UpdateSymbolWithNewSource(Symbol symbol, string newSource)
        {
            var newAssembly = OperatorUpdating.CompileSymbolFromSource(newSource, symbol.Name);
            if (newAssembly == null)
                return false;

            //string path = @"Operators\Types\" + symbol.Name + ".cs";
            var sourcePath = Model.BuildFilepathForSymbol(symbol, Model.SourceExtension);

            var operatorResource = ResourceManager.Instance().GetOperatorFileResource(sourcePath);
            if (operatorResource != null)
            {
                operatorResource.OperatorAssembly = newAssembly;
                operatorResource.Updated = true;
                symbol.PendingSource = newSource;
                return true;
            }

            Log.Error($"Could not update symbol '{symbol.Name}' because its file resource couldn't be found.");

            return false;
        }
    }
}