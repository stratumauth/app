// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Newtonsoft.Json;

namespace Stratum.Droid
{
    public class CategorySelector : IEquatable<CategorySelector>
    {
        [JsonProperty]
        private readonly string _categoryId;
        
        [JsonProperty]
        private readonly MetaCategory _metaCategory;
      
        [JsonConstructor]
        private CategorySelector(string categoryId, MetaCategory metaCategory)
        {
            _categoryId = categoryId;
            _metaCategory = metaCategory;
        }
        
        private CategorySelector(string id)
        {
            _categoryId = id;
            _metaCategory = MetaCategory.None;
        }

        private CategorySelector(MetaCategory metaCategory)
        {
            _categoryId = null;
            _metaCategory = metaCategory;
        }

        public bool Is(MetaCategory metaCategory)
        {
            return _categoryId == null && _metaCategory == metaCategory;
        }

        public bool Is(string id)
        {
            return _categoryId == id && _metaCategory == MetaCategory.None;
        }

        public bool IsCategory(out string id)
        {
            id = _categoryId;
            return _categoryId != null && _metaCategory == MetaCategory.None;
        }

        public bool IsMetaCategory(out MetaCategory metaCategory)
        {
            metaCategory = _metaCategory;
            return _categoryId == null && _metaCategory != MetaCategory.None;
        }

        public static CategorySelector Of(MetaCategory metaCategory)
        {
            return new CategorySelector(metaCategory);
        }

        public static CategorySelector Of(string id)
        {
            ArgumentNullException.ThrowIfNull(id);
            return new CategorySelector(id);
        }
        
        public bool Equals(CategorySelector other)
        {
            return other != null && _categoryId == other._categoryId && _metaCategory == other._metaCategory;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_categoryId, (int)_metaCategory);
        }
    }
}