﻿using System;
using System.Collections.Generic;
using RapidCMS.Common.Attributes;
using RapidCMS.Common.Enums;
using RapidCMS.Common.Extensions;
using RapidCMS.Common.Interfaces;

namespace RapidCMS.Common.Models.Config
{
    public class NodeEditorConfig
    {
        public Type BaseType { get; set; }
        public List<ButtonConfig> Buttons { get; set; } = new List<ButtonConfig>();
        public List<NodeEditorPaneConfig> EditorPanes { get; set; } = new List<NodeEditorPaneConfig>();
    }

    public class NodeEditorConfig<TEntity> : NodeEditorConfig
        where TEntity : IEntity
    {
        public NodeEditorConfig()
        {
            BaseType = typeof(TEntity);
        }

        public NodeEditorConfig<TEntity> AddDefaultButton(DefaultButtonType type, string label = null, string icon = null)
        {
            var button = new DefaultButtonConfig
            {
                ButtonType = type,
                Icon = icon ?? type.GetCustomAttribute<DefaultIconLabelAttribute>().Icon,
                Label = label ?? type.GetCustomAttribute<DefaultIconLabelAttribute>().Label
            };

            Buttons.Add(button);

            return this;
        }

        public NodeEditorConfig<TEntity> AddCustomButton(string alias, Action action, string label = null, string icon = null)
        {
            var button = new CustomButtonConfig
            {
                Action = action,
                Alias = alias,
                Icon = icon,
                Label = label
            };

            Buttons.Add(button);

            return this;
        }

        public NodeEditorConfig<TEntity> AddEditorPane(Action<NodeEditorPaneConfig<TEntity>> configure)
        {
            return AddEditorPane<TEntity>(configure);
        }

        public NodeEditorConfig<TEntity> AddEditorPane<TDerivedEntity>(Action<NodeEditorPaneConfig<TDerivedEntity>> configure)
            where TDerivedEntity : TEntity
        {
            var config = new NodeEditorPaneConfig<TDerivedEntity>();

            configure.Invoke(config);

            config.VariantType = typeof(TDerivedEntity);

            EditorPanes.Add(config);

            return this;
        }
    }
}