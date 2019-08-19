﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using RapidCMS.Common.Extensions;
using RapidCMS.Common.Models.DTOs;
using RapidCMS.Common.Models.Metadata;

namespace RapidCMS.Common.Data
{
    internal class CollectionDataProvider : IRelationDataCollection, IDisposable
    {
        private readonly IRepository _repository;
        private readonly Type _relatedEntityType;
        private readonly IPropertyMetadata? _repositoryParentIdProperty;
        private readonly IPropertyMetadata _idProperty;
        private readonly IEnumerable<IExpressionMetadata> _labelProperties;
        private readonly IMemoryCache _memoryCache;
        private IEntity? _entity;

        private IDisposable? _eventHandle;

        private List<IElement>? _elements;
        private List<IElement>? _relatedElements;
        private ICollection<object>? _relatedIds;

        public CollectionDataProvider(
            IRepository repository, 
            Type relatedEntityType, 
            IPropertyMetadata? repositoryParentIdProperty, 
            IPropertyMetadata idProperty, 
            IEnumerable<IExpressionMetadata> labelProperties,
            IMemoryCache memoryCache)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _relatedEntityType = relatedEntityType ?? throw new ArgumentNullException(nameof(relatedEntityType));
            _repositoryParentIdProperty = repositoryParentIdProperty;
            _idProperty = idProperty ?? throw new ArgumentNullException(nameof(idProperty));
            _labelProperties = labelProperties ?? throw new ArgumentNullException(nameof(labelProperties));
            _memoryCache = memoryCache;
        }

        public event EventHandler OnDataChange;

        public async Task SetEntityAsync(IEntity entity)
        {
            _entity = entity;

            await SetElementsAsync();
        }

        private async Task SetElementsAsync()
        {
            if (_entity == null)
            {
                return;
            }

            _eventHandle?.Dispose();
            _eventHandle = _repository.ChangeToken.RegisterChangeCallback(async x =>
            {
                await SetElementsAsync();

                OnDataChange?.Invoke(this, new EventArgs());

            }, null);

            var parentId = _repositoryParentIdProperty?.Getter.Invoke(_entity) as string;
            var entities = await _memoryCache.GetOrCreateAsync(new { _repository, parentId }, (entry) =>
            {
                entry.AddExpirationToken(_repository.ChangeToken);

                return _repository.InternalGetAllAsync(parentId, Query.Default());
            });

            _elements = entities
                .Select(entity => (IElement)new ElementDTO
                {
                    Id = _idProperty.Getter(entity),
                    Labels = _labelProperties.Select(x => x.StringGetter(entity))
                })
                .ToList();
        }

        public async Task SetRelationMetadataAsync(IEntity entity, IPropertyMetadata collectionProperty)
        {
            var data = collectionProperty.Getter(entity);

            await SetEntityAsync(entity);

            if (data is ICollection<IEntity> entityCollection)
            {
                _relatedIds = entityCollection.Select(x => (object)x.Id).ToList();
            }
            else if (data is ICollection<object> objectCollection)
            {
                _relatedIds = objectCollection;
            }
            else if (data is IEnumerable enumerable)
            {
                var list = new List<object>();
                foreach (var element in enumerable)
                {
                    if (element != null)
                    {
                        list.Add(element);
                    }
                }
                _relatedIds = list;
            }
            else
            {
                throw new InvalidOperationException("Failed to convert relation property to ICollection<object>");
            }

            UpdateRelatedElements();
        }

        private void UpdateRelatedElements()
        {
            _relatedElements = _elements?.Where(x => _relatedIds?.Contains(x.Id) ?? false).ToList();
        }

        public Task<IEnumerable<IElement>> GetAvailableElementsAsync()
        {
            return Task.FromResult(_elements ?? Enumerable.Empty<IElement>());
        }

        public Task<IReadOnlyList<IElement>> GetRelatedElementsAsync()
        {
            var relatedElements = (_relatedIds != null && _elements != null)
                ? _elements.Where(x => _relatedIds.Contains(x.Id)).ToList()
                : new List<IElement>();

            return Task.FromResult(relatedElements as IReadOnlyList<IElement>);
        }

        public Task AddElementAsync(IElement option)
        {
            _relatedElements?.Add(_elements.First(x => x.Id == option.Id));

            return Task.CompletedTask;
        }
        public Task RemoveElementAsync(IElement option)
        {
            return Task.FromResult(_relatedElements?.RemoveAll(x => x.Id == option.Id));
        }

        public Task SetElementAsync(IElement option)
        {
            _relatedElements?.Clear();
            _relatedElements?.Add(_elements.First(x => x.Id == option.Id));

            return Task.CompletedTask;
        }

        public IReadOnlyList<IElement> GetCurrentRelatedElements()
        {
            return _relatedElements ?? new List<IElement>();
        }

        public Type GetRelatedEntityType()
        {
            return _relatedEntityType ?? typeof(object);
        }

        public void Dispose()
        {
            _eventHandle?.Dispose();
        }
    }
}
