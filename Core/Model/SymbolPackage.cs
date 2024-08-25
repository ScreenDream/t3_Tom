#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.Stats;

// ReSharper disable RedundantNameQualifier

namespace T3.Core.Model;

/// <summary>
/// Base class of essentially what is a read only project.
/// </summary>
/// <remarks>
/// Regarding naming, we consider all t3 operator packages as packages for the sake of consistency with future nuget terminology etc.
/// -- only the user's editable "packages" are referred to as projects
///</remarks>
public abstract partial class SymbolPackage : IResourcePackage
{
    public virtual AssemblyInformation AssemblyInformation { get; }
    public string Folder { get; }
    
    public virtual string DisplayName => AssemblyInformation.Name;

    protected virtual IEnumerable<string> SymbolSearchFiles =>
        Directory.EnumerateFiles(Path.Combine(Folder, SymbolsSubfolder), $"*{SymbolExtension}", SearchOption.AllDirectories);

    public const string SymbolsSubfolder = "Symbols";
    protected event Action<string?, Symbol>? SymbolAdded;
    protected event Action<Symbol>? SymbolUpdated;
    protected event Action<Guid>? SymbolRemoved;

    private static ConcurrentBag<SymbolPackage> _allPackages = [];
    public static IEnumerable<SymbolPackage> AllPackages => _allPackages;

    public string ResourcesFolder { get; private set; } = null!;

    public IReadOnlyCollection<DependencyCounter> Dependencies => (ReadOnlyCollection<DependencyCounter>)DependencyDict.Values;
    protected readonly ConcurrentDictionary<SymbolPackage, DependencyCounter> DependencyDict = new();

    protected virtual ReleaseInfo ReleaseInfo
    {
        get
        {
            if (AssemblyInformation.TryGetReleaseInfo(out var releaseInfo))
                return releaseInfo;
            
            throw new InvalidOperationException($"Failed to get release info for package {AssemblyInformation.Name}");
        }
    }

    static SymbolPackage()
    {
        RenderStatsCollector.RegisterProvider(new OpUpdateCounter());
        RegisterTypes();
    }

    protected SymbolPackage(AssemblyInformation assembly, string? directory = null)
    {
        AssemblyInformation = assembly;
        Folder = directory ?? assembly.Directory;
        lock(_allPackages)
            _allPackages.Add(this);
        
        // ReSharper disable once VirtualMemberCallInConstructor
        InitializeResources(assembly);
    }

    protected virtual void InitializeResources(AssemblyInformation assemblyInformation)
    {
        ResourcesFolder = Path.Combine(Folder, ResourceManager.ResourcesSubfolder);
        Directory.CreateDirectory(ResourcesFolder);
        ResourceManager.AddSharedResourceFolder(this, assemblyInformation.ShouldShareResources);
    }

    public virtual void Dispose()
    {
        ResourceManager.RemoveSharedResourceFolder(this);
        ClearSymbols();
        
        
        var currentPackages = _allPackages.ToList();
        currentPackages.Remove(this);
        lock (_allPackages)
            _allPackages = new ConcurrentBag<SymbolPackage>(currentPackages);
        
        AssemblyInformation.Unload();
        // Todo - symbol instance destruction...?
    }

    private void ClearSymbols()
    {
        if (SymbolDict.Count == 0)
            return;

        var symbols = SymbolDict.Values.ToArray();
        foreach (var symbol in symbols)
        {
            var id = symbol.Id;
            SymbolDict.Remove(id, out _);
        }
    }

    public void LoadSymbols(bool parallel, out List<SymbolJson.SymbolReadResult> newlyRead, out List<Symbol> allNewSymbols)
    {
        Log.Debug($"{AssemblyInformation.Name}: Loading symbols...");

        ConcurrentDictionary<Guid, Type> newTypes = new();

        var removedSymbolIds = new HashSet<Guid>(SymbolDict.Keys);
        List<Symbol> updatedSymbols = new();

        if (parallel)
        {
            AssemblyInformation.OperatorTypeInfo
                               .AsParallel()
                               .ForAll(kvp => LoadTypes(kvp.Key, kvp.Value.Type, newTypes));
        }
        else
        {
            foreach (var (guid, type) in AssemblyInformation.OperatorTypeInfo)
            {
                LoadTypes(guid, type.Type, newTypes);
            }
        }

        foreach (var symbol in updatedSymbols)
        {
            UpdateSymbolInstances(symbol);
            SymbolUpdated?.Invoke(symbol);
        }

        if (SymbolRemoved != null)
        {
            // remaining symbols have been removed from the assembly
            foreach (var symbolId in removedSymbolIds)
            {
                SymbolRemoved.Invoke(symbolId);
            }
        }

        newlyRead = [];
        allNewSymbols = [];
        
        if (newTypes.Count > 0)
        {
            var searchFileEnumerator = parallel ? SymbolSearchFiles.AsParallel() : SymbolSearchFiles;
            var symbolsRead = searchFileEnumerator
                             .Select(JsonFileResult<Symbol>.ReadAndCreate)
                             .Select(result =>
                                     {
                                         if (!newTypes.TryGetValue(result.Guid, out var type))
                                             return default;

                                         return ReadSymbolFromJsonFileResult(result, type);
                                     })
                             .Where(symbolReadResult => symbolReadResult.Result.Symbol is not null)
                             .ToArray();

            Log.Debug($"{AssemblyInformation.Name}: Registering loaded symbols...");

            foreach (var readSymbolResult in symbolsRead)
            {
                var symbol = readSymbolResult.Result.Symbol;
                var id = symbol.Id;

                if (!SymbolDict.TryAdd(id, symbol))
                {
                    Log.Error($"Can't load symbol for [{symbol.Name}]. Registry already contains id {symbol.Id}: [{SymbolDict[symbol.Id].Name}]");
                    continue;
                }

                newlyRead.Add(readSymbolResult.Result);
                newTypes.Remove(id, out _);
                allNewSymbols.Add(symbol);

                SymbolAdded?.Invoke(readSymbolResult.Path, symbol);
            }
        }

        // these do not have a file
        foreach (var (guid, newType) in newTypes)
        {
            var symbol = CreateSymbol(newType, guid);

            var id = symbol.Id;
            if (!SymbolDict.TryAdd(id, symbol))
            {
                Log.Error($"{AssemblyInformation.Name}: Ignoring redefinition symbol {symbol.Name}.");
                continue;
            }

            //Log.Debug($"{AssemblyInformation.Name}: new added symbol: {newType}");

            allNewSymbols.Add(symbol);

            SymbolAdded?.Invoke(null, symbol);
        }

        return;

        void LoadTypes(Guid guid, Type type, ConcurrentDictionary<Guid, Type> newTypesDict)
        {
            if (SymbolDict.TryGetValue(guid, out var symbol))
            {
                removedSymbolIds.Remove(guid);

                symbol.UpdateTypeWithoutUpdatingDefinitionsOrInstances(type, this);
                updatedSymbols.Add(symbol);
            }
            else
            {
                // it's a new type!!
                newTypesDict.TryAdd(guid, type);
            }
        }

        SymbolJsonResult ReadSymbolFromJsonFileResult(JsonFileResult<Symbol> jsonInfo, Type type)
        {
            var result = SymbolJson.ReadSymbolRoot(jsonInfo.Guid, jsonInfo.JToken, type, this);

            jsonInfo.Object = result.Symbol;
            return new SymbolJsonResult(result, jsonInfo.FilePath);
        }
    }

    protected static void UpdateSymbolInstances(Symbol symbol)
    {
        symbol.UpdateInstanceType();
        symbol.CreateAnimationUpdateActionsForSymbolInstances();
    }

    protected internal Symbol CreateSymbol(Type instanceType, Guid id)
    {
        return new Symbol(instanceType, id, this);
    }


    public void ApplySymbolChildren(List<SymbolJson.SymbolReadResult> symbolsRead)
    {
        Log.Debug($"{AssemblyInformation.Name}: Applying symbol children...");
        Parallel.ForEach(symbolsRead, result => TryReadAndApplyChildren(result));
        Log.Debug($"{AssemblyInformation.Name}: Done applying symbol children.");
    }

    protected static bool TryReadAndApplyChildren(SymbolJson.SymbolReadResult result)
    {
        if (!SymbolJson.TryReadAndApplySymbolChildren(result))
        {
            Log.Error($"Problem obtaining children of {result.Symbol.Name} ({result.Symbol.Id})");
            return false;
        }

        return true;
    }

    public readonly record struct SymbolJsonResult(in SymbolJson.SymbolReadResult Result, string Path);

    public IReadOnlyDictionary<Guid, Symbol> Symbols => SymbolDict;
    protected readonly ConcurrentDictionary<Guid, Symbol> SymbolDict = new();

    public const string SymbolExtension = ".t3";

    public bool ContainsSymbolName(string newSymbolName, string symbolNamespace)
    {
        foreach (var existing in SymbolDict.Values)
        {
            if (existing.Name == newSymbolName && existing.Namespace == symbolNamespace)
                return true;
        }

        return false;
    }

    public virtual ResourceFileWatcher? FileWatcher => null;
    public string Alias
    {
        get
        {
            if (ReleaseInfo.HomeGuid == Guid.Empty)
            {
                return AssemblyInformation.Name;
            }

            // hacky way of getting the home namespace
            var typeInfo = AssemblyInformation.OperatorTypeInfo[ReleaseInfo.HomeGuid].Type;
            return typeInfo.Namespace! + "." + typeInfo.Name; // alias is fully-qualified project name
        }
    }

    public virtual bool IsReadOnly => true;

    internal bool TryGetSymbol(Guid symbolId, [NotNullWhen(true)] out Symbol? symbol) => SymbolDict.TryGetValue(symbolId, out symbol);

    public void AddResourceDependencyOn(FileResource resource)
    {
        if (!TryGetDependencyCounter(resource, out var dependencyCount))
            return;

        dependencyCount.ResourceCount++;
    }

    public void RemoveResourceDependencyOn(FileResource fileResource)
    {
        if (!TryGetDependencyCounter(fileResource, out var dependency))
            return;
        
        dependency.ResourceCount--;

        RemoveIfNoRemainingReferences(dependency);
    }

    public void AddDependencyOn(Symbol symbol)
    {
        if(symbol.SymbolPackage == this)
            return;
        
        if (!TryGetDependencyCounter(symbol, out var dependency))
            return;
        
        dependency.SymbolChildCount++;
    }
    
    public void RemoveDependencyOn(Symbol symbol)
    {
        if (symbol.SymbolPackage == this)
            return;
        
        if (!TryGetDependencyCounter(symbol, out var dependency))
            return;
        
        dependency.SymbolChildCount--;
        RemoveIfNoRemainingReferences(dependency);
    }

    private void RemoveIfNoRemainingReferences(DependencyCounter dependency)
    {
        if (dependency.SymbolChildCount == 0 && dependency.ResourceCount == 0)
        {
            DependencyDict.Remove((SymbolPackage)dependency.Package, out _);
        }
    }

    private bool TryGetDependencyCounter(IResource fileResource, [NotNullWhen(true)] out DependencyCounter? dependencyCounter)
    {
        var owningPackage = fileResource.OwningPackage;
        if (owningPackage is not SymbolPackage symbolPackage)
        {
            dependencyCounter = null;
            return false;
        }

        if (DependencyDict.TryGetValue(symbolPackage, out dependencyCounter)) 
            return true;
        
        dependencyCounter = new DependencyCounter
                                {
                                    Package = symbolPackage
                                };
        DependencyDict.TryAdd(symbolPackage, dependencyCounter);

        return true;
    }

    protected virtual void OnDependenciesChanged()
    {
    }
}

public record DependencyCounter
{
    public IResourcePackage Package { get; init; }
    public int SymbolChildCount { get; internal set; }
    public int ResourceCount { get; internal set; }
    
    public override string ToString()
    {
        return $"{Package.DisplayName}: Symbol References: {SymbolChildCount}, Resource References: {ResourceCount}";
    }
}