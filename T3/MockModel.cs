﻿using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using Random = System.Random;

namespace T3
{
    class MockModel
    {
        public MockModel()
        {
            Init();
        }

        class AddOperator : Instance
        {
            public readonly Slot<float> Result;

            public AddOperator()
            {
                Result = new Slot<float>(Update);
                Input1.Name = "Input1";
                Inputs.Add(Input1);
                Input2.Name = "Input2";
                Inputs.Add(Input2);
            }

            private void Update(EvaluationContext context)
            {
                Result.Value = Input1.GetValue(context) + Input2.GetValue(context);
            }

            public readonly InputSlot<float> Input1 = new InputSlot<float>(5.0f);
            public readonly InputSlot<float> Input2 = new InputSlot<float>(10.0f);
        }

        class RandomOperator : Instance
        {
            public readonly Slot<float> Result;

            public RandomOperator()
            {
                Result = new Slot<float>(Update);
                Seed.Name = "Seed";
                Inputs.Add(Seed);
            }

            private void Update(EvaluationContext context)
            {
                var random = new Random(Seed.GetValue(context));
                Result.Value = (float)random.NextDouble();
            }

            public readonly InputSlot<int> Seed = new InputSlot<int>(0);
        }

        class ProjectOperator : Instance
        {

        }

        private void Init()
        {
            var addSymbol = new Symbol()
                            {
                                Id = Guid.NewGuid(),
                                SymbolName = "Add",
                                InstanceType = typeof(AddOperator),
                                InputDefinitions =
                                {
                                    new InputDefinition { Id = Guid.NewGuid(), Name = "Value1", InputValue = new InputValue<float>(5.0f) },
                                    new InputDefinition { Id = Guid.NewGuid(), Name = "Value1", InputValue = new InputValue<float>(10.0f) }
                                }
                            };
            var randomSymbol = new Symbol()
                               {
                                   Id = Guid.NewGuid(),
                                   SymbolName = "Random",
                                   InstanceType = typeof(RandomOperator),
                                   InputDefinitions =
                                   {
                                       new InputDefinition { Id = Guid.NewGuid(), Name = "Seed", InputValue = new InputValue<int>(42) }
                                   }
                               };
            var projectSymbol = new Symbol()
                                {
                                    Id = Guid.NewGuid(),
                                    SymbolName = "Project",
                                    InstanceType = typeof(ProjectOperator),
                                    _children =
                                    {
                                        new SymbolChild() { Id = Guid.NewGuid(), Symbol = addSymbol, },
                                        new SymbolChild() { Id = Guid.NewGuid(), Symbol = addSymbol, },
                                        new SymbolChild() { Id = Guid.NewGuid(), Symbol = randomSymbol },
                                    }
                                };

            // register the symbols globally
            var symbols = SymbolRegistry.Entries;
            symbols.Add(addSymbol.Id, addSymbol);
            symbols.Add(randomSymbol.Id, randomSymbol);
            symbols.Add(projectSymbol.Id, projectSymbol);

            // create instance of project op, all children are create automatically
            Instance projectOp = projectSymbol.CreateInstance();

            // create ui data for project symbol
            var uiEntries = InstanceUiRegistry.Instance.UiEntries;
            uiEntries.Add(projectSymbol.Id, new Dictionary<Guid, SymbolChildUi>()
                                            {
                                                { projectOp.Children[0].Id, new SymbolChildUi { SymbolChild = projectSymbol._children[0], Name = "Add1" } },
                                                { projectOp.Children[1].Id, new SymbolChildUi { SymbolChild = projectSymbol._children[1] } },
                                                { projectOp.Children[2].Id, new SymbolChildUi { SymbolChild = projectSymbol._children[2], Name = "Random" } },
                                            });

            // create and register input controls
            InputUiRegistry.Entries.Add(typeof(float), new FloatInputUi());
            InputUiRegistry.Entries.Add(typeof(int), new IntInputUi());

            _initialized = true;
            MainOp = projectOp;

            //_nodes.Add(new Node()
            //{
            //    ID = 0,
            //    Name = "MainTex",
            //    Pos = new Vector2(40, 50),
            //    Value = 0.5f,
            //    Color = TColors.White,
            //    InputsCount = 1,
            //    OutputsCount = 1,
            //}
            //);
            //_nodes.Add(new Node()
            //{
            //    ID = 1,
            //    Name = "MainTex2",
            //    Pos = new Vector2(140, 50),
            //    Value = 0.5f,
            //    Color = TColors.White,
            //    InputsCount = 1,
            //    OutputsCount = 1
            //}
            //);
            //_nodes.Add(new Node()
            //{
            //    ID = 2,
            //    Name = "MainTex3",
            //    Pos = new Vector2(240, 50),
            //    Value = 0.5f,
            //    Color = TColors.White,
            //    InputsCount = 1,
            //    OutputsCount = 1
            //}
            //);

            //_links.Add(new NodeLink(0, 0, 2, 0));
            //_links.Add(new NodeLink(1, 0, 2, 1));
        }

        public Instance MainOp;
        private bool _initialized;
    }
}
