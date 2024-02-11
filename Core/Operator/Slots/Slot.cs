using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator.Interfaces;
using T3.Core.Stats;
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace T3.Core.Operator.Slots
{
    public class Slot<T> : ISlot
    {
        public Guid Id;
        public readonly Type ValueType;
        Type ISlot.ValueType => ValueType;
        public Instance Parent { get => _parent; set => _parent = value; }

        public readonly DirtyFlag DirtyFlag = new();
        
        public T Value;

        protected bool _isDisabled;

        protected virtual void SetDisabled(bool shouldBeDisabled)
        {
            if (shouldBeDisabled == _isDisabled)
                return;

            if (shouldBeDisabled)
            {
                if (_keepOriginalUpdateAction != null)
                {
                    Log.Warning("Is already bypassed or disabled");
                    return;
                }
                
                _keepOriginalUpdateAction = UpdateAction;
                _keepDirtyFlagTrigger = DirtyFlag.Trigger;
                UpdateAction = EmptyAction;
                DirtyFlag.Invalidate();
            }
            else
            {
                RestoreUpdateAction();
            }

            _isDisabled = shouldBeDisabled;
        }
        
        public bool TryGetAsMultiInputTyped(out MultiInputSlot<T> multiInput)
        {
            multiInput = ThisAsMultiInputSlot;
            return IsMultiInput;
        }

        public virtual bool TrySetBypassToInput(Slot<T> targetSlot)
        {
            if (_keepOriginalUpdateAction != null)
            {
                //Log.Warning("Already disabled or bypassed");
                return false;
            }
            
            _keepOriginalUpdateAction = UpdateAction;
            _keepDirtyFlagTrigger = DirtyFlag.Trigger;
            UpdateAction = ByPassUpdate;
            DirtyFlag.Invalidate();
            _targetInputForBypass = targetSlot;
            return true;
        }

        public void OverrideWithAnimationAction(Action<EvaluationContext> newAction)
        {
            // Animation actions are updated regardless if operator was already animated
            if (_keepOriginalUpdateAction == null)
            {
                _keepOriginalUpdateAction = UpdateAction;
                _keepDirtyFlagTrigger = DirtyFlag.Trigger;
            }

            UpdateAction = newAction;
            DirtyFlag.Invalidate();
        }
        
        public virtual void RestoreUpdateAction()
        {
            // This will happen when operators are recompiled and output slots are disconnected
            if (_keepOriginalUpdateAction == null)
            {
                UpdateAction = null;
                return;
            }
            
            UpdateAction = _keepOriginalUpdateAction;
            _keepOriginalUpdateAction = null;
            DirtyFlag.Trigger = _keepDirtyFlagTrigger;
            DirtyFlag.Invalidate();
        }

        public bool IsDisabled 
        {
            get => _isDisabled;
            set => SetDisabled(value);
        }

        // ReSharper disable once StaticMemberInGenericType
        protected static readonly Action<EvaluationContext> EmptyAction = _ => { };

        public Slot()
        {
            // UpdateAction = Update;
            ValueType = typeof(T);
            _valueIsCommand = ValueType == typeof(Command);
            
            if (this is IInputSlot)
            {
                _isInputSlot = true;
            }
        }

        public Slot(T defaultValue) : this()
        {
            Value = defaultValue;
        }
        
        // dummy constructor to initialize input slot values
        // ReSharper disable once UnusedParameter.Local
        protected Slot(bool _) : this()
        {
            _isInputSlot = true;
            if (this is MultiInputSlot<T> multiInputSlot)
            {
                IsMultiInput = true;
                ThisAsMultiInputSlot = multiInputSlot;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(EvaluationContext context)
        {
            if (DirtyFlag.IsDirty || _valueIsCommand)
            {
                OpUpdateCounter.CountUp();
                UpdateAction?.Invoke(context);
                DirtyFlag.Clear();
                DirtyFlag.SetUpdated();
            }
        }

        public void ConnectedUpdate(EvaluationContext context)
        {
            Value = InputConnections[0].GetValue(context);
        }
        
        public void ByPassUpdate(EvaluationContext context)
        {
            Value = _targetInputForBypass.GetValue(context);
        }

        public T GetValue(EvaluationContext context)
        {
            Update(context);

            return Value;
        }

        public void AddConnection(ISlot sourceSlot, int index = 0)
        {
            if (!IsConnected)
            {
                if (UpdateAction != null)
                {
                    _actionBeforeAddingConnecting = UpdateAction;
                    if (Parent.Children.Count > 0 && Parent is ICompoundWithUpdate compoundWithUpdate && this is not IInputSlot)
                    {
                        //Log.Debug($"Skipping connection for compound op with update method for {Parent.Symbol} {this}", compoundWithUpdate);
                        //compoundWithUpdate.RegisterOutputUpdateAction(this, ConnectedUpdate);
                        InputConnections.Insert(index, (Slot<T>)sourceSlot);
                        DirtyFlag.Target = sourceSlot.DirtyFlag.Target;
                        DirtyFlag.Reference = DirtyFlag.Target - 1;
                        return;
                    }
                }
                UpdateAction = ConnectedUpdate;
                DirtyFlag.Target = sourceSlot.DirtyFlag.Target;
                DirtyFlag.Reference = DirtyFlag.Target - 1;
            }
            
            if (sourceSlot.ValueType != ValueType)
            {
                Log.Warning("Type mismatch during connection");
                return;
            }
            InputConnections.Insert(index, (Slot<T>)sourceSlot);
        }

        private Action<EvaluationContext> _actionBeforeAddingConnecting;

        public void RemoveConnection(int index = 0)
        {
            if (IsConnected)
            {
                if (index < InputConnections.Count)
                {
                    InputConnections.RemoveAt(index);
                }
                else
                {
                    Log.Error($"Trying to delete connection at index {index}, but input slot only has {InputConnections.Count} connections");
                }
            }

            if (!IsConnected)
            {
                if (_actionBeforeAddingConnecting != null)
                {
                    UpdateAction = _actionBeforeAddingConnecting;
                }
                else
                {
                    // if no connection is set anymore restore the default update action
                    RestoreUpdateAction();
                }
                DirtyFlag.Invalidate();
            }
        }

        public bool IsConnected => InputConnections.Count > 0;

        public ISlot FirstConnection => InputConnections[0];

        protected readonly List<Slot<T>> InputConnections = [];

        public virtual int Invalidate()
        {
            // ReSharper disable once InlineTemporaryVariable
            var dirtyFlag = DirtyFlag;
            
            if (dirtyFlag.IsAlreadyInvalidated || dirtyFlag.HasBeenVisited)
                return dirtyFlag.Target;

            // reduce the number of method (property) calls

            if (IsConnected)
            {
                dirtyFlag.Target = FirstConnection.Invalidate();
            }
            else if (_isInputSlot)
            {
                if(dirtyFlag.Trigger != DirtyFlagTrigger.None)
                    dirtyFlag.Invalidate();
            }
            else
            {
                var parentInputs = _parent.Inputs;
                
                bool outputDirty = dirtyFlag.IsDirty;
                foreach (var input in parentInputs)
                {
                    var inputFlag = input.DirtyFlag;
                    if (input.IsConnected)
                    {
                        int target;
                        if (input.TryGetAsMultiInput(out var multiInput))
                        {
                            // NOTE: In situations with extremely large graphs (1000 of instances)
                            // invalidation can become bottle neck. In these cases it might be justified
                            // to limit the invalidation to "active" parts of the subgraph. The [Switch]
                            // operator defines this list.
                            var multiInputLimitList = multiInput.LimitMultiInputInvalidationToIndices;
                            var hasLimitList = multiInputLimitList.Count > 0;

                            var collectedInputs = multiInput.GetCollectedInputs();
                            var collectedCount = collectedInputs.Count;
                            target = 0;
                            for (var i = 0; i < collectedCount; i++)
                            {
                                if (hasLimitList && !multiInputLimitList.Contains(i))
                                    continue;

                                target += collectedInputs[i].Invalidate();
                            }
                        }
                        else
                        {
                            target = input.FirstConnection.Invalidate();
                        }
                        
                        inputFlag.Target = target;
                    }
                    else if ((inputFlag.Trigger & DirtyFlagTrigger.Animated) == DirtyFlagTrigger.Animated)
                    {
                        inputFlag.Invalidate();
                    }

                    inputFlag.SetVisited();
                    outputDirty |= inputFlag.IsDirty;
                }

                if (outputDirty || (dirtyFlag.Trigger & DirtyFlagTrigger.Animated) == DirtyFlagTrigger.Animated)
                {
                    dirtyFlag.Invalidate();
                }
            }

            dirtyFlag.SetVisited();
            return dirtyFlag.Target;
        }

        Guid ISlot.Id { get => Id; set => Id = value; }
        DirtyFlag ISlot.DirtyFlag => DirtyFlag;

        public virtual Action<EvaluationContext> UpdateAction { get; set; }

        protected Action<EvaluationContext> _keepOriginalUpdateAction;
        private DirtyFlagTrigger _keepDirtyFlagTrigger;
        protected Slot<T> _targetInputForBypass;
        
        private readonly bool _isInputSlot;
        public readonly bool IsMultiInput;
        protected readonly MultiInputSlot<T> ThisAsMultiInputSlot;
        private Instance _parent;
        private readonly bool _valueIsCommand;
    }
}