﻿using System;
using System.Collections.Generic;
using T3.Gui.Interaction.PresetSystem.Model;

namespace T3.Gui.Interaction.PresetSystem
{
    public class ParameterGroup
    {
        public Guid Id = Guid.NewGuid();
        public string Title;
        public List<GroupParameter> Parameters = new List<GroupParameter>(16);
        
        // TODO: Do not serialize
        public Preset ActivePreset { get; set; }

        public void AddParameterToIndex(GroupParameter parameter, int index)
        {
            // Extend list
            while (Parameters.Count <= index)
            {
                Parameters.Add(null);
            }

            Parameters[index] = parameter;
        }

        public void SetActivePreset(Preset preset)
        {
            if(ActivePreset != null) 
                ActivePreset.State = Preset.States.InActive;
            
            ActivePreset = preset;
        }
    }
}